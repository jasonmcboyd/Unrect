using System;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The third layout combinator. Where <c>Vertical</c> and <c>Horizontal</c> flow — each child
  /// consumes space and moves a cursor on — <c>Overlay</c> places: every child is applied to the
  /// same extent and finds its own spot inside it. Children are independent, may overlap, and may
  /// read the same cells, because they read rather than paint.
  /// </summary>
  public class OverlayShapeTests
  {
    // Values are (row * 10 + column + 1) over 4 columns by 3 rows, so an assertion reads as a
    // coordinate: 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    private static ISpace CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    // --- No flow ------------------------------------------------------------------------------------

    [Fact]
    public void Overlay_AppliesEveryChildToTheSameExtent()
    {
      // Two children, each placing itself independently inside the one extent.
      Assert.Equal((1, 13), Overlay(IntCell(), IntCell().Down(1).Right(2)).Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_DoesNotAdvanceACursorBetweenChildren()
    {
      // The distinguishing test: in a stack the second child starts where the first stopped; in an
      // overlay both children start from the overlay's own origin.
      Assert.Equal((1, 1), Overlay(IntCell(), IntCell()).Map(CoordinateGrid()));
      Assert.Equal((1, 11), Vertical(IntCell(), IntCell()).Map(CoordinateGrid()));
    }

    [Fact]
    public void Overlay_LetsChildrenOverlapAndReadTheSameCells()
    {
      // Deliberately no z-order and no occlusion: reading a cell twice is not a conflict.
      var (block, cell, sameCell) = Overlay(
        Cells(2, 2, b => b[1, 0].GetInt()),
        IntCell().Right(1),
        IntCell().Right(1)).Map(CoordinateGrid());

      Assert.Equal(2, block);
      Assert.Equal(2, cell);
      Assert.Equal(2, sameCell);
    }

    [Fact]
    public void Overlay_ProjectsChildrenInDeclarationOrder()
    {
      var result = Overlay(IntCell(), IntCell().Right(1), IntCell().Right(2))
        .Select((first, second, third) => new[] { first, second, third })
        .Map(CoordinateGrid());

      Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    // --- Bounding-box extent -------------------------------------------------------------------------

    [Fact]
    public void Overlay_SizesItselfToTheBoundingBoxOfItsChildren()
    {
      // Per axis, the furthest any child reached: three columns across, two rows down.
      var applied = Overlay(IntCell(), IntCell().Down(1).Right(2)).Apply(CoordinateGrid());

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Overlay_SizesItselfToTheWidestChildNotTheLast()
    {
      // The last child reaches least far; the bounding box is still the widest reach of any of them.
      var applied = Overlay(IntCell().Right(3), IntCell()).Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void AFollowingSiblingStartsAfterTheOverlaysBoundingBox()
    {
      // What the derived extent is actually for: the overlay occupies one row here, so the next
      // child of the enclosing stack begins on the second.
      var (_, next) = Vertical(
        Overlay(IntCell(), IntCell().Right(2)),
        IntCell()).Map(CoordinateGrid());

      Assert.Equal(11, next);
    }

    [Fact]
    public void Sized_OverridesTheBoundingBox()
    {
      // Common for a header region, whose footprint on the sheet exceeds its sparse content.
      var shape = Overlay(IntCell(), IntCell().Right(2)).Sized(AreaStrategies.ExplicitArea(4, 2));

      var applied = shape.Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);

      var (_, next) = Vertical(shape, IntCell()).Map(CoordinateGrid());
      Assert.Equal(21, next);
    }

    // --- Misfit is a hard error -----------------------------------------------------------------------

    [Fact]
    public void Overlay_WhenAChildDoesNotFit_Throws()
    {
      // Consistent with a stack: an overlay places its children, it does not decide which of them
      // are optional.
      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(IntCell(), Cells(9, 9, b => b.Width)).Map(CoordinateGrid()));

      Assert.Contains("an extent of 9x9 does not fit here", failure.Message);
      Assert.Equal("Overlay -> Cells(9, 9)", failure.Path);
    }

    [Fact]
    public void Overlay_WhenAChildsOffsetRunsOff_Throws()
    {
      Assert.Throws<ShapeException>(() => Overlay(IntCell(), IntCell().Right(9)).Map(CoordinateGrid()));
    }

    // --- Context and diagnostics ----------------------------------------------------------------------

    [Fact]
    public void AChildFailure_ReportsItsAbsolutePosition()
    {
      // Each child descends from the overlay's scope carrying its own offset, so a failure names
      // where the child actually landed rather than where the overlay starts.
      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(IntCell(), Cell(v => v.GetString()).Down(1).Right(2)).Map(CoordinateGrid()));

      Assert.Equal("Overlay -> Cell", failure.Path);
      Assert.Equal("C2", failure.Location.A1);
      Assert.Equal(2, failure.Location.Row);
      Assert.Equal(3, failure.Location.Column);
    }

    [Fact]
    public void AChildFailure_IsReportedRelativeToAPlacedOverlayToo()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Overlay(IntCell(), Cell(v => v.GetString()).Right(1)).Down(1).Map(CoordinateGrid()));

      Assert.Equal("B2", failure.Location.A1);
    }

    // --- Inspection ------------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayDescribesItselfAndExposesItsChildren()
    {
      var first = IntCell().Named("first");
      var second = IntCell().Named("second");

      var overlay = Overlay(first, second);

      Assert.Equal("Overlay", overlay.Description);
      Assert.Equal(2, overlay.Children.Count);
      Assert.Same(first, overlay.Children[0]);
      Assert.Same(second, overlay.Children[1]);
      Assert.False(overlay.IsTransparent);
    }

    [Fact]
    public void AnOverlayDerivesItsExtent()
    {
      Assert.Null(Overlay(IntCell(), IntCell()).Placement.Area);
    }

    [Fact]
    public void AnOverlayIsAShapeAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0, 0 }, { 1, 2 } });

      var shape = Overlay(IntCell(), IntCell().Right(1)).AfterBlankRows().Named("header");

      Assert.Equal((1, 2), shape.Map(space));
      Assert.Equal("header", shape.Name);
    }

    // --- Arity -------------------------------------------------------------------------------------------

    [Fact]
    public void Overlay_SupportsEightChildren()
    {
      var shape = Overlay(
        IntCell(), IntCell().Right(1), IntCell().Right(2), IntCell().Right(3),
        IntCell().Down(1), IntCell().Down(1).Right(1), IntCell().Down(2), IntCell().Down(2).Right(1));

      Assert.Equal((1, 2, 3, 4, 11, 12, 21, 22), shape.Map(CoordinateGrid()));
    }

    [Fact]
    public void BeyondEightChildren_NestAnOverlayInsideAnOverlay()
    {
      var shape = Overlay(
        Overlay(IntCell(), IntCell().Right(3)),
        IntCell().Down(2));

      var (pair, below) = shape.Map(CoordinateGrid());

      Assert.Equal((1, 4), pair);
      Assert.Equal(21, below);
    }

    [Fact]
    public void OverlaysAndStacksNest()
    {
      // The header band of a real report: two independent blocks sharing rows, above a table.
      var space = Mixed(new object?[,]
      {
        { "Acme Fund", null, null, "2026" },
        { null, null, null, null },
        { "Item", "Amount", null, null },
        { "Fees", 10, null, null },
      });

      var shape = Vertical(
        Overlay(
          Cell(v => v.GetString()).Named("entity"),
          Cell(v => v.GetString()).Right(3).Named("year")),
        TableRows(r => r["Amount"].GetInt()).Named("items"));

      var ((entity, year), amounts) = shape.Map(space);

      Assert.Equal("Acme Fund", entity);
      Assert.Equal("2026", year);
      Assert.Equal(new[] { 10 }, amounts);
    }

    // --- Construction guards -------------------------------------------------------------------------------

    [Fact]
    public void AnOverlayWithANullChild_IsRejectedAtConstruction()
    {
      Assert.Equal("second", Assert.Throws<ArgumentNullException>(() => Overlay(IntCell(), (IShape<int>)null!)).ParamName);
      Assert.Equal("first", Assert.Throws<ArgumentNullException>(() => Overlay((IShape<int>)null!, IntCell())).ParamName);
    }
  }
}
