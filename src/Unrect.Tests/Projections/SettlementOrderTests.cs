using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// An outcome settles no later than the first structural commitment that depends on it. Three
  /// places a decision turns on an extent, and what settling first looks like from outside at each:
  /// <list type="number">
  /// <item><strong>Repeat</strong> — the run steps by what its item consumed, so an item that reads
  /// nothing of its extent still advances the run by the extent its rule discovered.</item>
  /// <item><strong>Flow</strong> — a sibling starts where its predecessor's extent ended, and never
  /// starts at all when that extent's rule failed.</item>
  /// <item><strong>Choice</strong> — the next alternative begins against a settled verdict, with the
  /// loser's rows behind the reading and the loser reduced to one Info.</item>
  /// </list>
  /// </summary>
  public class SettlementOrderTests
  {
    /// <summary>
    /// Two blocks of values with one blank row between them, so a repeat finds exactly two and a
    /// discovered extent has somewhere to stop.
    /// </summary>
    private static ISheetCells TwoBlocks() => Grid(new[,]
    {
      { 1, 2 },
      { 3, 4 },
      { 5, 6 },
      { 0, 0 },
      { 7, 8 },
      { 9, 10 },
    });

    /// <summary>
    /// A cell rule that breaks on the cell holding <paramref name="marker"/>. Content-based rather
    /// than call-count-based, so it breaks in the same place however many cells the reading asks for.
    /// </summary>
    private static Func<Point<ISheetCells>, bool> BreaksOn(int marker)
      => cell => cell.Kind() == CellKind.Number && cell.Integer() == marker
        ? throw new InvalidOperationException("no")
        : true;

    /// <summary>The third row of the first block: a scan that breaks here survives two rows first.</summary>
    private const int MarkerInTheThirdRow = 5;

    /// <summary>How many rows a scan has read when it breaks on <see cref="MarkerInTheThirdRow"/>.</summary>
    private const int RowsReadReachingTheBreak = 3;

    // --- Site 1: a repeat's item settles before collection ---------------------------------------

    [Fact]
    public void ARepeatAdvancesByTheSettledExtentEvenWhereTheItemReadsNothing()
    {
      // This item asks its extent for nothing at all, and the repetition still finds two occurrences
      // and consumes both blocks and the gap: the run steps by the extent the item's rule
      // discovered, whether or not the item read a cell of it.
      var item = Range(RowsWhileAnyValue(), _ => 0);

      var applied = VerticalRepeat(item, separatedBy: BlankRows()).Apply(TwoBlocks());

      Assert.Equal(2, applied.Value.Count);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(6, applied.Consumed.Height);
    }

    // --- Site 2: a flow child settles by the advance ----------------------------------------------

    /// <summary>Three rows of values, a blank one, and a tail — so a discovered band has a successor.</summary>
    private static ISheetCells BlockThenGapThenTail() => Mixed(new object?[,] { { 1 }, { 2 }, { 3 }, { null }, { 9 } });

    [Fact]
    public void AFlowChildSettlesByTheAdvance()
    {
      // The sibling reads 9, which is only reachable if the flow stepped by the three rows the first
      // child's scan discovered. The row count is the same statement from the other side: all five
      // rows are behind the reading by the time the sibling reads its cell — the band, the blank row
      // that ended it, and its own.
      var counter = new CountingSpace(BlockThenGapThenTail());
      var rowsReadInsideTheSibling = -1;

      var flow = VerticalFlow(v =>
      {
        v.Next(Range(RowsWhileAnyValue(), _ => 0).Named("body"));

        var afterBlankRows = v.Next(AfterBlankRows().Of(Point().Select(point =>
        {
          rowsReadInsideTheSibling = counter.RowsTouched;

          return point.Integer();
        })).Named("next"));

        return afterBlankRows;
      });

      Assert.Equal(9, flow.Map(counter));
      Assert.Equal(5, rowsReadInsideTheSibling);
    }

    [Fact]
    public void AFlowChildWhoseExtentBreaksSettlesBeforeTheNextSiblingRuns()
    {
      // The failure is the first child's rule breaking on its third row, and it arrives before the
      // cursor moves: a sibling that had run would have read cells inside a band whose size was
      // about to turn out not to exist.
      var siblingRuns = 0;

      var flow = VerticalFlow(v =>
      {
        v.Next(Range(RowsWhileAny(BreaksOn(MarkerInTheThirdRow)), _ => 0).Named("body"));

        var pointSlot = v.Next(Point().Select(point =>
        {
          siblingRuns++;

          return point.Integer();
        }).Named("next"));

        return pointSlot;
      });

      var failure = Assert.Throws<ProjectionException>(() => flow.Map(TwoBlocks()));

      Assert.Equal(0, siblingRuns);
      Assert.Equal("VerticalFlow -> 'body' (Range)", failure.Path);
    }

    // --- Site 3: a rejected alternative settles at the catch --------------------------------------

    [Fact]
    public void ARejectedAlternativeSettlesBeforeTheNextAlternativeBegins()
    {
      // The losing alternative breaks while being placed and never projects; the winner begins
      // against a settled verdict, and the three rows its predecessor's scan needed are behind the
      // reading when it does.
      var counter = new CountingSpace(TwoBlocks());
      var rowsReadWhenTheWinnerBegan = -1;

      var choice = Choice(
        Range(RowsWhileAny(BreaksOn(MarkerInTheThirdRow)), _ => 0).Named("first"),
        Range(WholeExtent(), _ =>
        {
          rowsReadWhenTheWinnerBegan = counter.RowsTouched;

          return 42;
        }).Named("second"));

      var result = choice.MapWithDiagnostics(counter);

      Assert.Equal(42, result.Value);
      Assert.Equal(RowsReadReachingTheBreak, rowsReadWhenTheWinnerBegan);

      // Settled, and settled as a rejection: the loser is one Info, which is what a verdict reached
      // inside the catch looks like from outside it.
      var note = Assert.Single(result.Diagnostics);
      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("alternative 1 ('first')", note.Message);
    }
  }
}
