using System;

using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The composite-invariance law (v0.4 §8, ORC-01): a flow and an overlay whose children come to
  /// rest on the same cells are the same declaration. The flow divides its extent into bands and
  /// each child takes the next one; the overlay hands every child the whole extent and each places
  /// itself. Where those two arrangements produce the same footprints, nothing a caller can measure
  /// tells them apart.
  /// <para>
  /// <strong>The law holds at L2</strong> — one level stronger than the inventory claimed, which
  /// recorded it as L0 + L1 from the policy-gap experiments. The overlay's bounding box (per axis,
  /// the furthest any child reached) and the flow's band arithmetic (along the axis the sum of the
  /// advances, across it the widest child) agree wherever the footprints do, so the two spellings
  /// hand a following sibling the same starting position.
  /// </para>
  /// <para>
  /// <strong>It first fails at L3</strong>, on the one thing that ought to differ: the operator's
  /// own name, in the path and the subject of every message. And it is not a word law either — the
  /// last case shows a matched pair that agrees on everything above while reading the sheet
  /// backwards, which is L0's independence from L1–L3 made concrete rather than argued.
  /// </para>
  /// </summary>
  public class CompositeInvarianceLawTests
  {
    // The grid every case is read over: (row * 10 + column + 1) across 4 columns by 3 rows, so a
    // projected value reads as the coordinate it came from.
    //
    //   1  2  3  4
    //  11 12 13 14
    //  21 22 23 24

    /// <summary>
    /// A flow spelling and an overlay spelling built to land on the same cells. The overlay's
    /// children carry the offsets the flow's cursor would have supplied.
    /// </summary>
    private static (IProjection<string> Flow, IProjection<string> Overlay) Pair(string spelling) => spelling switch
    {
      // Two single cells side by side: the flow's second band is the second column, and the overlay
      // says so with .Right(1).
      "adjacent cells" => (
        HorizontalFlow(h => $"{h.Next(IntCell())}/{h.Next(IntCell())}"),
        Overlay(o => $"{o.Next(IntCell())}/{o.Next(IntCell().Right(1))}")),

      // A gap between them, which the two spellings express differently — the flow's child steps one
      // column into its own band, the overlay's steps two from the origin — and must absorb
      // identically.
      "a gap between them" => (
        HorizontalFlow(h => $"{h.Next(IntCell())}/{h.Next(IntCell().Right(1))}"),
        Overlay(o => $"{o.Next(IntCell())}/{o.Next(IntCell().Right(2))}")),

      // Children of different heights, so the consumed extent is not simply the first child's: the
      // flow takes the tallest across its axis, the overlay the furthest reach down.
      "children of different heights" => (
        HorizontalFlow(h => $"{h.Next(Column(2, s => s[0].GetInt()))}/{h.Next(IntCell())}"),
        Overlay(o => $"{o.Next(Column(2, s => s[0].GetInt()))}/{o.Next(IntCell().Right(1))}")),

      _ => throw new ArgumentOutOfRangeException(nameof(spelling), spelling, "No such spelling."),
    };

    // --- L2: the invariance itself -------------------------------------------------------------------

    [Theory]
    [InlineData("adjacent cells")]
    [InlineData("a gap between them")]
    [InlineData("children of different heights")]
    public void AFlowAndAnOverlayThatLandOnTheSameCellsAgreeOnValueAndConsumption(string spelling)
    {
      var (flow, overlay) = Pair(spelling);

      AssertL2(Observe(flow, CoordinateGrid()), Observe(overlay, CoordinateGrid()));
    }

    [Theory]
    [InlineData("adjacent cells")]
    [InlineData("a gap between them")]
    [InlineData("children of different heights")]
    public void TheyReadExactlyTheSameCells(string spelling)
    {
      // The committed stand-in for word-level evidence: two spellings that agree on the answer could
      // still have got there by reading different amounts of the sheet, and a differential value pin
      // would never notice.
      var (flow, overlay) = Pair(spelling);

      var byFlow = new CountingSpace(CoordinateGrid());
      var byOverlay = new CountingSpace(CoordinateGrid());

      flow.Apply(byFlow);
      overlay.Apply(byOverlay);

      // Not vacuous: something was read, and both read the same amount of it.
      Assert.NotEqual(0, byFlow.CellReads);
      Assert.Equal(byFlow.CellReads, byOverlay.CellReads);
      Assert.Equal(byFlow.RowsTouched, byOverlay.RowsTouched);
    }

    [Theory]
    [InlineData("adjacent cells")]
    [InlineData("a gap between them")]
    [InlineData("children of different heights")]
    public void TheyWalkTheSheetTheSameWay(string spelling)
    {
      // The coarser instrument, and the one a windowed reader cares about: how far down the sheet
      // the reading got, and how far back behind that it ever reached.
      var (flow, overlay) = Pair(spelling);

      var byFlow = new WatermarkSpace(CoordinateGrid());
      var byOverlay = new WatermarkSpace(CoordinateGrid());

      flow.Apply(byFlow);
      overlay.Apply(byOverlay);

      Assert.Equal(byFlow.HighWaterMark, byOverlay.HighWaterMark);
      Assert.Equal(byFlow.BackwardReach, byOverlay.BackwardReach);
    }

    // --- L3: where the invariance stops ----------------------------------------------------------------

    [Fact]
    public void TheInvarianceStopsAtTheOperatorNameInADiagnostic()
    {
      // A declaration is not anonymous, and it should not be: whichever spelling was written is the
      // one a reader has to go and find. Both directions asserted, so neither may start rendering as
      // the other.
      var (flow, overlay) = Pair("adjacent cells");

      var byFlow = Assert.Single(flow.MapWithDiagnostics(CoordinateGrid()).Diagnostics);
      var byOverlay = Assert.Single(overlay.MapWithDiagnostics(CoordinateGrid()).Diagnostics);

      Assert.Equal("HorizontalFlow", byFlow.Subject);
      Assert.Equal("HorizontalFlow", byFlow.Path);

      Assert.Equal("Overlay", byOverlay.Subject);
      Assert.Equal("Overlay", byOverlay.Path);

      // ...and the sentence beneath the two names is word for word the same.
      Assert.Equal(byFlow.Message, byOverlay.Message);
      Assert.Equal(byFlow.Severity, byOverlay.Severity);
      Assert.Equal(byFlow.Location.A1, byOverlay.Location.A1);
    }

    [Fact]
    public void TheInvarianceStopsAtTheOperatorNameInAFailurePath()
    {
      // The same difference where it matters most. The problem, the cell and the child's own label
      // are identical; only the enclosing segment says which composite was written.
      var inFlow = Assert.Throws<ProjectionException>(() =>
        HorizontalFlow(h => $"{h.Next(IntCell())}/{h.Next(Text())}").Map(CoordinateGrid()));

      var inOverlay = Assert.Throws<ProjectionException>(() =>
        Overlay(o => $"{o.Next(IntCell())}/{o.Next(Text().Right(1))}").Map(CoordinateGrid()));

      Assert.Equal("HorizontalFlow -> Text#2", inFlow.Path);
      Assert.Equal("Overlay -> Text#2", inOverlay.Path);

      Assert.Equal(inFlow.Problem, inOverlay.Problem);
      Assert.Equal(inFlow.Location.A1, inOverlay.Location.A1);
    }

    // --- L0 is an independent axis -----------------------------------------------------------------------

    [Fact]
    public void AnOverlayDeclaredOutOfOrderKeepsTheDenotationAndLosesTheWord()
    {
      // The sharpest statement of what composite invariance is not. This overlay declares the lower
      // cell first, so it reads row 2 and then reaches back to row 1 — a word no vertical flow can
      // produce, because a flow's cursor only ever goes forwards. Everything the law is about is
      // nevertheless identical.
      var flow = VerticalFlow(v => v.Next(IntCell()) + v.Next(IntCell()));
      var backwards = Overlay(o => o.Next(IntCell().Down(1)) + o.Next(IntCell()));

      AssertL2(Observe(flow, CoordinateGrid()), Observe(backwards, CoordinateGrid()));

      var byFlow = new WatermarkSpace(CoordinateGrid());
      var byOverlay = new WatermarkSpace(CoordinateGrid());

      flow.Apply(byFlow);
      backwards.Apply(byOverlay);

      // Same furthest row, and only one of them got there monotonically.
      Assert.Equal(byFlow.HighWaterMark, byOverlay.HighWaterMark);
      Assert.Equal(0, byFlow.BackwardReach);
      Assert.Equal(1, byOverlay.BackwardReach);
    }
  }
}
