using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;


namespace Unrect.Tests.Projections
{
  /// <summary>
  /// How a repetition ends, and what it says about it. A run ends when its item stops placing or
  /// stands still; whether the last item absorbed a failure is what tells the one ending worth a
  /// note — a tolerated item that cannot drive the run — from the ordinary one, where the data held
  /// no more of these. The guard decides; absorption only explains.
  /// </summary>
  public class RepetitionEndingTests
  {
    // --- The grids, and the declarations that meet them --------------------------------------------

    private static ICellSpace Numbers() => Ladder(3);


    private static ICellSpace Blank() => Grid(new int[2, 2]);


    private static ICellSpace TwoNamesThenANumber() => Mixed(new object?[,] { { "a" }, { "b" }, { 1 } });


    /// <summary>The discovered extent: full width, and as many leading rows as hold anything.</summary>
    private static IProjectionDefinition<ICellSpace, int> Rows() => Range(RowsWhileAnyIsNotBlank(), b => b.Height);

    /// <summary>A cell read as text — which is a failure over <see cref="Numbers"/>, and an absorbable one.</summary>
    private static IProjectionDefinition<ICellSpace, string> Title() => TextCell();

    // --- The repetition's exit reasons -------------------------------------------------------------

    /// <summary>
    /// The repeat's note, verbatim. Written out here rather than referenced from the production
    /// constant on purpose: a message a user reads is pinned by quoting it, or the pin only says the
    /// code equals itself.
    /// </summary>
    private const string EndedByTolerance =
      "the repetition ended at occurrence [2]: the item's failure was absorbed by a tolerance "
      + "boundary, and a tolerated item cannot drive a repetition — drop the boundary, or declare "
      + "atLeast: 0 if an empty run is the concern";

    [Fact]
    public void ARepetitionThatRanOutOfContentSaysNothing()
    {
      // The ordinary ending, and the control for the two below: the space ran out, which is not a
      // declaration smell and gets no commentary.
      var result = VerticalRepeat(IntCell()).MapWithDiagnostics(Numbers());

      Assert.Equal(new[] { 1, 2, 3 }, result.Value);
      Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ARepetitionEndedByAToleratedItemSaysSoExactlyOnce()
    {
      // The documented trap, now guided. The third occurrence's failure was absorbed, the attempt
      // was rolled back with the warning that came from it — so this Info is the only thing left
      // saying anything happened, and it is the whole of what is said.
      //
      // BOTH HALVES ARE PRESENT HERE, and that is why the note fires: the item was Absorbed AND it
      // stood still, since a bare Optional consumes nothing to absorb into. Drop either half and the
      // note goes: ARepetitionThatRanOutOfContentSaysNothing is a standstill without tolerance, and
      // AnAbsorbedItemThatStillConsumedItsExtentGoesOnRepeating is tolerance without a standstill.
      var item = Title().Optional();

      var result = VerticalRepeat(item).MapWithDiagnostics(TwoNamesThenANumber());

      Assert.Equal(new[] { "a", "b" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      var note = Assert.Single(result.Diagnostics, d => d.Message.Contains("absorbed"));

      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Equal("VerticalRepeat", note.Subject);
      Assert.Equal(EndedByTolerance, note.Message);
    }

    [Fact]
    public void ARepetitionEndedByAnEmptyItemSaysNothingBecauseEmptyIsNotAbsorbed()
    {
      // This run ends for the same arithmetic reason as the one above — an item that consumed
      // nothing — and says nothing, because the item looked and the data held none of these.
      var result = VerticalRepeat(Rows()).MapWithDiagnostics(Blank());

      Assert.Empty(result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));
    }

    [Fact]
    public void ARepeatOverAnAcrossZeroBandEndsBeforeAttemptingItsItemRatherThanTrippingTolerance()
    {
      // The across-axis guard, pinned on the case it was added for. A horizontal repeat over a band
      // with zero width has no across-extent to hand any occurrence, so the walk must END before the
      // item is attempted. Without the guard the item runs against a zero-across slice, its Optional
      // absorbs the failure, the productivity guard trips on the standstill, and a spurious Info
      // fires — an item nobody could have read is reported as a tolerated ending.
      var band = SheetGrid.Of(new CellValue[0, 3]);

      var horizontal = HorizontalRepeat(Text().Optional()).MapWithDiagnostics(band);

      Assert.Empty(horizontal.Value);
      Assert.DoesNotContain(horizontal.Diagnostics, d => d.Message.Contains("absorbed"));

      // The mirror across the axis is unaffected and stays so: a vertical repeat over a zero-HEIGHT
      // band ends the same quiet way, so the guard reads the same on both axes.
      var column = SheetGrid.Of(new CellValue[0, 1]);

      var vertical = VerticalRepeat(Text().Optional()).MapWithDiagnostics(column);

      Assert.Empty(vertical.Value);
      Assert.DoesNotContain(vertical.Diagnostics, d => d.Message.Contains("absorbed"));
    }

    [Fact]
    public void AtLeastStillRejectsARunThatCollectedTooFew()
    {
      // The occurrence count is what atLeast is about, and an ending explained is still an ending.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(Title().Optional(), atLeast: 1).Map(Numbers()));

      Assert.Equal("expected at least 1 occurrences but found 0", Problem(failure));
    }

    // --- The guard decides, absorption only explains ------------------------------------------------
    //
    // A tolerated item that still consumes rows goes on repeating: the numeric productivity guard is
    // the sole decider of when a run ends, and whether the last item absorbed only explains a stop
    // the guard made. An earlier rule that ended a run at the first absorption silently dropped
    // occurrences a declaration used to collect, which is what these pin against.

    /// <summary>
    /// A tolerated occurrence that still consumes a row: the boundary absorbs the failure, and the
    /// declared extent on the boundary — resolved before it can catch anything — is consumed in full
    /// whatever the boundary made of it. One row per occurrence, whether the row read or was
    /// tolerated.
    /// </summary>
    private static IProjectionDefinition<ICellSpace, int> ToleratedRow() => Sized(Extent(1, 1)).Of(Integer().Optional());

    [Fact]
    public void AnAbsorbedItemThatStillConsumedItsExtentGoesOnRepeating()
    {
      // Three rows of text under a declaration expecting numbers: every occurrence absorbs, every
      // occurrence yields the filler, and the run goes all the way to the end of the sheet because
      // the extent — not the presence — is what moves the cursor.
      var tolerated = ToleratedRow();

      var result = VerticalRepeat(tolerated).MapWithDiagnostics(Mixed(new object?[,] { { "x" }, { "y" }, { "z" } }));

      Assert.Equal(new[] { 0, 0, 0 }, result.Value);

      // One Warning per absorption, all of them kept: nothing was rolled back, because no attempt
      // was discarded. And no Info — the run did not end by tolerance, it ended by running out of
      // sheet, which is the ordinary ending.
      Assert.Equal(3, result.Diagnostics.Count);
      Assert.All(result.Diagnostics, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));
      Assert.All(result.Diagnostics, d => Assert.Equal("'tolerated'", d.Subject));
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));

