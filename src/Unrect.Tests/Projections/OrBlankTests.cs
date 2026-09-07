using System;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>OrBlank</c>: the leaf's way of saying "this field may be absent". A blank reads as null and
  /// nothing is recorded, because an expected blank is a value the declaration allowed for — where
  /// <c>Optional</c> absorbs a <em>failure</em> and says so with a <c>Warning</c>. That difference is
  /// the whole reason the modifier exists, so it is pinned from both sides over the same cell.
  /// <para>
  /// Everything else about the leaf is untouched. A cell of the wrong kind still fails in the
  /// document's vocabulary, and a number that will not fit still fails as a conversion on a number
  /// that is really there — the §1.4 discipline <see cref="TypedLeafTests"/> protects, restated here
  /// because tolerance is exactly where a shortcut would blur it.
  /// </para>
  /// </summary>
  public class OrBlankTests
  {
    private static ISpace One(object? value) => Mixed(new object?[,] { { value } });

    /// <summary>
    /// One row eight columns wide with something in column 0 and <paramref name="atSix"/> in column
    /// 6 — the sparse shape the modifier was designed for, small enough to say one thing.
    /// </summary>
    private static ISpace Sparse(object? atSix)
    {
      var cells = new object?[1, 8];

      cells[0, 0] = "ACCOUNT";
      cells[0, 6] = atSix;

      return Mixed(cells);
    }

    // --- Quietly: the difference from Optional -----------------------------------------------------

    [Fact]
    public void ABlankCellReadsAsNull()
    {
      Assert.Null(Decimal().OrBlank().Map(One(null)));
      Assert.Null(Text().OrBlank().Map(One(null)));
    }

    [Fact]
    public void AndRecordsNothingAtAll()
    {
      // The pin the modifier exists for. Nothing is absorbed here, so there is nothing to report:
      // the declaration said the cell may be absent and the cell is absent.
      var read = Decimal().OrBlank().MapWithDiagnostics(One(null));

      Assert.Null(read.Value);
      Assert.Empty(read.Diagnostics);
    }

    [Fact]
    public void WhereTheSameCellThroughOptionalRecordsAWarning()
    {
      // The other half, over the same cell, so the two spellings can be read side by side. Optional
      // turns a kind failure — a blank IS a kind failure to Decimal — into a default plus a Warning,
      // which is right for a section that may be missing and wrong for a field that may be empty.
      var read = Decimal().Optional().MapWithDiagnostics(One(null));

      var warning = Assert.Single(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("Decimal", warning.Subject);
      Assert.Equal("A1", warning.Location.A1);
      Assert.Equal("expected Number at A1, found Blank", warning.Message);
    }

    // --- Loudly: everything that is not a blank ----------------------------------------------------

    [Fact]
    public void AWrongKindStillFailsInTheDocumentsOwnVocabulary()
    {
      // Word for word what the intolerant leaf says. Blankness is about the data; a kind is about
      // the format, and no format tolerates the wrong one.
      var tolerant = Assert.Throws<ProjectionException>(() => Decimal().OrBlank().Map(One("n/a")));
      var strict = Assert.Throws<ProjectionException>(() => Decimal().Map(One("n/a")));

      Assert.Equal("expected Number at A1, found Text", Problem(tolerant));
      Assert.Equal(Problem(strict), Problem(tolerant));
    }

    [Fact]
    public void TextOverANumberIsAKindFailureToo()
    {
      Assert.Equal(
        "expected Text at A1, found Number",
        Problem(Assert.Throws<ProjectionException>(() => Text().OrBlank().Map(One(1)))));
    }

    [Fact]
    public void AnErrorCellIsNotABlankAndIsNotTolerated()
    {
      // The kind that most looks like an absence and is not one: #DIV/0! is the sheet saying it
      // tried and failed, which is never what "this field may be omitted" declared.
      Assert.Equal(
        "expected Number at A1, found Error(#DIV/0!)",
        Problem(Assert.Throws<ProjectionException>(() =>
          Decimal().OrBlank().Map(One(CellValue.OfError(CellError.DivisionByZero))))));
    }

    [Fact]
    public void AWhitespaceOnlyCellIsTextUnlessTheAdapterSaidOtherwise()
    {
      // Blankness is decided at adaptation time, not by the leaf, so what OrBlank tolerates is
      // whatever the space calls blank — and the array adapter calls "" blank and "   " Text.
      Assert.Equal(
        "expected Number at A1, found Text",
        Problem(Assert.Throws<ProjectionException>(() => Decimal().OrBlank().Map(One("   ")))));

      Assert.Null(Decimal().OrBlank().Map(One("")));
    }

    [Fact]
    public void ANumberThatWillNotFitStillFailsAsAConversion()
    {
      // The kind was right, so the message names the value rather than the column — the second half
      // of the §1.4 discipline, unchanged by tolerance.
      var failure = Assert.Throws<ProjectionException>(() => Integer().OrBlank().Map(One(1.5)));

      Assert.Equal("the Number at A1 (1.5) is not a whole number", Problem(failure));
      Assert.DoesNotContain("expected", Problem(failure));
    }

    // --- The algebra --------------------------------------------------------------------------------

    [Fact]
    public void ItCommutesWithPlacement()
    {
      // Decimal().Right(6).OrBlank() and Decimal().OrBlank().Right(6) are the same declaration: the
      // kind, the accessor and the placement are the leaf's own, and tolerance changes only what a
      // blank means. Compared on everything a caller can observe rather than on one reading.
      var placedThenTolerant = Decimal().Right(6).OrBlank();
      var tolerantThenPlaced = Decimal().OrBlank().Right(6);

      Assert.Equal(1250.75m, placedThenTolerant.Map(Sparse(1250.75m)));
      Assert.Equal(1250.75m, tolerantThenPlaced.Map(Sparse(1250.75m)));

      Assert.Null(placedThenTolerant.Map(Sparse(null)));
      Assert.Null(tolerantThenPlaced.Map(Sparse(null)));

      var placedFirst = placedThenTolerant.Apply(Sparse(1250.75m));
      var tolerantFirst = tolerantThenPlaced.Apply(Sparse(1250.75m));

      Assert.Equal(placedFirst.Consumed, tolerantFirst.Consumed);
      Assert.Equal(placedThenTolerant.Description, tolerantThenPlaced.Description);

      // ...including where they fail, which is the reading that would expose a lost offset.
      var one = Assert.Throws<ProjectionException>(() => placedThenTolerant.Map(Sparse("n/a")));
      var other = Assert.Throws<ProjectionException>(() => tolerantThenPlaced.Map(Sparse("n/a")));

      Assert.Equal("expected Number at G1, found Text", Problem(one));
      Assert.Equal(one.Message, other.Message);
      Assert.Equal(one.Path, other.Path);
    }

    [Fact]
    public void ItKeepsTheNameWhicheverSideItIsWrittenOn()
    {
      Assert.Equal("primary", Decimal().Named("primary").OrBlank().Name);
      Assert.Equal("primary", Decimal().OrBlank().Named("primary").Name);
      Assert.Equal("fund", Text().Named("fund").OrBlank().Name);
    }

    [Fact]
    public void AnUnnamedLeafIsStillUnnamedAfterwards()
    {
      // The other side of the clause that preserves the name: it must not invent one, or a hoisted
      // leaf would stop borrowing the identifier at its use site.
      Assert.Null(Decimal().OrBlank().Name);
      Assert.Null(Text().OrBlank().Name);
    }

    [Fact]
    public void ItDescribesItselfAsTheNullableReading()
    {
      Assert.Equal("Decimal?", Decimal().OrBlank().Description);
      Assert.Equal("Text?", Text().OrBlank().Description);
      Assert.Equal("Integer?", Integer().OrBlank().Description);
      Assert.Equal("Double?", Double().OrBlank().Description);
      Assert.Equal("Date?", Date().OrBlank().Description);
      Assert.Equal("Boolean?", Boolean().OrBlank().Description);
    }

    [Fact]
    public void AndSaysSoOnAFailurePath()
    {
      // Where the description is actually read: a declaration that tolerated a blank and still met
      // the wrong kind should say which of the two readings was declared.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => v.Next(Decimal().OrBlank())).Map(One("n/a")));

      Assert.Equal("VerticalFlow -> Decimal?#1", failure.Path);
      Assert.Equal("Decimal?#1", failure.Subject);
    }

    [Fact]
    public void AHoistedLeafStillBorrowsTheIdentifierItWasWrittenAs()
    {
      // The naming ladder is unchanged: the modifier does not name what it returns, so the use site
      // still gets to.
      var primary = Decimal().OrBlank();

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => v.Next(primary)).Map(One("n/a")));

      Assert.Equal("VerticalFlow -> 'primary' (Decimal?)", failure.Path);
    }

    [Fact]
    public void ItStillConsumesExactlyOneCell()
    {
      var applied = Decimal().OrBlank().Apply(One(null));

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    // Not testable, and recorded here because it is a designed refusal rather than an omission:
    // NEITHER of these compiles, and they are refused for two different reasons.
    //
    //   Decimal().OrBlank().OrBlank()   CS0453, "the type 'decimal?' must be a non-nullable value
    //     type". The first call returns IProjection<decimal?>; the generic overload constrains T to
    //     a non-nullable struct and the reference-typed one takes IProjection<string>. There is no
    //     Nullable<Nullable<T>> to reach for, so the second "may be absent" has nothing left to say.
    //     This is the library's own refusal, and the one the design intended.
    //
    //   Text().OrBlank().OrBlank()      CS8620 — IProjection<string?> cannot be passed where
    //     IProjection<string> is wanted, because IProjection<TResult> is invariant. Verified against
    //     this tree, where TreatWarningsAsErrors makes it an error outright; in a project that only
    //     warns it would compile and be a harmless no-op, since string? and string are one type at
    //     run time and the receiver is a TypedCellProjection either way. So the reference half of
    //     the family is closed by C#'s nullability rules rather than by anything here, which is why
    //     it is a note and not a pin — the compiler diagnostic is not this library's to keep.

    // --- The construction guard ---------------------------------------------------------------------

    [Fact]
    public void OnAProjectionThatDeclaresNoKind_ItIsADeclarationError()
    {
      // Raised where the projection is built, not per file: a blank has no meaning to read anywhere
      // but in a reading that asserted a kind, so this is a broken declaration and not bad data.
      var failure = Assert.Throws<ArgumentException>(() => Row(cells => cells.Count).OrBlank());

      Assert.StartsWith(
        "OrBlank reads a blank cell as null, so it belongs on a cell leaf that declares a kind — "
        + "Text, Decimal, Integer, Double, Date or Boolean. Row is not one.",
        failure.Message);

      Assert.Equal("projection", failure.ParamName);
    }

    [Theory]
    [InlineData("Row", "Row")]
    [InlineData("Cell", "Cell")]
    [InlineData("Range", "Range")]
    [InlineData("Caption", "Caption(\"Total\")")]
    [InlineData("Optional", "Optional")]
    [InlineData("Select", "Select")]
    public void AndItNamesTheReceiverItRefused(string receiver, string described)
    {
      // Every shape of receiver that reaches the guard, each naming itself as a reader would see it
      // written. Caption and Optional are the two worth having by name: a caption reads a cell and
      // still declares no kind, and a tolerance wrapper is the one a reader would most plausibly try
      // to stack this on.
      var failure = Assert.Throws<ArgumentException>(() => Refuse(receiver));

      Assert.Contains($". {described} is not one.", failure.Message);
    }

    private static IProjection Refuse(string receiver) => receiver switch
    {
      "Row" => Row(cells => cells.Count).OrBlank(),
      "Cell" => Cell(cell => cell.GetInt()).OrBlank(),
      "Range" => Range(block => block.Width).OrBlank(),

      // A caption reads text out of a cell and is still not a typed leaf: it asserts a spelling
      // rather than a kind, and its result is the file's own, never an absence.
      "Caption" => Caption("Total").OrBlank(),

      // The pin the modifier's own documentation implies: the two tolerances do not stack. Optional
      // has already turned every failure into a default, so there is no blank left to tolerate.
      "Optional" => Decimal().Optional().OrBlank(),

      // A conversion beyond the leaf's own reading is Select territory, and a Select is not a leaf.
      "Select" => Decimal().Select(amount => amount * 2).OrBlank(),

      _ => throw new ArgumentOutOfRangeException(nameof(receiver), receiver, "No such receiver."),
    };

    [Fact]
    public void AndItRefusesNullTheWayEveryModifierDoes()
    {
      Assert.Throws<ArgumentNullException>(() => ((IProjection<decimal>)null!).OrBlank());
      Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).OrBlank());
    }
  }
}
