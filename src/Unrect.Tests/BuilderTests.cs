using System.Linq;

using Unrect.Array;
using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.RegionBuilderFactory;
using static Unrect.Strategies.AreaStrategies;
using static Unrect.Strategies.OffsetStrategies;

namespace Unrect.Tests
{
  /// <summary>
  /// Builders turn a declared shape into a region tree. These tests are about where each subregion
  /// lands: a stack consumes space along its own axis only, a builder's offset and area are applied
  /// exactly once, and a shape that asks for more space than exists fails as an out-of-bounds error.
  /// </summary>
  public class BuilderTests
  {
    // A 4-wide, 4-tall grid whose cell value is (row * 10 + column): every assertion below reads as
    // a coordinate, so a misplaced subregion names its own actual position in the failure message.
    private static ISpace CoordinateGrid()
    {
      var values = new int[4, 4];

      for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
          values[row, column] = row * 10 + column;

      return ArraySpace.Create(values);
    }

    private static void AssertRegion(IRegion region, int topLeftValue, int width, int height)
    {
      Assert.Equal(width, region.Space.Area.Size.Width);
      Assert.Equal(height, region.Space.Area.Size.Height);
      Assert.Equal(topLeftValue, region.Space[0, 0].GetInt());
    }

    // --- Vertical stacking ------------------------------------------------------------------------

