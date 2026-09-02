using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The third layout combinator. Where a flow flows — each child consuming space and moving a
  /// cursor on — an overlay places: every child is applied to the same extent and finds its own spot
  /// inside it. Children are independent, may overlap, and may read the same cells, because they
  /// read rather than paint.
  /// <para>
  /// It is declared with the same cursor and the same lambda as a flow; all that differs is what the
  /// composite does between <c>Next</c> calls, which is the composite's business and not the
  /// cursor's.
  /// </para>
  /// </summary>
  public class OverlayShapeTests
  {
    // Values are (row * 10 + column + 1) over 4 columns by 3 rows, so an assertion reads as a
    // coordinate: 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    // --- No flow ------------------------------------------------------------------------------------

    [Fact]
    public void Overlay_AppliesEveryChildToTheSameExtent()
    {
      // Two children, each placing itself independently inside the one extent.
      Assert.Equal("1|13", Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Down(1).Right(2))}").Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_DoesNotAdvanceACursorBetweenChildren()
    {
      // The distinguishing test, now cursor against cursor: the same two calls read one cell twice
      // in an overlay and two rows in a flow. Nothing but the composite differs.
      Assert.Equal("1|1", Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell())}").Map(CoordinateGrid()));
      Assert.Equal("1|11", VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}").Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_LetsChildrenOverlapAndReadTheSameCells()
    {
      // Deliberately no z-order and no occlusion: reading a cell twice is not a conflict.
      var read = Overlay(o =>
        $"{o.Next(Range(2, 2, b => b[1, 0].GetInt()))}|{o.Next(IntCell().Right(1))}|{o.Next(IntCell().Right(1))}");

      Assert.Equal("2|2|2", read.Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_ProjectsChildrenInDeclarationOrder()
    {
      var result = Overlay(o => new[] { o.Next(IntCell()), o.Next(IntCell().Right(1)), o.Next(IntCell().Right(2)) })
        .Map(CoordinateGrid());

      Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    // --- Bounding-box extent -------------------------------------------------------------------------

    [Fact]
    public void Overlay_SizesItselfToTheUnionOfItsChildrensFootprints()
    {
      // Per axis, the furthest any child reached: three columns across, two rows down.
      var applied = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Down(1).Right(2))}").Apply(CoordinateGrid());

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Overlay_SizesItselfToTheWidestChildNotTheLast()
    {
      // The last child reaches least far; the bounding box is still the widest reach of any of them.
      var applied = Overlay(o => $"{o.Next(IntCell().Right(3))}|{o.Next(IntCell())}").Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void AFollowingSiblingStartsAfterTheOverlaysBoundingBox()
    {
      // What the derived extent is for: the overlay occupies one row here, so the next child of the
      // enclosing flow begins on the second.
      var band = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(2))}");

      Assert.Equal("1|3/11", VerticalFlow(v => $"{v.Next(band)}/{v.Next(IntCell())}").Map(CoordinateGrid()));
    }

    [Fact]
    public void Sized_OverridesTheBoundingBox()
    {
      // Common for a header region, whose footprint on the sheet exceeds its sparse content.
      var band = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(2))}")
        .Sized(AreaStrategies.ExplicitArea(4, 2));

      var applied = band.Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);

      Assert.Equal("1|3/21", VerticalFlow(v => $"{v.Next(band)}/{v.Next(IntCell())}").Map(CoordinateGrid()));
    }

    // --- Misfit is a hard error -----------------------------------------------------------------------

    [Fact]
    public void Overlay_WhenAChildDoesNotFit_Throws()
    {
      // Consistent with a flow: an overlay places its children, it does not decide which of them
      // are optional.
      var block = Range(9, 9, b => b.Width);

      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell())}|{o.Next(block)}").Map(CoordinateGrid()));

      Assert.Contains("an extent of 9x9 does not fit here", failure.Message);
      Assert.Equal("Overlay -> 'block' (Range)", failure.Path);
    }

    [Fact]
    public void Overlay_WhenAChildsOffsetRunsOff_Throws()
    {
      Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(9))}").Map(CoordinateGrid()));
    }

    // --- Context and diagnostics ----------------------------------------------------------------------

    [Fact]
    public void AChildFailure_ReportsItsAbsolutePosition()
    {
      // Each child descends from the overlay's scope carrying its own offset, so a failure names
      // where the child actually landed rather than where the overlay starts.
      var title = Cell(v => v.GetString()).Down(1).Right(2);

      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell())}|{o.Next(title)}").Map(CoordinateGrid()));

      Assert.Equal("Overlay -> 'title' (Cell)", failure.Path);
      Assert.Equal("C2", failure.Location.A1);
      Assert.Equal(2, failure.Location.Row);
      Assert.Equal(3, failure.Location.Column);
    }

    [Fact]
    public void AChildFailure_IsReportedRelativeToAPlacedOverlayToo()
    {
      var title = Cell(v => v.GetString()).Right(1);

      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell())}|{o.Next(title)}").Down(1).Map(CoordinateGrid()));

      Assert.Equal("B2", failure.Location.A1);
    }

    // --- Inspection ------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayDescribesItselfAndSaysWhyItsChildrenAreMissing()
    {
      // Every cursor composite is opaque: what it declares is knowable only by running it, so an
      // empty Children would read as "leaf" to a renderer unless the marker said otherwise.
      var overlay = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(1))}");

      Assert.Equal("Overlay", overlay.Description);
      Assert.Empty(overlay.Children);
      Assert.False(overlay.IsTransparent);

      var marker = Assert.IsAssignableFrom<IOpaqueComposite>(overlay);

      Assert.Equal("declared by a cursor lambda; children are known only while it runs", marker.Reason);
    }

    [Fact]
    public void AnOverlayDerivesItsExtent()
    {
      Assert.Null(Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell())}").Placement.Area);
    }

    [Fact]
    public void AnOverlayIsAShapeAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0, 0 }, { 1, 2 } });

      var shape = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(1))}").AfterBlankRows().Named("header");

      Assert.Equal("1|2", shape.Map(space));
      Assert.Equal("header", shape.Name);
    }

    // --- Arity -------------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayHasNoArityLimit()
    {
      // Children are Next calls, so there is no tuple to run out of and no nesting to reach for.
      var shape = Overlay(o => string.Join(",", new[]
      {
        o.Next(IntCell()), o.Next(IntCell().Right(1)), o.Next(IntCell().Right(2)), o.Next(IntCell().Right(3)),
        o.Next(IntCell().Down(1)), o.Next(IntCell().Down(1).Right(1)), o.Next(IntCell().Down(1).Right(2)),
        o.Next(IntCell().Down(2)), o.Next(IntCell().Down(2).Right(1)), o.Next(IntCell().Down(2).Right(2)),
      }));

      Assert.Equal("1,2,3,4,11,12,13,21,22,23", shape.Map(CoordinateGrid()));
    }

    [Fact]
    public void OverlaysAndFlowsNest()
    {
      // The header band of a real report: two independent blocks sharing rows, above a table.
      var space = Mixed(new object?[,]
      {
        { "Acme Fund", null, null, "2026" },
        { null, null, null, null },
        { "Item", "Amount", null, null },
        { "Fees", 10, null, null },
      });

      var entity = Cell(v => v.GetString());
      var year = Cell(v => v.GetString()).Right(3);
      var items = TableRows(r => r["Amount"].GetInt());

      var shape = VerticalFlow(v =>
        $"{v.Next(Overlay(o => $"{o.Next(entity)}/{o.Next(year)}"))}|{string.Join(",", v.Next(items))}");

      Assert.Equal("Acme Fund/2026|10", shape.Map(space));
    }

    // --- Guards -------------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayThatDeclaresNothing_Fails()
    {
      // Same rule as a flow, told with the right noun: an overlay that declared nothing would match
      // anything and describe nothing.
      var failure = Assert.Throws<ShapeException>(() => Overlay(_ => 42).Map(CoordinateGrid()));

      Assert.Contains("an overlay must declare at least one shape; this one called Next zero times", failure.Message);
    }

    [Fact]
    public void AnOverlayThatDeclaresNothing_ResistsAToleranceBoundary()
    {
      Assert.Throws<ShapeException>(() => Overlay(_ => 42).Optional().Map(CoordinateGrid()));
    }

    [Fact]
    public void ANullChildIsReportedAtTheOverlaysOrigin()
    {
      // Every overlay child starts from the same origin, so there is no cursor position to report
      // one against — unlike a flow, where the hole is where the child would have gone.
      IShape<int>? missing = null;

      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell().Down(1))}|{o.Next(missing!)}").Map(CoordinateGrid()));

      Assert.Contains("a null shape was declared as child 2", failure.Message);
      Assert.Equal("A1", failure.Location.A1);
      Assert.Equal("Overlay", failure.Path);
    }

    [Fact]
    public void ANullChild_ResistsAToleranceBoundary()
    {
      IShape<int>? missing = null;

      Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(IntCell())}|{o.Next(missing!)}").Optional().Map(CoordinateGrid()));
    }

    [Fact]
    public void AnOverlayChildCarriesNoSiblingNote()
    {
      // The note explains a child failing on cells its predecessor declined to consume. An overlay
      // has no such relation — every child starts from the same origin whatever its neighbours did —
      // so the identical declaration is noted in a flow and silent here.
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      var absorbed = IntCell().Optional();
      var second = IntCell();

      var inOverlay = Assert.Throws<ShapeException>(() =>
        Overlay(o => $"{o.Next(absorbed)}|{o.Next(second)}").Map(space));

      var inFlow = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(absorbed)}|{v.Next(second)}").Map(space));

      Assert.DoesNotContain("note:", inOverlay.Message);
      Assert.Contains("note: the preceding sibling consumed nothing at this position", inFlow.Message);
    }

    [Fact]
    public void ACaptureNothingOverlayIsSafeToApplyToManySpacesAtOnce()
    {
      var shape = Overlay(o => $"{o.Next(IntCell())}|{o.Next(IntCell().Right(1))}");

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1, seed + 2 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = shape.Map(spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}|{index + 2}", results[index]);
    }
  }
}
