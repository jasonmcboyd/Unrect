using System;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The leaves: Cell, Row, Column and Range. Each comes in a discovered form (the default area
  /// finds the extent), an explicit-count form, and a strategy form — and each validates the extent
  /// it was actually given, so a Column that has been sized two columns wide is an error rather than
  /// a silent half-read.
  /// </summary>
  public class PrimitiveShapeTests
  {
    // --- Cell ---------------------------------------------------------------------------------------

    [Fact]
    public void Cell_ProjectsTheSingleValueAtItsOrigin()
    {
      var space = Grid(new[,] { { 7, 8 }, { 9, 10 } });

      Assert.Equal(7, Cell(v => v.GetInt()).Map(space));
    }

    [Fact]
    public void Cell_ConsumesExactlyOneCell()
    {
      var applied = Cell(v => v.GetInt()).Apply(Grid(new[,] { { 7, 8 }, { 9, 10 } }));

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void Cell_SizedLargerThanOneCell_Throws()
    {
      var space = Grid(new[,] { { 7, 8 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetInt()).Sized(AreaStrategies.ExplicitArea(2, 1)).Map(space));

      Assert.Contains("a Cell must be exactly one cell; this one is 2x1", failure.Message);
    }

    // --- Row ----------------------------------------------------------------------------------------

    [Fact]
    public void Row_Discovered_TakesOneRowAsWideAsTheColumnsThatCarryValues()
    {
      var space = Grid(new[,]
      {
        { 1, 2, 0, 0 },
        { 3, 4, 5, 6 },   // the row below is wider, but a Row is one row and counts its own columns
      });

      Assert.Equal(new[] { 1, 2 }, Row(s => s.Select(v => v.GetInt()).ToArray()).Map(space));
    }

    [Fact]
    public void Row_WithAnExplicitWidth_TakesThatManyColumns()
    {
      var space = Grid(new[,] { { 1, 2, 0, 4 } });

      Assert.Equal(3, Row(3, s => s.Count).Map(space));
    }

    [Fact]
    public void Row_WithAColumnStrategy_CountsColumnsWithinItsOwnRow()
    {
      // The strategy sees the space already narrowed to one row: counting "columns where every cell
      // has a value" over the whole space would stop at zero because of the blank second row.
      var space = Grid(new[,]
      {
        { 1, 2, 3 },
        { 0, 0, 0 },
      });

      var shape = Row(ColumnStrategies.TakeColumnsWhileAll(v => v.HasValue), s => s.Count);

      Assert.Equal(3, shape.Map(space));
    }

    [Fact]
    public void Row_SizedTallerThanOneRow_Throws()
    {
      var space = Grid(new[,] { { 1, 2 }, { 3, 4 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Row(s => s.Count).Sized(AreaStrategies.ExplicitArea(2, 2)).Map(space));

      Assert.Contains("a Row must be exactly one row tall; this one is 2 rows tall", failure.Message);
    }

    // --- Column -------------------------------------------------------------------------------------

    [Fact]
    public void Column_Discovered_TakesOneColumnAsTallAsTheRowsThatCarryValues()
    {
      var space = Grid(new[,]
      {
        { 1, 9 },
        { 2, 9 },
        { 0, 9 },   // the neighbouring column continues, but a Column counts its own rows
      });

      Assert.Equal(new[] { 1, 2 }, Column(s => s.Select(v => v.GetInt()).ToArray()).Map(space));
    }

    [Fact]
    public void Column_WithAnExplicitHeight_TakesThatManyRows()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 4 } });

      Assert.Equal(4, Column(4, s => s.Count).Map(space));
    }

    [Fact]
    public void Column_WithARowStrategy_CountsRowsWithinItsOwnColumn()
    {
      var space = Grid(new[,]
      {
        { 1, 0 },
        { 2, 0 },
        { 3, 0 },
      });

      var shape = Column(RowStrategies.TakeRowsWhileAll(v => v.HasValue), s => s.Count);

      Assert.Equal(3, shape.Map(space));
    }

    [Fact]
    public void Column_SizedWiderThanOneColumn_Throws()
    {
      var space = Grid(new[,] { { 1, 2 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Column(s => s.Count).Sized(AreaStrategies.ExplicitArea(2, 1)).Map(space));

      Assert.Contains("a Column must be exactly one column wide; this one is 2 columns wide", failure.Message);
    }

    // --- Range --------------------------------------------------------------------------------------

    [Fact]
    public void Cells_Discovered_TakesTheMaximalValueBearingBlock()
    {
      var space = Grid(new[,]
      {
        { 1, 2, 0 },
        { 3, 4, 0 },
        { 0, 0, 0 },   // rows stop here
        { 5, 5, 5 },
      });

      Assert.Equal((2, 2), Range(b => (b.Width, b.Height)).Map(space));
    }

    [Fact]
    public void Cells_WithExplicitDimensions_TakesThatBlock()
    {
      var space = Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } });

      var values = Range(2, 2, b => new[] { b[0, 0].GetInt(), b[1, 0].GetInt(), b[0, 1].GetInt(), b[1, 1].GetInt() }).Map(space);

      Assert.Equal(new[] { 1, 2, 4, 5 }, values);
    }

    [Fact]
    public void Cells_WithAnAreaStrategy_UsesThatStrategy()
    {
      var space = Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

      var shape = Range(AreaStrategies.ExplicitArea(3, 1), b => (b.Width, b.Height));

      Assert.Equal((3, 1), shape.Map(space));
    }

    [Fact]
    public void Cells_ImposesNoShapeValidation()
    {
      // Unlike Row and Column, a block of any dimensions is legal — including an empty one.
      var space = Grid(new[,] { { 0, 0 }, { 0, 0 } });

      Assert.Equal((0, 0), Range(b => (b.Width, b.Height)).Map(space));
    }

    // --- CellStrip ------------------------------------------------------------------------------------

    [Fact]
    public void CellStrip_OfARow_IsIndexedLeftToRight()
    {
      var strip = Capture(Row(3, s => s), Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } }));

      Assert.Equal(3, strip.Count);
      Assert.Equal(1, strip[0].GetInt());
      Assert.Equal(3, strip[2].GetInt());
      Assert.Equal(new[] { 1, 2, 3 }, strip.Select(v => v.GetInt()).ToArray());
    }

    [Fact]
    public void CellStrip_OfAColumn_IsIndexedTopToBottom()
    {
      var strip = Capture(Column(3, s => s), Grid(new[,] { { 1, 9 }, { 2, 9 }, { 3, 9 } }));

      Assert.Equal(3, strip.Count);
      Assert.Equal(new[] { 1, 2, 3 }, strip.Select(v => v.GetInt()).ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void CellStrip_IndexedOutsideItsExtent_Throws(int index)
    {
      var strip = Capture(Row(3, s => s), Grid(new[,] { { 1, 2, 3 } }));

      Assert.Throws<ArgumentOutOfRangeException>(() => strip[index]);
    }

    [Fact]
    public void CellStrip_ExposesTheSpaceItReads()
    {
      var strip = Capture(Row(2, s => s), Grid(new[,] { { 1, 2, 3 } }));

      Assert.Equal(2, strip.Space.Area.Size.Width);
      Assert.Equal(1, strip.Space.Area.Size.Height);
    }

    // --- CellBlock ------------------------------------------------------------------------------------

    [Fact]
    public void CellBlock_IsIndexedByColumnThenRow()
    {
      var block = Capture(Range(b => b), Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } }));

      Assert.Equal(3, block.Width);
      Assert.Equal(2, block.Height);
      Assert.Equal(2, block[1, 0].GetInt());
      Assert.Equal(4, block[0, 1].GetInt());
    }

    [Fact]
    public void CellBlock_RowsAndColumnsEnumerateInOrder()
    {
      var block = Capture(Range(b => b), Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } }));

      Assert.Equal(new[] { 1, 2, 3 }, block.Row(0).Select(v => v.GetInt()).ToArray());
      Assert.Equal(new[] { 4, 5, 6 }, block.Row(1).Select(v => v.GetInt()).ToArray());
      Assert.Equal(new[] { 1, 4 }, block.Column(0).Select(v => v.GetInt()).ToArray());
      Assert.Equal(new[] { 3, 6 }, block.Column(2).Select(v => v.GetInt()).ToArray());

      Assert.Equal(2, block.Rows.Count);
      Assert.Equal(3, block.Columns.Count);
      Assert.Equal(new[] { 1, 2, 3 }, block.Rows[0].Select(v => v.GetInt()).ToArray());
      Assert.Equal(new[] { 2, 5 }, block.Columns[1].Select(v => v.GetInt()).ToArray());
    }

    [Fact]
    public void CellBlock_OutsideItsExtent_Throws()
    {
      var block = Capture(Range(b => b), Grid(new[,] { { 1, 2 }, { 3, 4 } }));

      Assert.Throws<ArgumentOutOfRangeException>(() => block[2, 0]);
      Assert.Throws<ArgumentOutOfRangeException>(() => block[0, 2]);
      Assert.Throws<ArgumentOutOfRangeException>(() => block[-1, 0]);
      Assert.Throws<ArgumentOutOfRangeException>(() => block.Row(2));
      Assert.Throws<ArgumentOutOfRangeException>(() => block.Column(2));
    }

    // --- Construction guards ---------------------------------------------------------------------------

    [Fact]
    public void Leaves_RejectANullProjection()
    {
      Assert.Throws<ArgumentNullException>(() => Cell<int>(null!));
      Assert.Throws<ArgumentNullException>(() => Row<int>(null!));
      Assert.Throws<ArgumentNullException>(() => Column<int>(null!));
      Assert.Throws<ArgumentNullException>(() => Range<int>(null!));
    }

    [Fact]
    public void Cells_RejectsANullAreaStrategy()
    {
      var failure = Assert.Throws<ArgumentNullException>(() => Range((IAreaStrategy)null!, b => b.Width));

      Assert.Equal("area", failure.ParamName);
    }

    /// <summary>
    /// Runs a shape purely to get hold of the view it was handed, so the view can be exercised
    /// outside a projection — where its own exceptions are not wrapped by the engine.
    /// </summary>
    private static TView Capture<TView>(IShape<TView> shape, ISpace space) => shape.Map(space);
  }
}
