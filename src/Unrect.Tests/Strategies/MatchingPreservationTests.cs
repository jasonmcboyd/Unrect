using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// What every text matcher refuses, pinned kind by kind: a cell that is not <c>Text</c> is not
  /// found by a declaration that spells out what the cell would <em>look like</em>. A numeric 42 is
  /// not a row containing "42"; a boolean is not a row containing "TRUE"; an error cell is not a row
  /// containing "#DIV/0!". The same for the column twins, for <c>Caption</c>, for <c>Field</c>, and
  /// for the header parse behind a label map, where a non-text header cell leaves its column
  /// unlabelled rather than named by its rendering.
  /// <para>
  /// This is not a new rule — it falls out of every matcher reading a cell through
  /// <c>TryGetString</c>, which answers only for <see cref="CellKind.Text"/>. It is pinned because
  /// the rule is currently an emergent property of one accessor and is due to become an explicit
  /// guard: once a cell can render itself as text, the same predicates would see "42" everywhere
  /// unless the text-only test is kept. A widening here would silently re-anchor real declarations
  /// — a section bounded by <c>RowContaining("2026")</c> would start finding date cells — so the
  /// refusals are stated positively, one per matcher, rather than left to be inferred.
  /// </para>
  /// <para>
  /// Each test carries its positive twin: the same matcher, the same needle, over a <em>text</em>
  /// cell spelling it. A pin that only asserts "not found" would still pass if the matcher stopped
  /// finding anything at all.
  /// </para>
  /// </summary>
  public class MatchingPreservationTests
  {
    // --- The kinds, and the text each of them obviously looks like ---------------------------------
    //
    // The needle in every negative case is the rendering a reader would expect the cell to have:
    // the number's digits, the date's ISO form, the boolean's Excel spelling, the error's literal.
    // Passed as the kind's name rather than as the value itself so the theory data stays
    // serialisable and a failing case names its kind in the runner.

    private const string ANumber = "Number";
    private const string ATemporal = "Temporal";
    private const string ABoolean = "Boolean";
    private const string AnError = "Error";

    /// <summary>
    /// Every non-text kind paired with the text a reader would expect its cell to have — the one
    /// list every refusal and every grounding pin below is stated over, so a kind cannot be added
    /// to one family and forgotten in another.
    /// <para>
    /// It serves both axes: <see cref="ColumnsHolding"/> places the identical
    /// <see cref="Cell"/> that <see cref="RowsHolding"/> does, transposed, so a needle
    /// grounded on one is grounded on the other.
    /// </para>
    /// </summary>
    public static TheoryData<string, string> Needles => new TheoryData<string, string>
    {
      { ANumber, "42" },
      { ATemporal, "2026-03-04" },
      { ABoolean, "TRUE" },
      { AnError, "#DIV/0!" },
    };

    /// <summary>
    /// The same needles as text alone — what a positive twin spells into a text cell. Derived from
    /// <see cref="Needles"/> rather than written out again, because a twin that had drifted from
    /// its refusal would stop being a twin without anything saying so.
    /// </summary>
    public static TheoryData<string> NeedleTexts
    {
      get
      {
        var texts = new TheoryData<string>();

        foreach (var needle in Needles)
          texts.Add((string)needle[1]);

        return texts;
      }
    }

    /// <summary>The cell one of the kind names above stands for.</summary>
    private static object CellOf(string kind) =>
      kind switch
      {
        ANumber => 42,
        ATemporal => new DateTime(2026, 3, 4),
        ABoolean => true,
        AnError => Cell.OfError(CellError.DivisionByZero),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No cell for that kind.")
      };

    /// <summary>
    /// The predicate a declaration writes when it means "the cell that says <paramref name="text"/>".
    /// Today the only text a cell offers is a <see cref="CellKind.Text"/> cell's own string, so a
    /// number, a date, a boolean and an error are all invisible to it.
    /// </summary>
    private static Func<Point<ISpace>, bool> Says(string text) => cell => cell.IsText && cell.AsText()!.Trim() == text;

    /// <summary>A junk row, then <paramref name="value"/> at A2 with a neighbour at B2.</summary>
    private static ISheetCells RowsHolding(object? value) => Mixed(new object?[,]
    {
      { "junk", null },
      { value, "x" },
    });

    /// <summary>A junk column, then <paramref name="value"/> at B1 with a neighbour at B2.</summary>
    private static ISheetCells ColumnsHolding(object? value) => Mixed(new object?[,]
    {
      { "junk", value },
      { null, "x" },
    });

    // --- What the cells in these pins actually say ---------------------------------------------------
    //
    // Every refusal below is worth something only if its needle is the text the cell really has. A
    // needle no rendering could produce would make "not found" true for an uninteresting reason,
    // and the widening these pins exist to catch would sail through them. So the needles are
    // checked against the canonical surface first: each non-text cell SAYS exactly the needle, and
    // is still not a text cell.

    [Theory]
    [MemberData(nameof(Needles))]
    public void TheNeedleIsExactlyWhatTheNonTextCellSays(string kind, string text)
    {
      var cells = RowsHolding(CellOf(kind));

      Assert.Equal(text, cells.AsText(0, 1));
      Assert.False(cells.IsText(0, 1));
      Assert.False(cells.IsBlank(0, 1));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void ATextCellSpellingTheSameThingSaysItAsItsOwnValue(string text)
    {
      // The positive twin: the same words, and this time they are the cell's own. IsText is the
      // whole of the difference the matchers read.
      var cells = RowsHolding(text);

      Assert.Equal(text, cells.AsText(0, 1));
      Assert.True(cells.IsText(0, 1));
    }

    // --- RowContaining ------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Needles))]
    public void RowContaining_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      Assert.Null(RowLandmarks.RowContaining(text).FindRow(RowsHolding(CellOf(kind))));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void RowContaining_MatchesATextCellSpellingTheSameThing(string text)
    {
      Assert.Equal(1, RowLandmarks.RowContaining(text).FindRow(RowsHolding(text)));
    }

    // --- RowWithCell --------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Needles))]
    public void RowWithCell_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      Assert.Null(RowLandmarks.RowWithCell(Says(text)).FindRow(RowsHolding(CellOf(kind))));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void RowWithCell_MatchesATextCellSpellingTheSameThing(string text)
    {
      Assert.Equal(1, RowLandmarks.RowWithCell(Says(text)).FindRow(RowsHolding(text)));
    }

    // --- ColumnContaining ---------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Needles))]
    public void ColumnContaining_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      Assert.Null(ColumnLandmarks.ColumnContaining(text).FindColumn(ColumnsHolding(CellOf(kind))));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void ColumnContaining_MatchesATextCellSpellingTheSameThing(string text)
    {
      Assert.Equal(1, ColumnLandmarks.ColumnContaining(text).FindColumn(ColumnsHolding(text)));
    }

    // --- ColumnWithCell -----------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Needles))]
    public void ColumnWithCell_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      Assert.Null(ColumnLandmarks.ColumnWithCell(Says(text)).FindColumn(ColumnsHolding(CellOf(kind))));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void ColumnWithCell_MatchesATextCellSpellingTheSameThing(string text)
    {
      Assert.Equal(1, ColumnLandmarks.ColumnWithCell(Says(text)).FindColumn(ColumnsHolding(text)));
    }

    // --- Caption ------------------------------------------------------------------------------------
    //
    // A caption asserts a LABEL. A rendered number is not one, so a caption over a numeric cell is a
    // miss — the loud, absorbable "no row containing ..." failure, not a silent anchor onto the
    // number.

    [Theory]
    [MemberData(nameof(Needles))]
    public void Caption_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      var failure = Assert.Throws<ProjectionException>(() => Caption(text).Map(RowsHolding(CellOf(kind))));

      Assert.Equal($"no row containing '{text}' exists in the available space", Problem(failure));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void Caption_MatchesATextCellSpellingTheSameThing(string text)
    {
      Assert.Equal(text, Caption(text).Map(RowsHolding(text)));
    }

    // --- Field --------------------------------------------------------------------------------------

    [Theory]
    [MemberData(nameof(Needles))]
    public void Field_DoesNotMatchANonTextCellsText(string kind, string text)
    {
      var failure = Assert.Throws<ProjectionException>(() => Fields(Field(text)).Map(RowsHolding(CellOf(kind))));

      Assert.Equal($"no column with the label '{text}' exists in the available space", Problem(failure));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void Field_MatchesATextCellSpellingTheSameThing(string text)
    {
      IReadOnlyDictionary<string, Point<ISheetCells>> read = Fields(Field(text)).Map(RowsHolding(text));

      Assert.Equal("x", read[text].Text());
    }

    // --- Saying: the widening, and the only way to reach it -------------------------------------------
    //
    // Every refusal above is a rule with a door beside it. `Containing` asks what a cell HOLDS and so
    // sees text cells alone; `Saying` asks what a cell SAYS and sees every kind. The pairing is the
    // point: the same needle, the same fixture, one matcher that finds the number and one that does
    // not — so the widening is never something a declaration got by accident, and never something it
    // cannot get at all.
    //
    // Stated over the same Needles, because a kind that is refused by Containing and NOT found by
    // Saying would mean the escape hatch had a hole in it exactly where the refusal had teeth.

    [Theory]
    [MemberData(nameof(Needles))]
    public void RowSaying_FindsWhatRowContainingRefuses(string kind, string text)
    {
      var cells = RowsHolding(CellOf(kind));

      Assert.Equal(1, RowLandmarks.RowSaying(text).FindRow(cells));

      // The other half, restated here so the pair reads as one fact rather than two files apart.
      Assert.Null(RowLandmarks.RowContaining(text).FindRow(cells));
    }

    [Theory]
    [MemberData(nameof(Needles))]
    public void ColumnSaying_FindsWhatColumnContainingRefuses(string kind, string text)
    {
      var cells = ColumnsHolding(CellOf(kind));

      Assert.Equal(1, ColumnLandmarks.ColumnSaying(text).FindColumn(cells));
      Assert.Null(ColumnLandmarks.ColumnContaining(text).FindColumn(cells));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void SayingAlsoFindsTheTextCellThatContainingFinds(string text)
    {
      // The control, and it is not a formality: `Saying` is a WIDENING, so it has to find everything
      // the narrow rule finds as well as the kinds it refuses. A rule that had somehow swapped the
      // two would pass every theory above and fail here.
      Assert.Equal(1, RowLandmarks.RowSaying(text).FindRow(RowsHolding(text)));
      Assert.Equal(1, RowLandmarks.RowContaining(text).FindRow(RowsHolding(text)));

      Assert.Equal(1, ColumnLandmarks.ColumnSaying(text).FindColumn(ColumnsHolding(text)));
      Assert.Equal(1, ColumnLandmarks.ColumnContaining(text).FindColumn(ColumnsHolding(text)));
    }

    [Theory]
    [MemberData(nameof(NeedleTexts))]
    public void SayingTrimsAndIgnoresCaseLikeContainingDoes(string text)
    {
      // One content rule, two kind policies — so the trimming and the case policy must not fork with
      // the widening. Spelled with the needle padded and upper-cased, over the text cell where both
      // matchers agree, so any divergence shows as one of the two failing to find it.
      var padded = $"  {text.ToUpperInvariant()}  ";

      Assert.Equal(1, RowLandmarks.RowSaying(padded).FindRow(RowsHolding(text)));
      Assert.Equal(1, ColumnLandmarks.ColumnSaying(padded).FindColumn(ColumnsHolding(text)));
    }

    [Fact]
    public void SayingIsStillWholeCellRatherThanASubstring()
    {
      // The widening is about KIND and nothing else. A rendering is longer than most needles, and a
      // matcher that had relaxed into substring matching here would anchor a section on the "4" of
      // a "42" — which is the false-anchor failure the whole-cell rule exists to prevent.
      var cells = RowsHolding(42);

      Assert.Null(RowLandmarks.RowSaying("4").FindRow(cells));
      Assert.Null(RowLandmarks.RowSaying("2").FindRow(cells));
      Assert.Equal(1, RowLandmarks.RowSaying("42").FindRow(cells));
    }

    [Fact]
    public void AnAbsentSayingMatchSpeaksInItsOwnVoice()
    {
      // The failure sentence, pinned exactly, because it is what a reader sees and because it is the
      // only place the widening is named out loud. "no row saying" and "no row containing" are
      // different sentences on purpose: a declaration that anchored on a rendering and found nothing
      // must not be told the file has no such LABEL, which would send the reader looking for the
      // wrong thing.
      var sheet = RowsHolding(42);

      var row = Assert.Throws<ProjectionException>(() => On(RowSaying("nope")).Of(Text()).Map(sheet));

      Assert.Equal("no row saying 'nope' exists in the available space", Problem(row));

      var column = Assert.Throws<ProjectionException>(
        () => On(ColumnSaying("nope")).Of(Text()).Map(ColumnsHolding(42)));

      Assert.Equal("no column saying 'nope' exists in the available space", Problem(column));

      // ...and the narrow matcher's sentence differs by exactly the one word, which is what makes
      // the pair readable as a choice rather than as two unrelated messages.
      var containing = Assert.Throws<ProjectionException>(() => On(RowContaining("nope")).Of(Text()).Map(sheet));

      Assert.Equal("no row containing 'nope' exists in the available space", Problem(containing));
    }

    // --- Composition: a matcher only locates; the lift decides where the section lands ----------------

    /// <summary>
    /// Four rows whose second holds the NUMBER 42 — the anchor only `Saying` can find — with a
    /// labelled row after it and a labelled row before, so a landing row is unambiguous.
    /// </summary>
    private static ISheetCells NumericAnchor() => Mixed(new object?[,]
    {
      { "header", null },
      { 42, "the anchor row" },
      { "below", "first" },
      { "end", "second" },
    });

    [Fact]
    public void OnASayingMatchOwnsTheRowItFound()
    {
      // Occupancy: the section starts at the matched row and reads it. One pipeline, anchor first
      // and the step after it, because an anchor is a root and a movement composes onto it.
      Assert.Equal("the anchor row", On(RowSaying("42")).Right(1).Of(Text()).Map(NumericAnchor()));
    }

    [Fact]
    public void BelowASayingMatchStartsOneRowBeyondIt()
    {
      // Adjacency: exactly one row past the match, never a distance the declaration chose.
      Assert.Equal("below", Below(RowSaying("42")).Of(Text()).Map(NumericAnchor()));
    }

    [Fact]
    public void UntilASayingMatchStopsJustBeforeIt()
    {
      // The bound, which consumes in full: the header alone is inside it, so the section is one row
      // tall and the row after it is the anchor.
      var band = Until(RowSaying("42")).Of(Range(WholeExtent(), block => block.Height)).Map(NumericAnchor());

      Assert.Equal(1, band);
    }

    [Fact]
    public void AndTheColumnTwinLandsTheSameWay()
    {
      // The transpose of the three above in one fact, since the landing rule is the same rule: the
      // NUMBER 42 sits in column 1, so RightOf starts at column 2.
      var sheet = Mixed(new object?[,]
      {
        { "header", 42, "right of it" },
        { null, "x", "y" },
      });

      Assert.Equal("right of it", RightOf(ColumnSaying("42")).Of(Text()).Map(sheet));
      Assert.Equal("42", On(ColumnSaying("42")).Of(AsSaid()).Map(sheet));
    }

    /// <summary>A cell read as whatever it says, so an anchored column can report a non-text cell.</summary>
    private static IProjectionDefinition<ISheetCells, string?> AsSaid() => Point().Select(point => point.AsText());

    // --- The header parse behind a label map ----------------------------------------------------------
    //
    // The same rule one layer up: LabelMap reads a header cell through TryGetString, so a column
    // whose header is a number, a date, a boolean or an error carries the EMPTY label — it is
    // unnamed, not named "42". Two observations of the one parse: the labels themselves, and what a
    // Table<T> bind says when it goes looking for a caption that is not there.

    [Theory]
    [InlineData(ANumber)]
    [InlineData(ATemporal)]
    [InlineData(ABoolean)]
    [InlineData(AnError)]
    public void AHeadersNonTextCellYieldsAnEmptyLabel(string kind)
    {
      var sheet = Mixed(new object?[,]
      {
        { "Client", CellOf(kind) },
        { "Acme", 10m },
      });

      LabelMap map = ColumnLabels(1).Map(sheet);

      Assert.Equal(new[] { "Client", string.Empty }, map.Labels);
    }

    [Fact]
    public void ATableBindSeesAnEmptyCaptionWhereTheHeaderCellIsNumeric()
    {
      // The label map's rendering of an unlabelled column, read back off the failure the bind
      // raises: the captions the table carries are 'Client' and '' — the second column is nameless,
      // so nothing binds Amount to it.
      var sheet = Mixed(new object?[,]
      {
        { "Client", 42 },
        { "Acme", 10m },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table<Money>().Map(sheet));

      Assert.Equal(
        "no column binds Money.Amount; the table's captions are 'Client', ''. "
        + "Bind one with Column(t => t.Amount, \"…\") or drop it with Ignore(t => t.Amount)",
        Problem(failure));
    }

    /// <summary>The record the bind pin above binds through; its Amount column is the one left unbound.</summary>
    public record Money(string Client, decimal Amount);
  }
}
