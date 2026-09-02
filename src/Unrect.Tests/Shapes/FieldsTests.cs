using System;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// A block of labelled pairs — the entity card at the top of a K-1, the deal header on an
  /// investor report. Each field is a label cell and the cell beside it, and the block anchors
  /// itself on the first label, so a card that moves with the sheet is still found.
  /// <para>
  /// Labels match by their own rule: trimmed, case-insensitive, and tolerant of a trailing colon,
  /// because sheets are inconsistent about it and a label is the left half of a pair rather than a
  /// whole cell value. That rule is <em>Fields-only</em> — <see cref="CaptionComparerTests"/> pins
  /// that it does not reach captions.
  /// </para>
  /// </summary>
  public class FieldsTests
  {
    // A card sitting two columns in, as a real one does.
    private static ISpace Card() => Mixed(new object?[,]
    {
      { null, null, "EIN:", "12-3456789" },
      { null, null, "Entity Type", "LLC" },
      { null, null, "Deal Type:", "Growth" },
    });

    private static IShape<System.Collections.Generic.IReadOnlyDictionary<string, CellValue>> Entity()
      => Fields(Field("EIN"), Field("Entity Type"), Field("Deal Type"));

    // --- What it reads -------------------------------------------------------------------------------

    [Fact]
    public void ABlockYieldsOneEntryPerDeclaredField()
    {
      var entity = Entity().Map(Card());

      Assert.Equal(3, entity.Count);
      Assert.Equal(new[] { "EIN", "Entity Type", "Deal Type" }, entity.Keys.ToArray());
    }

    [Fact]
    public void TheKeysAreTheDeclaredLabelsNotTheFilesText()
    {
      // The declaration says EIN; the sheet says "EIN:". The key is what the code asked for, so a
      // consumer's lookups do not depend on the sheet's punctuation.
      var entity = Entity().Map(Card());

      Assert.True(entity.ContainsKey("EIN"));
      Assert.False(entity.ContainsKey("EIN:"));
    }

    [Fact]
    public void LookupsGoThroughTheCaptionComparer()
    {
      var entity = Entity().Map(Card());

      Assert.Equal("LLC", entity["entitytype"].GetString());
      Assert.Equal("LLC", entity["  Entity  Type  "].GetString());
    }

    [Fact]
    public void ValuesKeepTheirKinds()
    {
      var space = Mixed(new object?[,]
      {
        { "Count:", 42 },
        { "As Of", new DateTime(2026, 6, 30) },
        { "Note", null },
      });

      var entity = Fields(Field("Count"), Field("As Of"), Field("Note")).Map(space);

      Assert.Equal(CellKind.Number, entity["Count"].Kind);
      Assert.Equal(CellKind.Temporal, entity["As Of"].Kind);

      // A blank value cell is a blank value, not a failure: the label was there, which is what the
      // block asserted.
      Assert.Equal(CellKind.Blank, entity["Note"].Kind);
    }

    // --- The label rule -------------------------------------------------------------------------------

    [Theory]
    [InlineData("EIN")]
    [InlineData("EIN:")]
    [InlineData("ein")]
    [InlineData("  EIN  ")]
    [InlineData("ein :")]
    public void ALabelMatchesWithOrWithoutTheColonAndInAnyCase(string declared)
    {
      var space = Mixed(new object?[,] { { "EIN:", "12-3456789" } });

      Assert.Equal("12-3456789", Fields(Field(declared)).Map(space).Values.Single().GetString());
    }

    [Theory]
    [InlineData("EINS")]
    [InlineData("MY EIN")]
    public void ALabelIsStillAWholeCellValue(string declared)
    {
      Assert.Throws<ShapeException>(() => Fields(Field(declared)).Map(Card()));
    }

    [Fact]
    public void OnlyTheTrailingColonIsAbsorbed()
    {
      // A colon inside a label is part of it.
      var space = Mixed(new object?[,] { { "Note: see below", "x" } });

      Assert.Equal("x", Fields(Field("Note: see below")).Map(space).Values.Single().GetString());
      Assert.Throws<ShapeException>(() => Fields(Field("Note")).Map(space));
    }

    // --- The anchor -------------------------------------------------------------------------------------

    [Fact]
    public void ABlockFindsItselfOnBothAxes()
    {
      // Two columns in and no rows down here; the offset is what the anchor found.
      var applied = Entity().Apply(Card());

      Assert.Equal(2, applied.Offset.Size.Width);
      Assert.Equal(0, applied.Offset.Size.Height);
    }

    [Fact]
    public void ABlockConsumesTwoColumnsByOneRowPerField()
    {
      var applied = Entity().Apply(Card());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void AMovementComposesOntoTheAnchor()
    {
      var space = Mixed(new object?[,]
      {
        { "EIN:", "first" },
        { "EIN:", "second" },
      });

      // The anchor finds row 0; Down(1) carries on from there to the second card.
      Assert.Equal("second", Fields(Field("EIN")).Down(1).Map(space).Values.Single().GetString());
    }

    [Fact]
    public void AfterReplacesTheAnchor()
    {
      var space = Mixed(new object?[,]
      {
        { "junk", "junk" },
        { "EIN:", "12-3456789" },
      });

      Assert.Equal("12-3456789", Fields(Field("EIN")).After(SkipRows(1)).Map(space).Values.Single().GetString());
    }

    [Fact]
    public void AnAnchorThatIsNowhere_SaysWhatItLookedFor()
    {
      var failure = Assert.Throws<ShapeException>(() => Fields(Field("Nope")).Map(Card()));

      Assert.Contains("no column with the label 'Nope' exists in the available space", failure.Message);
    }

    // --- A row that is not the field it should be ---------------------------------------------------------

    [Fact]
    public void AMissingMiddleRow_NamesTheLabelAndWhatWasThereInstead()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Fields(Field("EIN"), Field("Deal Type")).Map(Card()));

      Assert.Equal(
        "expected a label reading 'Deal Type' here, but this cell reads 'Entity Type'",
        Problem(failure));
      Assert.Equal("Field(\"Deal Type\")#2", failure.Subject);
    }

    [Fact]
    public void ABlockMissIsAbsorbable()
    {
      var result = Fields(Field("EIN"), Field("Deal Type")).Optional().MapWithDiagnostics(Card());

      Assert.Null(result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Equal("Field(\"Deal Type\")#2", warning.Subject);
    }

    [Fact]
    public void ABlockSizedTooSmallForItsFields_Throws()
    {
      // A field is two cells wide by construction, so a block bounded to one column cannot hold
      // one. The failure is the engine's — the field does not fit the extent it was handed — and
      // it names the field that could not be placed.
      var failure = Assert.Throws<ShapeException>(() =>
        Fields(Field("EIN")).Sized(Extent(1, 1)).Map(Card()));

      Assert.Contains("an extent of 2x1 does not fit here", failure.Message);
      Assert.Equal("Field(\"EIN\")#1", failure.Subject);
    }

    // --- Inspection and naming -------------------------------------------------------------------------------

    [Fact]
    public void ABlockIsAnOpaqueFlowThatDescribesItselfByItsFactory()
    {
      var entity = Entity();

      Assert.Equal("Fields", entity.Description);
      Assert.Empty(entity.Children);

      var marker = Assert.IsAssignableFrom<IOpaqueComposite>(entity);

      Assert.Equal("declared by a cursor lambda; children are known only while it runs", marker.Reason);
    }

    [Fact]
    public void EachFieldIsARealChildRenderedAtItsOrdinal()
    {
      var entity = Fields(Field("EIN"), Field("Entity Type"), Field("Deal Type"), Field("Vintage"));

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => v.Next(entity)).Map(Card()));

      Assert.Equal("VerticalFlow -> 'entity' -> Field(\"Vintage\")#4", failure.Path);
    }

    [Fact]
    public void NoChildEverCarriesAnIdentifierFromInsideTheHelper()
    {
      // The phase-B leak pin, on this helper: Fields builds its flow from a loop over its own
      // parameter, and without an explicit opt-out the naming ladder would label every field with
      // an identifier no user wrote.
      var failure = Assert.Throws<ShapeException>(() => Fields(Field("EIN"), Field("Deal Type")).Map(Card()));

      Assert.DoesNotContain("'field'", failure.Path);
      Assert.DoesNotContain("'fields'", failure.Path);
      Assert.Contains("Field(\"Deal Type\")#2", failure.Path);
    }

    // --- Guards ---------------------------------------------------------------------------------------------

    [Fact]
    public void ABlockRejectsADeclarationItCannotHonour()
    {
      var duplicate = Assert.Throws<ArgumentException>(() => Fields(Field("EIN"), Field("ein:")));
      Assert.Equal("fields", duplicate.ParamName);
      Assert.Contains("Two fields carry the label 'ein:'", duplicate.Message);
      Assert.Contains("ignoring case, surrounding whitespace and a trailing colon", duplicate.Message);

      var empty = Assert.Throws<ArgumentException>(() => Fields());
      Assert.Equal("fields", empty.ParamName);
      Assert.Contains("A Fields block must declare at least one field.", empty.Message);

      Assert.Equal("label", Assert.Throws<ArgumentException>(() => Field("")).ParamName);
      Assert.Equal("label", Assert.Throws<ArgumentException>(() => Field("   ")).ParamName);
      Assert.Equal("label", Assert.Throws<ArgumentNullException>(() => Field(null!)).ParamName);
    }

    [Fact]
    public void TwoLabelsThatWouldCollideIntoOneKeyAreRefused()
    {
      // A block is refused for two different relations, and they are not the same relation.
      //
      //   * two labels that would match the SAME CELL — "EIN" and "ein:" — because a label absorbs
      //     a trailing colon, so the block would read one row twice;
      //   * two labels that would become the SAME KEY — "Net Income" and "NetIncome" — because the
      //     dictionary's keys ignore all whitespace, so two distinct rows would collide into one
      //     entry and the second would silently win.
      //
      // The second is the one a reader is least likely to predict, since the labels do not match
      // each other by the label rule at all.
      var collision = Assert.Throws<ArgumentException>(() => Fields(Field("Net Income"), Field("NetIncome")));

      Assert.Equal("fields", collision.ParamName);
      Assert.Contains(
        "The labels 'Net Income' and 'NetIncome' would be the same key; "
        + "a block's keys ignore case and all whitespace, so these two fields would collide into one entry.",
        collision.Message);
    }

    [Fact]
    public void TheTwoRefusalsAreToldApart()
    {
      // Same file, side by side: two relations, two messages, and neither borrows the other's
      // wording — so a reader knows which rule they tripped.
      var sameCell = Assert.Throws<ArgumentException>(() => Fields(Field("EIN"), Field("ein:")));
      var sameKey = Assert.Throws<ArgumentException>(() => Fields(Field("Net Income"), Field("NetIncome")));

      Assert.Contains("Two fields carry the label", sameCell.Message);
      Assert.DoesNotContain("would be the same key", sameCell.Message);

      Assert.Contains("would be the same key", sameKey.Message);
      Assert.DoesNotContain("Two fields carry the label", sameKey.Message);
    }

    [Fact]
    public void ABlockCopiesTheArrayItWasHanded()
    {
      // params may hand us the caller's own array, and the lambda is captured for every future
      // application. A shape that could change underneath its user is not a declaration.
      var fields = new[] { Field("EIN") };

      var entity = Fields(fields);

      fields[0] = Field("Nope");

      Assert.Equal("12-3456789", entity.Map(Card()).Values.Single().GetString());
    }

    // --- The K-1 card, mirrored -----------------------------------------------------------------------------

    [Fact]
    public void TheK1EntityCardIsReadWhereverItSits()
    {
      // The real shape of it: a label column well to the right, two labels carrying colons and
      // three not, and nothing above or beside the card to anchor on but the first label.
      var space = Mixed(new object?[,]
      {
        { null, null, null, null, null },
        { null, null, null, "EIN:", "98-7654321" },
        { null, null, null, "Entity Type", "Partnership" },
        { null, null, null, "Deal Type:", "Buyout" },
        { null, null, null, "Vintage", 2019 },
        { null, null, null, "Currency", "USD" },
      });

      var card = Fields(
        Field("EIN"),
        Field("Entity Type"),
        Field("Deal Type"),
        Field("Vintage"),
        Field("Currency"));

      var applied = card.Apply(space);

      Assert.Equal(
        new[] { "EIN", "Entity Type", "Deal Type", "Vintage", "Currency" },
        applied.Value.Keys.ToArray());
      Assert.Equal("98-7654321", applied.Value["EIN"].GetString());
      Assert.Equal(2019, applied.Value["Vintage"].GetInt());

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(5, applied.Consumed.Height);
    }

    /// <summary>The problem text, without the subject the template puts in front of it.</summary>
  }
}