      // And the run consumed the whole column: three occurrences collected, three rows.
      var applied = VerticalRepeat(tolerated).Apply(Mixed(new object?[,] { { "x" }, { "y" }, { "z" } }));

      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void ATolerantRepetitionCollectsReadAndAbsorbedOccurrencesAlike()
    {
      // The mixed run, which is the shape a real sheet has: a good row, a malformed one, a good one.
      // The tolerated row contributes its filler in the middle of the list rather than truncating it
      // — the whole reason a declaration puts a boundary on a repeat's item.
      var tolerated = ToleratedRow();

      var result = VerticalRepeat(tolerated).MapWithDiagnostics(Mixed(new object?[,] { { 1 }, { "y" }, { 3 } }));

      Assert.Equal(new[] { 1, 0, 3 }, result.Value);

      var warning = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
      Assert.Contains("expected Number", warning.Message);
    }

    [Fact]
    public void ThePaddedSpellingKeepsTheRunGoingForTheSameReason()
    {
      // The second route to a consuming absorption, and the pin that the law is about the extent
      // rather than about one modifier: padding is added outside the boundary, so an absorbed
      // occupant still occupies its padding. Two rows per occurrence — the inner reading contributes
      // nothing and the padding contributes the rest — so six rows make three occurrences.
      var padded = Integer().Optional().Padded(1);
      var sheet = Mixed(new object?[,]
      {
        { "a", "b", "c" },
        { "d", "e", "f" },
        { "g", "h", "i" },
        { "j", "k", "l" },
        { "m", "n", "o" },
        { "p", "q", "r" },
      });

      var result = VerticalRepeat(padded).MapWithDiagnostics(sheet);

      Assert.Equal(new[] { 0, 0, 0 }, result.Value);
      Assert.Equal(3, result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning));
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("absorbed"));

      // The lone Info is the sheet's third column, which this declaration never describes — a
      // statement about the space left over, and not about how the run ended.
      var note = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);

      Assert.Contains("were not described", note.Message);

      Assert.Equal(6, VerticalRepeat(padded).Apply(sheet).Consumed.Height);
    }
  }
}
