using System;

using Unrect.Core;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// <see cref="GridSpace"/> is the reference adapter: it fixes the index orientation every other
  /// space must honour (backing storage is [row, column]; the space indexer is [column, row]), and
  /// its <c>Create</c> overloads are where blankness is decided, at adaptation time.
  /// </summary>
  public class GridSpaceTests
  {
    // A 3-wide, 2-tall grid. Backing storage is row-major, so the outer initializer is rows.
    private static GridSpace TextGrid() =>
      new GridSpace(new[,]
      {
        { CellValue.Of("a"), CellValue.Of("b"), CellValue.Of("c") },
        { CellValue.Of("d"), CellValue.Of("e"), CellValue.Of("f") },
      });

    // A 4-wide, 4-tall grid whose cell value is (row * 10 + column), so a misread coordinate is
    // immediately obvious in the failure message.
    private static GridSpace CoordinateGrid()
    {
      var values = new int[4, 4];

      for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
          values[row, column] = row * 10 + column;

      return GridSpace.Create(values);
    }

    // --- Orientation ----------------------------------------------------------------------------

    [Fact]
    public void Area_TakesWidthFromTheSecondArrayDimensionAndHeightFromTheFirst()
    {
      var space = TextGrid();

      Assert.Equal(3, space.Area.Size.Width);
      Assert.Equal(2, space.Area.Size.Height);
    }

    [Fact]
    public void Indexer_IsColumnThenRow()
    {
      var space = TextGrid();

      Assert.Equal("a", space[0, 0].GetString());
      Assert.Equal("b", space[1, 0].GetString());
      Assert.Equal("c", space[2, 0].GetString());
      Assert.Equal("d", space[0, 1].GetString());
      Assert.Equal("f", space[2, 1].GetString());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 2)]
    public void Indexer_OutsideTheArea_Throws(int column, int row)
    {
      var space = TextGrid();

      Assert.Throws<IndexOutOfRangeException>(() => { _ = space[column, row]; });
    }

    // --- Adaptation and blankness ---------------------------------------------------------------

    [Fact]
    public void Create_WithBlankPredicate_MapsMatchingValuesToBlank()
    {
      var space = GridSpace.Create(new[,] { { 1, 0 }, { 0, 2 } }, isBlank: v => v == 0);

      Assert.Equal(1, space[0, 0].GetInt());
      Assert.True(space[1, 0].IsBlank);
      Assert.True(space[0, 1].IsBlank);
      Assert.Equal(2, space[1, 1].GetInt());
    }

    [Fact]
    public void Create_WithBlankPredicate_YieldsTheBlankValue()
    {
      var space = GridSpace.Create(new[,] { { 0 } }, isBlank: v => v == 0);

      // Was Assert.Same: CellValue is a value type, so blankness is a value, not an instance.
      Assert.Equal(CellValue.Blank, space[0, 0]);
    }

    [Fact]
    public void Create_WithoutBlankPredicate_TreatsEveryValueAsPresent()
    {
      var space = GridSpace.Create(new[,] { { 0, 1 } });

      Assert.True(space[0, 0].HasValue);
      Assert.Equal(0, space[0, 0].GetInt());
    }

    [Fact]
    public void Create_FromDoubles_MapsBlanksAndNumbers()
    {
      var space = GridSpace.Create(new[,] { { 1.5, double.NaN } }, isBlank: double.IsNaN);

      Assert.Equal(1.5, space[0, 0].GetDouble());
      Assert.True(space[1, 0].IsBlank);
    }

    [Fact]
    public void Create_FromStrings_TreatsNullAndEmptyAsBlank()
    {
      // This overload's blankness decision differs from CellValue's: CellValue.Of("") is Text, but
      // in a string grid an empty string means an empty cell.
      var space = GridSpace.Create(new string?[,] { { "x", "", null } });

      Assert.Equal("x", space[0, 0].GetString());
      Assert.True(space[1, 0].IsBlank);
      Assert.True(space[2, 0].IsBlank);
    }

    [Fact]
    public void Create_WithAMap_AdaptsArbitraryValues()
    {
      var space = GridSpace.Create(
        new[,] { { "yes", "no" } },
        v => v == "-" ? CellValue.Blank : CellValue.Of(v == "yes"));

      Assert.True(space[0, 0].GetBoolean());
      Assert.False(space[1, 0].GetBoolean());
    }

    // Two tests stood here — Create_WhenTheMapReturnsNull_Throws and
    // Constructor_WithANullCell_Throws — and both are gone because the thing they guarded against
    // no longer exists: CellValue is a value type, so a map cannot return null and an array cannot
    // hold a null cell. The states are unrepresentable rather than merely rejected. What the tests
    // were really protecting — that an unfilled cell is Blank, not something broken — is now
    // structural (default(CellValue) is Blank) and is covered by
    // Constructor_LeavesUnfilledCellsBlank below.

    [Fact]
    public void Constructor_LeavesUnfilledCellsBlank()
    {
      var values = new CellValue[1, 2];
      values[0, 0] = CellValue.Of(1);

      var space = new GridSpace(values);

      Assert.Equal(1, space[0, 0].GetInt());
      Assert.True(space[1, 0].IsBlank);
    }

    // --- Subspaces ------------------------------------------------------------------------------

    [Fact]
    public void GetSubspace_ReportsTheRequestedArea()
    {
      var subspace = CoordinateGrid().GetSubspace(new Offset(1, 2), new Area(2, 1));

      Assert.Equal(2, subspace.Area.Size.Width);
      Assert.Equal(1, subspace.Area.Size.Height);
    }

    [Fact]
    public void GetSubspace_ReadsFromTheOffsetOrigin()
    {
      var subspace = CoordinateGrid().GetSubspace(new Offset(1, 2), new Area(2, 2));

      Assert.Equal(21, subspace[0, 0].GetInt());
      Assert.Equal(22, subspace[1, 0].GetInt());
      Assert.Equal(31, subspace[0, 1].GetInt());
      Assert.Equal(32, subspace[1, 1].GetInt());
    }

    [Fact]
    public void GetSubspace_ComposesOffsets()
    {
      var subspace = CoordinateGrid()
        .GetSubspace(new Offset(1, 1), new Area(3, 3))
        .GetSubspace(new Offset(1, 1), new Area(2, 2));

      Assert.Equal(22, subspace[0, 0].GetInt());
      Assert.Equal(33, subspace[1, 1].GetInt());
    }

    [Fact]
    public void GetSubspace_WithOffsetOnly_TakesTheRemainder()
    {
      var subspace = CoordinateGrid().GetSubspace(new Offset(1, 1));

      Assert.Equal(3, subspace.Area.Size.Width);
      Assert.Equal(3, subspace.Area.Size.Height);
      Assert.Equal(11, subspace[0, 0].GetInt());
    }

    [Fact]
    public void GetSubspace_WithAreaOnly_StartsAtTheOrigin()
    {
      var subspace = CoordinateGrid().GetSubspace(new Area(2, 2));

      Assert.Equal(0, subspace[0, 0].GetInt());
      Assert.Equal(11, subspace[1, 1].GetInt());
    }

    [Fact]
    public void GetSubspace_BeyondTheSpace_Throws()
    {
      var space = CoordinateGrid();

      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(5, 1)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(1, 5)));
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(3, 0), new Area(2, 1)));
    }

    [Fact]
    public void GetSubspace_IsBoundedByItsParentNotByTheBackingArray()
    {
      // Columns 2..3 and rows 2..3 exist in the backing array, but not in this subspace: a subspace
      // is a space in its own right and cannot reach back out into its parent's siblings.
      var parent = CoordinateGrid().GetSubspace(new Offset(1, 1), new Area(2, 2));

      Assert.Throws<OutOfBoundsException>(() => parent.GetSubspace(new Offset(1, 1), new Area(2, 2)));
      Assert.Throws<OutOfBoundsException>(() => parent.GetSubspace(new Offset(0, 0), new Area(3, 3)));
    }

    [Fact]
    public void Indexer_OnASubspace_IsBoundedByTheSubspaceArea()
    {
      var subspace = CoordinateGrid().GetSubspace(new Offset(1, 1), new Area(2, 2));

      Assert.Throws<IndexOutOfRangeException>(() => { _ = subspace[2, 0]; });
      Assert.Throws<IndexOutOfRangeException>(() => { _ = subspace[0, 2]; });
    }
  }
}
