using System;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The uniform settlement law (v0.4 §5.6), stated verbatim:
  /// <para>
  /// <strong>an outcome settles no later than the first structural commitment that depends on it.</strong>
  /// </para>
  /// <para>
  /// It is what makes laziness statable at all. Deferral is not a global mode — the evaluator is
  /// neither eager nor lazy as a policy — it is a licence to leave an extent unresolved for exactly
  /// as long as no decision turns on it. The law names the three places a decision does turn on one,
  /// and this file pins each:
  /// </para>
  /// <list type="number">
  /// <item><strong>Repeat</strong> — an item's verdict settles before collection: the repeat must know
  /// whether the next occurrence exists, and how far it reaches, before it may keep it and move on.
  /// Enforced by <c>ProjectionEngine.Bind</c>'s second condition (a non-strict placement never
  /// defers), so a repeat's item is measured up front however it is declared.</item>
  /// <item><strong>Flow</strong> — a child settles by the advance: the band a following sibling gets
  /// is the previous child's, so a deferred failure may not outlive the cursor's step past it.</item>
  /// <item><strong>Choice</strong> — a rejected alternative settles at the catch: the next
  /// alternative may not begin against an extent whose verdict is still open.</item>
  /// </list>
  /// <para>
  /// <strong>What is pinned elsewhere, and not repeated here.</strong> <c>LazyForcingTests</c> is the
  /// cost model — which reads force and how many rows each costs — and its
  /// <c>AProjectionThatReadsNothingHasTouchedNothing</c> is the control that gives this law its
  /// content: <em>outside</em> a commitment site nothing settles early. <c>LazyErrorTimingTests</c>
  /// pins the failure IDENTITY across the two evaluation orders (rule 3), including
  /// <c>ARepeatsItemIsMeasuredBeforeItIsProjected</c>'s row counts and
  /// <c>AChoiceWhoseFirstAlternativeBreaksLateLeavesNothingBehind</c>'s rollback. What this file adds
  /// is the law as such: at each site, the observable consequence of settlement having happened
  /// BEFORE the commitment — a projection that never runs, a sibling that never runs, a scan already
  /// read to its end when the next branch starts.
  /// </para>
  /// </summary>
  public class SettlementLawTests
  {
    /// <summary>
    /// Two blocks of values with one blank row between them, so a repeat finds exactly two and a
    /// discovered extent has somewhere to stop.
    /// </summary>
    private static ISpace TwoBlocks() => Grid(new[,]
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
    /// than call-count-based, so it breaks in the same place however many cells the reading asks
    /// for — the idiom <c>LazyErrorTimingTests</c> established and for the same reason.
    /// </summary>
    private static Func<CellValue, bool> BreaksOn(int marker)
      => cell => cell.TryGetInt() == marker ? throw new InvalidOperationException("no") : true;

    /// <summary>
    /// The third row of the first block. A scan that breaks here survives two rows first, so the
    /// break is late enough that a deferred extent would not have reached it while being placed.
    /// </summary>
    private const int MarkerInTheThirdRow = 5;

    /// <summary>How many rows a scan has read when it breaks on <see cref="MarkerInTheThirdRow"/>.</summary>
    private const int RowsReadReachingTheBreak = 3;

    // --- Site 1: a repeat's item settles before collection ---------------------------------------

    [Fact]
    public void ARepeatSettlesItsItemsVerdictBeforeProjectingIt()
    {
      // The same declaration, read twice, and the counter is the law: the lone leaf's extent is
      // still open while its projection runs (1), and the repeat's is closed before its item is
      // handed anything to read (0). Nothing about the item changed — what changed is that a repeat
      // has a decision waiting on the answer.
      var projections = 0;
      var item = Range(RowsWhileAny(BreaksOn(MarkerInTheThirdRow)), _ => { projections++; return 0; }).Named("item");

      projections = 0;
      Assert.Throws<ProjectionException>(() => item.Map(TwoBlocks()));
      Assert.Equal(1, projections);

      projections = 0;
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(item, separatedBy: BlankRows()).Map(TwoBlocks()));
      Assert.Equal(0, projections);

      // And settling early costs nothing in the telling: it is still the placement's failure, said
      // in the same words, with the repeat's own segment in front of it.
      Assert.Equal("its area strategy threw InvalidOperationException: no", Problem(failure));
      Assert.Equal("VerticalRepeat[0] -> 'item' (Range)", failure.Path);
    }

    [Fact]
    public void ARepeatAdvancesByTheSettledExtentEvenWhereTheItemReadsNothing()
    {
      // The positive half. This item asks its extent for nothing at all, so there is no reading to
      // settle the bound — and the repetition still finds two occurrences and consumes both blocks
      // and the gap. The commitment the law names is exactly this one: the cursor cannot step by an
      // extent nobody has resolved.
      var item = Range(RowsWhileAnyValue(), _ => 0);

      var applied = VerticalRepeat(item, separatedBy: BlankRows()).Apply(TwoBlocks());

      Assert.Equal(2, applied.Value.Count);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(6, applied.Consumed.Height);
    }

    // --- Site 2: a flow child settles by the advance ----------------------------------------------

    /// <summary>Three rows of values, a blank one, and a tail — so a discovered band has a successor.</summary>
    private static ISpace BlockThenGapThenTail() => Mixed(new object?[,] { { 1 }, { 2 }, { 3 }, { null }, { 9 } });

    [Fact]
    public void AFlowChildSettlesByTheAdvance()
    {
      // The sibling reads 9, which is only reachable if the flow stepped by the three rows the first
      // child's scan discovered rather than by anything it was told up front. The row count is the
      // same statement from the other side: all five rows are behind the reading by the time the
      // sibling reads its cell — the band, the blank row that ended it, and its own.
      var counter = new CountingSpace(BlockThenGapThenTail());
      var rowsReadInsideTheSibling = -1;

      var flow = VerticalFlow(v =>
      {
        v.Next(Range(RowsWhileAnyValue(), _ => 0).Named("body"));

        return v.Next(Cell(c =>
        {
          rowsReadInsideTheSibling = counter.RowsTouched;

          return c.GetInt();
        }).AfterBlankRows().Named("next"));
      });

      Assert.Equal(9, flow.Map(counter));
      Assert.Equal(5, rowsReadInsideTheSibling);
    }

    [Fact]
    public void AFlowChildWhoseExtentBreaksSettlesBeforeTheNextSiblingRuns()
    {
      // The failure arrives from a bound the engine forces after the child's own projection has
      // returned — later than any failure could arrive before laziness existed — and it still
      // arrives before the cursor moves. A sibling that had run would have read cells inside a band
      // whose size was about to turn out not to exist.
      var siblingRuns = 0;

      var flow = VerticalFlow(v =>
      {
        v.Next(Range(RowsWhileAny(BreaksOn(MarkerInTheThirdRow)), _ => 0).Named("body"));

        return v.Next(Cell(c =>
        {
          siblingRuns++;

          return c.GetInt();
        }).Named("next"));
      });

      var failure = Assert.Throws<ProjectionException>(() => flow.Map(TwoBlocks()));

      Assert.Equal(0, siblingRuns);
      Assert.Equal("VerticalFlow -> 'body' (Range)", failure.Path);
    }

    // --- Site 3: a rejected alternative settles at the catch --------------------------------------

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARejectedAlternativeSettlesBeforeTheNextAlternativeBegins(bool eager)
    {
      // Both evaluation orders, because the law is about the ORDER OF EVENTS and not about which
      // order the reading happened to run in. Deferred, the losing alternative is placed, projected,
      // and only then broken by the bound the engine forces; measured, it breaks while being placed
      // and never projects at all. Either way the winner begins against a settled verdict, and the
      // three rows its predecessor's scan needed are behind the reading when it does.
      var counter = new CountingSpace(TwoBlocks());
      var rowsReadWhenTheWinnerBegan = -1;

      var choice = Choice(
        Range(RowsWhileAny(BreaksOn(MarkerInTheThirdRow)), _ => 0).Named("first"),
        Range(WholeExtent(), _ =>
        {
          rowsReadWhenTheWinnerBegan = counter.RowsTouched;

          return 42;
        }).Named("second"));

      var result = Read(choice, counter, eager);

      Assert.Equal(42, result.Value);
      Assert.Equal(RowsReadReachingTheBreak, rowsReadWhenTheWinnerBegan);

      // Settled, and settled as a rejection: the loser is one Info, which is what a verdict reached
      // inside the catch looks like from outside it.
      var note = Assert.Single(result.Diagnostics);
      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("alternative 1 ('first')", note.Message);
    }

    private static MapResult<T> Read<T>(IProjection<T> projection, ISpace space, bool eager)
    {
      if (!eager)
        return projection.MapWithDiagnostics(space);

      using (ProjectionEngine.ForceEager())
        return projection.MapWithDiagnostics(space);
    }
  }
}