    [Fact]
    public void Vertical_StacksTwoSubregionsTopToBottom()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 3)))
        .Build(CoordinateGrid());

      AssertRegion(region, 0, 4, 4);
      AssertRegion(region.Subregion1, 0, 4, 1);
      AssertRegion(region.Subregion2, 10, 4, 3);
    }

    [Fact]
    public void Vertical_StacksThreeSubregionsTopToBottom()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 2)))
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion1, 0, 4, 1);
      AssertRegion(region.Subregion2, 10, 4, 1);
      AssertRegion(region.Subregion3, 20, 4, 2);
    }

    [Fact]
    public void Vertical_LetsTheLastSubregionTakeTheRemainder()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 2)),
        Builder())
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion2, 20, 4, 2);
    }

    [Fact]
    public void Vertical_ConsumesASubregionOffsetOnTheVerticalAxisOnly()
    {
      // The first subregion is inset one column and one row. Only the row is consumed: the second
      // subregion starts back at column 0.
      var region = Vertical(
        Builder(ExplicitOffset(1, 1), ExplicitArea(2, 1)),
        Builder())
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion1, 11, 2, 1);
      AssertRegion(region.Subregion2, 20, 4, 2);
    }

    [Fact]
    public void Vertical_AppliesAStrategyDrivenOffsetPerSubregion()
    {
      var space = ArraySpace.Create(
        new[,]
        {
          { 1, 0 },
          { 0, 0 },
          { 0, 0 },
          { 2, 0 },
        },
        isBlank: v => v == 0);

      var region = Vertical(
        Builder(ExplicitArea(2, 1)),
        Builder(SkipBlankRows(), ExplicitArea(2, 1)))
        .Build(space);

      Assert.Equal(1, region.Subregion1.Space[0, 0].GetInt());
      Assert.Equal(2, region.Subregion2.Space[0, 0].GetInt());
    }

    // --- Horizontal stacking ----------------------------------------------------------------------

    [Fact]
    public void Horizontal_StacksTwoSubregionsLeftToRight()
    {
      var region = Horizontal(
        Builder(ExplicitArea(1, 4)),
        Builder(ExplicitArea(3, 4)))
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion1, 0, 1, 4);
      AssertRegion(region.Subregion2, 1, 3, 4);
    }

    [Fact]
    public void Horizontal_StacksThreeSubregionsLeftToRight()
    {
      var region = Horizontal(
        Builder(ExplicitArea(1, 4)),
        Builder(ExplicitArea(1, 4)),
        Builder(ExplicitArea(2, 4)))
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion1, 0, 1, 4);
      AssertRegion(region.Subregion2, 1, 1, 4);
      AssertRegion(region.Subregion3, 2, 2, 4);
    }

    [Fact]
    public void Horizontal_ConsumesASubregionOffsetOnTheHorizontalAxisOnly()
    {
      // The first subregion is inset one column and one row. Only the column is consumed: the second
      // subregion starts back at row 0.
      var region = Horizontal(
        Builder(ExplicitOffset(1, 1), ExplicitArea(1, 2)),
        Builder())
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion1, 11, 1, 2);
      AssertRegion(region.Subregion2, 2, 2, 4);
    }

    // --- Nesting ----------------------------------------------------------------------------------

    [Fact]
    public void Stack_AppliesTheStrategiesOfANestedStackBuilder()
    {
      // A nested stack's own offset and area are applied by its parent, so the inner stack sees only
      // the space the outer stack allotted it.
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Vertical(
          ExplicitOffset(1, 1),
          ExplicitArea(2, 2),
          Builder(ExplicitArea(2, 1)),
          Builder(ExplicitArea(2, 1))))
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion2, 21, 2, 2);
      AssertRegion(region.Subregion2.Subregion1, 21, 2, 1);
      AssertRegion(region.Subregion2.Subregion2, 31, 2, 1);
    }

    // --- RegionBuilder1 ---------------------------------------------------------------------------

    [Fact]
    public void Builder1_AppliesItsSubregionOffsetAndAreaExactlyOnce()
    {
      // The strategies belong to the *subregion* builder: they say where the subregion sits inside
      // the space the Region1 builder was handed.
      //
      // Regression: the offset was once applied twice — once to derive the available space and again
      // when slicing the area out of it — which would land this subregion on 22 instead of 11.
      var region = Builder(Builder(1, 1, 2, 2)).Build(CoordinateGrid());

      AssertRegion(region, 0, 4, 4);
      AssertRegion(region.Subregion1, 11, 2, 2);
    }

    [Fact]
    public void Builder1_OwnStrategiesPositionItWithinItsParent()
    {
      // The offset/area passed alongside a subregion builder configure the Region1 builder itself,
      // and a builder's own strategies are applied by its parent. Here the outer stack places the
      // Region1 region at (1, 2)-(2, 3); its subregion then fills what it was given.
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2), Builder()))
        .Build(CoordinateGrid());

      AssertRegion(region.Subregion2, 21, 2, 2);
      AssertRegion(region.Subregion2.Subregion1, 21, 2, 2);
    }

    [Fact]
    public void Builder1_WithAnOversizedOffset_ThrowsOutOfBounds()
    {
      // Not ArgumentOutOfRangeException: an oversized offset used to underflow the remaining-size
      // arithmetic before the bounds check ran.
      Assert.Throws<OutOfBoundsException>(() =>
        Builder(Builder(ExplicitOffset(9, 0), ExplicitArea(1, 1))).Build(CoordinateGrid()));

      Assert.Throws<OutOfBoundsException>(() =>
        Builder(Builder(ExplicitOffset(0, 9), ExplicitArea(1, 1))).Build(CoordinateGrid()));
    }

    [Fact]
    public void Builder1_WithAnOversizedArea_ThrowsOutOfBounds()
    {
      Assert.Throws<OutOfBoundsException>(() =>
        Builder(Builder(ExplicitOffset(0, 0), ExplicitArea(9, 1))).Build(CoordinateGrid()));

      Assert.Throws<OutOfBoundsException>(() =>
        Builder(Builder(ExplicitOffset(0, 0), ExplicitArea(1, 9))).Build(CoordinateGrid()));
    }

    [Fact]
    public void Builder1_WhenTheOffsetAndAreaTogetherOverflow_ThrowsOutOfBounds()
    {
      // Each of these fits on its own; together they do not.
      Assert.Throws<OutOfBoundsException>(() =>
        Builder(Builder(ExplicitOffset(2, 0), ExplicitArea(3, 1))).Build(CoordinateGrid()));
    }

    // --- Stacks that do not fit -------------------------------------------------------------------

    [Fact]
    public void Stack_WhenASubregionDoesNotFit_ThrowsOutOfBounds()
    {
      Assert.Throws<OutOfBoundsException>(() =>
        Vertical(Builder(ExplicitArea(4, 3)), Builder(ExplicitArea(4, 3))).Build(CoordinateGrid()));
    }

    [Fact]
    public void Stack_WithAnExplicitRowCountBeyondTheSpace_ThrowsOutOfBounds()
    {
      var space = ArraySpace.Create(new[,] { { 1, 2 }, { 3, 4 } });

      Assert.Throws<OutOfBoundsException>(() =>
        Vertical(Builder(RowStrategies.TakeRows(9).TakeColumnsWhileAnyValue()), Builder()).Build(space));
    }

    // --- Map --------------------------------------------------------------------------------------

    [Fact]
    public void Map_OnRegion1_PassesTheSubregionThenTheWholeRegion()
    {
      var region = Builder(Builder(1, 1, 2, 2)).Build(CoordinateGrid());

      var result = region.Map((subregion, whole) =>
        (Sub: subregion.Space[0, 0].GetInt(), Whole: whole.Space[0, 0].GetInt()));

      Assert.Equal(11, result.Sub);
      Assert.Equal(0, result.Whole);
    }

    [Fact]
    public void Map_OnRegion2_PassesSubregionsInDeclarationOrder()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 3)))
        .Build(CoordinateGrid());

      var result = region.Map((first, second, whole) => new[]
      {
        first.Space[0, 0].GetInt(),
        second.Space[0, 0].GetInt(),
        whole.Space.Area.Size.Height,
      });

      Assert.Equal(new[] { 0, 10, 4 }, result);
    }

    [Fact]
    public void Map_OnRegion3_PassesSubregionsInDeclarationOrder()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 2)))
        .Build(CoordinateGrid());

      var result = region.Map((first, second, third, whole) => new[]
      {
        first.Space[0, 0].GetInt(),
        second.Space[0, 0].GetInt(),
        third.Space[0, 0].GetInt(),
        whole.Space.Area.Size.Height,
      });

      Assert.Equal(new[] { 0, 10, 20, 4 }, result);
    }

    // --- Region projections -----------------------------------------------------------------------

    [Fact]
    public void GetSubregions_YieldsSubregionsInDeclarationOrder()
    {
      var region = Vertical(
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 1)),
        Builder(ExplicitArea(4, 2)))
        .Build(CoordinateGrid());

      Assert.Equal(
        new[] { 0, 10, 20 },
        region.GetSubregions().Select(r => r.Space[0, 0].GetInt()).ToArray());
    }

    [Fact]
    public void GetSubregions_OnALeafRegion_IsEmpty()
    {
      Assert.Empty(Builder().Build(CoordinateGrid()).GetSubregions());
    }

    [Fact]
    public void Rows_EnumeratesRowsLeftToRight()
    {
      var region = Builder(Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2))).Build(CoordinateGrid()).Subregion1;

      var rows = region.Rows().Select(r => r.Select(v => v.GetInt()).ToArray()).ToArray();

      Assert.Equal(2, rows.Length);
      Assert.Equal(new[] { 11, 12 }, rows[0]);
      Assert.Equal(new[] { 21, 22 }, rows[1]);
    }

    [Fact]
    public void Columns_EnumeratesColumnsTopToBottom()
    {
      var region = Builder(Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2))).Build(CoordinateGrid()).Subregion1;

      var columns = region.Columns().Select(c => c.Select(v => v.GetInt()).ToArray()).ToArray();

      Assert.Equal(2, columns.Length);
      Assert.Equal(new[] { 11, 21 }, columns[0]);
      Assert.Equal(new[] { 12, 22 }, columns[1]);
    }

    [Fact]
    public void RowOrderEnumerable_WalksRowByRow()
    {
      var region = Builder(Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2))).Build(CoordinateGrid()).Subregion1;

      Assert.Equal(new[] { 11, 12, 21, 22 }, region.RowOrderEnumerable().Select(v => v.GetInt()).ToArray());
    }

    [Fact]
    public void ColumnOrderEnumerable_WalksColumnByColumn()
    {
      var region = Builder(Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2))).Build(CoordinateGrid()).Subregion1;

      Assert.Equal(new[] { 11, 21, 12, 22 }, region.ColumnOrderEnumerable().Select(v => v.GetInt()).ToArray());
    }

    [Fact]
    public void ToArray_ProducesARowMajorArray()
    {
      var region = Builder(Builder(ExplicitOffset(1, 1), ExplicitArea(2, 2))).Build(CoordinateGrid()).Subregion1;

      var values = region.ToArray();

      Assert.Equal(2, values.GetLength(0));
      Assert.Equal(2, values.GetLength(1));
      Assert.Equal(11, values[0, 0].GetInt());
      Assert.Equal(12, values[0, 1].GetInt());
      Assert.Equal(21, values[1, 0].GetInt());
      Assert.Equal(22, values[1, 1].GetInt());
    }
  }
}
