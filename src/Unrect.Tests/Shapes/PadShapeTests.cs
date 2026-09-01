using System;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// <c>Padded</c> insets a shape's extent and applies the shape to what is left, while still
  /// consuming the whole of it — a block of numbers with a border of labels around it. Padding
  /// shrinks the inside where a movement shifts the outside, which is why it is a wrapper shape
  /// rather than a placement, and why the two compose without interfering.
  /// </summary>
  public class PadShapeTests
  {
    // Values are (row * 10 + column + 1): 1 2 3 4 / 11 12 13 14 / 21 22 23 24 (/ 31 ... / 41 ...).
    private static ISpace CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    private static IShape<(int Width, int Height, int TopLeft)> Extent()
      => Cells(b => (b.Width, b.Height, b[0, 0].GetInt()));

    // --- Inset arithmetic ------------------------------------------------------------------------

    [Fact]
    public void Padded_WithOneAmount_InsetsEverySide()
    {
      // A 4x3 extent inset by one on all four sides leaves the 2x1 middle, starting at B2.
      Assert.Equal((2, 1, 12), Extent().Padded(1).Map(CoordinateGrid()));
    }

    [Fact]
    public void Padded_WithTwoAmounts_InsetsHorizontallyThenVertically()
    {
      // One column off each side, no rows: the full height of the middle two columns.
      Assert.Equal((2, 3, 2), Extent().Padded(1, 0).Map(CoordinateGrid()));
    }

    [Fact]
    public void Padded_WithFourAmounts_InsetsEachSideIndependently()
    {
      // Left 1, top 2, right 0, bottom 0: the bottom-right 3x1 corner, starting at B3.
      Assert.Equal((3, 1, 22), Extent().Padded(1, 2, 0, 0).Map(CoordinateGrid()));
    }

    [Fact]
    public void Padded_WithZero_ChangesNothing()
    {
      Assert.Equal((4, 3, 1), Extent().Padded(0).Map(CoordinateGrid()));
      Assert.Equal((4, 3, 1), Extent().Padded(0, 0).Map(CoordinateGrid()));
      Assert.Equal((4, 3, 1), Extent().Padded(0, 0, 0, 0).Map(CoordinateGrid()));
    }

    // --- What a padded shape consumes ----------------------------------------------------------------

    [Fact]
    public void Padded_ConsumesWhatTheInnerShapeUsedPlusTheInsets()
    {
      // The inner Cell uses one cell of the 2x1 middle; the pad reports that plus its own border.
      var applied = Cell(v => v.GetInt()).Padded(1).Apply(CoordinateGrid());

      Assert.Equal(12, applied.Value);
      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void Padded_ConsumesTheWholeExtentWhenTheInnerShapeFillsIt()
    {
      var applied = Extent().Padded(1).Apply(CoordinateGrid());

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void Padded_WithAsymmetricInsets_AddsBothSidesOfEachAxis()
    {
      var applied = Cell(v => v.GetInt()).Padded(1, 2, 0, 0).Apply(CoordinateGrid());

      Assert.Equal(22, applied.Value);
      Assert.Equal(2, applied.Consumed.Width);    // inner 1 + left 1 + right 0
      Assert.Equal(3, applied.Consumed.Height);   // inner 1 + top 2 + bottom 0
    }

    [Fact]
    public void AFollowingSiblingStartsAfterThePadding()
    {
      // What the consumed size is for: the bottom inset is real space, so the next child clears it.
      var (padded, next) = Vertical(
        Cell(v => v.GetInt()).Padded(1),
        Cell(v => v.GetInt())).Map(CoordinateGrid(height: 5));

      Assert.Equal(12, padded);
      Assert.Equal(31, next);
    }

    // --- Composition with movement ---------------------------------------------------------------------

    [Fact]
    public void PaddingTheOutsideAndMovingTheOutsideCompose()
    {
      // The pad's own placement moves the padded region; the inset then applies within it.
      var applied = Cell(v => v.GetInt()).Padded(1).Down(1).Apply(CoordinateGrid(height: 5));

      Assert.Equal(22, applied.Value);
      Assert.Equal(1, applied.Offset.Size.Height);
    }

    [Fact]
    public void AMovementInsideThePaddingIsRelativeToTheInsetExtent()
    {
      // Padding shrinks the inside: the inner shape's own offset counts from the inset origin.
      Assert.Equal(22, Cell(v => v.GetInt()).Down(1).Padded(1).Map(CoordinateGrid(height: 5)));
    }

    // --- Insets that do not fit ---------------------------------------------------------------------------

    [Fact]
    public void AnInsetLargerThanTheExtent_Throws()
    {
      var failure = Assert.Throws<ShapeException>(() => Extent().Padded(5).Map(CoordinateGrid()));

      Assert.Contains("a padding of 5 left, 5 top, 5 right, 5 bottom does not fit an extent of 4x3", failure.Message);
    }

    [Fact]
    public void AnInsetLargerThanTheExtent_BlamesThePaddingItself()
    {
      // An unnamed pad is transparent, so without blaming itself explicitly the enclosing shape
      // would be named for a failure that is the padding's own.
      var failure = Assert.Throws<ShapeException>(() => Extent().Padded(0, 9).Map(CoordinateGrid()));

      Assert.Equal("Padded", failure.Subject);
      Assert.Contains("does not fit an extent of 4x3", failure.Message);
    }

    // --- Transparency -------------------------------------------------------------------------------------

    [Fact]
    public void AnUnnamedPadContributesNoPathSegment()
    {
      var padded = Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).Padded(1).Map(CoordinateGrid()));
      var plain = Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).Map(CoordinateGrid()));

      Assert.Equal("Cell", padded.Path);
      Assert.Equal(plain.Path, padded.Path);
      Assert.DoesNotContain("Padded", padded.Path);
    }

    [Fact]
    public void ANamedPadContributesAPathSegment()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).Padded(1).Named("inner block").Map(CoordinateGrid()));

      Assert.Equal("'inner block' -> Cell", failure.Path);
    }

    [Fact]
    public void OnlyAnUnnamedPadIsTransparent()
    {
      Assert.True(Cell(v => v.GetInt()).Padded(1).IsTransparent);
      Assert.False(Cell(v => v.GetInt()).Padded(1).Named("named").IsTransparent);
    }

    // --- Inspection ------------------------------------------------------------------------------------------

    [Fact]
    public void APadDescribesItselfAndExposesTheShapeItWraps()
    {
      var inner = Cell(v => v.GetInt()).Named("inner");

      var padded = inner.Padded(1);

      Assert.Equal("Padded", padded.Description);
      Assert.Same(inner, Assert.Single(padded.Children));
    }

    // --- Argument guards --------------------------------------------------------------------------------------

    [Fact]
    public void Padded_WithOneAmount_RejectsANegative()
    {
      Assert.Equal("all", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(-1)).ParamName);
    }

    [Fact]
    public void Padded_WithTwoAmounts_BlamesTheAxisThatWasNegative()
    {
      Assert.Equal("horizontal", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(-1, 0)).ParamName);
      Assert.Equal("vertical", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(0, -1)).ParamName);
    }

    [Fact]
    public void Padded_WithFourAmounts_BlamesTheSideThatWasNegative()
    {
      Assert.Equal("left", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(-1, 0, 0, 0)).ParamName);
      Assert.Equal("top", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(0, -1, 0, 0)).ParamName);
      Assert.Equal("right", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(0, 0, -1, 0)).ParamName);
      Assert.Equal("bottom", Assert.Throws<ArgumentOutOfRangeException>(() => Extent().Padded(0, 0, 0, -1)).ParamName);
    }

    [Fact]
    public void Padded_RejectsANullShape()
    {
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Padded(1)).ParamName);
    }

    // --- Error locations -------------------------------------------------------------------------

    [Fact]
    public void AFailureInsideThePadding_ReportsTheAbsoluteCellLocation()
    {
      // The padded Cell lands on C2 (column 3, row 2, 1-based) — the same cell Down(1).Right(2)
      // reaches. Padding is transparent in the path but must still advance the coordinates.
      var padded = Assert.Throws<ShapeException>(
        () => Cell(v => v.GetString()).Padded(2, 1, 0, 0).Map(CoordinateGrid()));
      var moved = Assert.Throws<ShapeException>(
        () => Cell(v => v.GetString()).Down(1).Right(2).Map(CoordinateGrid()));

      Assert.Equal(3, padded.Location.Column);
      Assert.Equal(2, padded.Location.Row);
      Assert.Equal("C2", padded.Location.A1);
      Assert.Equal(moved.Location.A1, padded.Location.A1);
    }
  }
}
