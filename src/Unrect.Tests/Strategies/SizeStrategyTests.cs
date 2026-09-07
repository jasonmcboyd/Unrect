using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

using static Unrect.Strategies.AreaStrategies;
using static Unrect.Strategies.OffsetStrategies;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// The extent a strategy computes: what "while any" counts on each axis, what the explicit and
  /// degenerate spellings of a size, an offset and an area resolve to, and how a row strategy and a
  /// column strategy compose into one.
  /// </summary>
  public class SizeStrategyTests
  {
    private static bool HasValue(CellValue value) => value.HasValue;

    // --- SizeStrategies.RowsWhileAny ------------------------------------------------------------

    [Fact]
    public void RowsWhileAnyValue_TakesTheFullWidthAndStopsAtTheFirstAllBlankRow()
    {
      var space = Grid(new[,]
      {
        { 1, 2, 0 },
        { 0, 3, 0 },
        { 0, 0, 0 },   // no cell has a value: the region ends here
        { 4, 0, 0 },
      });

      var size = RowsWhileAnyValue().GetSize(space);

      Assert.Equal(3, size.Width);
      Assert.Equal(2, size.Height);
    }

    [Fact]
    public void RowsWhileAnyValue_OnAnImmediatelyBlankSpace_TakesNoRows()
    {
      var space = Grid(new[,]
      {
        { 0, 0 },
        { 1, 1 },
      });

      var size = RowsWhileAnyValue().GetSize(space);

      Assert.Equal(2, size.Width);
      Assert.Equal(0, size.Height);
    }

    [Fact]
    public void RowsWhileAnyValue_WhenEveryRowHasAValue_TakesEveryRow()
    {
      var space = Grid(new[,] { { 1, 0 }, { 0, 2 }, { 3, 3 } });

      Assert.Equal(3, RowsWhileAnyValue().GetSize(space).Height);
    }

    [Fact]
    public void RowsWhileAny_UsesTheSuppliedPredicate()
    {
      var space = Grid(new[,]
      {
        { 5, 1 },
        { 1, 5 },
        { 1, 1 },   // no cell is 5: stop
        { 5, 5 },
      });

      var size = RowsWhileAny(v => v.TryGetInt() == 5).GetSize(space);

      Assert.Equal(2, size.Width);
      Assert.Equal(2, size.Height);
    }

    // --- The column mirrors of the row strategies above ---------------------------------------------
    //
    // Each is the transpose of its row twin: same test, same shape of grid turned on its side, so a
    // hole in one axis shows up as a missing test rather than as a missing method nobody noticed.

    [Fact]
    public void ColumnsWhileAnyValue_TakesTheFullHeightAndStopsAtTheFirstAllBlankColumn()
    {
      var space = Grid(new[,]
      {
        { 1, 0, 0, 4 },
        { 2, 3, 0, 0 },
      });

      var size = ColumnsWhileAnyValue().GetSize(space);

      Assert.Equal(2, size.Width);    // column 2 is empty in both rows: the region ends there
      Assert.Equal(2, size.Height);
    }

    [Fact]
    public void ColumnsWhileAnyValue_OnAnImmediatelyBlankSpace_TakesNoColumns()
    {
      var space = Grid(new[,] { { 0, 1 }, { 0, 1 } });

      var size = ColumnsWhileAnyValue().GetSize(space);

      Assert.Equal(0, size.Width);
      Assert.Equal(2, size.Height);
    }

    [Fact]
    public void ColumnsWhileAnyValue_WhenEveryColumnHasAValue_TakesEveryColumn()
    {
      var space = Grid(new[,] { { 1, 0, 3 }, { 0, 2, 3 } });

      Assert.Equal(3, ColumnsWhileAnyValue().GetSize(space).Width);
    }

    [Fact]
    public void ColumnsWhileAny_UsesTheSuppliedPredicate()
    {
      var space = Grid(new[,]
      {
        { 5, 1, 1, 5 },
        { 1, 5, 1, 5 },
      });

      var size = ColumnsWhileAny(v => v.TryGetInt() == 5).GetSize(space);

      Assert.Equal(2, size.Width);    // no cell of column 2 is 5: stop
      Assert.Equal(2, size.Height);
    }

    // --- Explicit / degenerate sizes -------------------------------------------------------------

    [Fact]
    public void ExplicitSize_IgnoresTheAvailableSpace()
    {
      var size = ExplicitSize(2, 3).GetSize(Grid(new[,] { { 1, 1 }, { 1, 1 } }));

      Assert.Equal(2, size.Width);
      Assert.Equal(3, size.Height);
    }

    [Fact]
    public void MaxSize_IsTheWholeAvailableSpace()
    {
      var size = MaxSize().GetSize(Grid(new[,] { { 1, 1, 1 }, { 1, 1, 1 } }));

      Assert.Equal(3, size.Width);
      Assert.Equal(2, size.Height);
    }

    [Fact]
    public void MinSize_IsEmpty()
    {
      var size = MinSize().GetSize(Grid(new[,] { { 1, 1 } }));

      Assert.Equal(0, size.Width);
      Assert.Equal(0, size.Height);
    }

    [Fact]
    public void ExplicitOffset_IsTheDeclaredOffset()
    {
      var offset = ExplicitOffset(1, 2).GetOffset(Grid(new[,] { { 1, 1 }, { 1, 1 }, { 1, 1 } }));

      Assert.Equal(1, offset.Size.Width);
      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void MinOffset_IsTheOrigin()
    {
      var offset = MinOffset().GetOffset(Grid(new[,] { { 1, 1 } }));

      Assert.Equal(0, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    [Fact]
    public void ExplicitArea_IsTheDeclaredArea()
    {
      var area = ExplicitArea(3, 1).GetArea(Grid(new[,] { { 1, 1, 1 }, { 1, 1, 1 } }));

      Assert.Equal(3, area.Size.Width);
      Assert.Equal(1, area.Size.Height);
    }

    [Fact]
    public void MaxArea_IsTheWholeAvailableSpace()
    {
      var area = MaxArea().GetArea(Grid(new[,] { { 1, 1, 1 }, { 1, 1, 1 } }));

      Assert.Equal(3, area.Size.Width);
      Assert.Equal(2, area.Size.Height);
    }

    [Fact]
    public void SelectSize_UsesTheSuppliedSelector()
    {
      var size = SelectSize(s => new Size(s.Area.Size.Width - 1, 1)).GetSize(Grid(new[,] { { 1, 1, 1 } }));

      Assert.Equal(2, size.Width);
      Assert.Equal(1, size.Height);
    }

    // --- Composition order ------------------------------------------------------------------------
    //
    // A row strategy and a column strategy compose in two orders, and the order is observable: the
    // first axis narrows the space the second axis is counted over.

    [Fact]
    public void RowsThenColumns_CountsColumnsOnlyWithinTheSelectedRows()
    {
      var space = Grid(new[,]
      {
        { 1, 1, 0, 0 },
        { 0, 0, 1, 0 },   // this row would extend the column count, but it is not selected
      });

      // Rows first (one row), then columns within that row.
      var area = RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue().GetArea(space);

      Assert.Equal(2, area.Size.Width);
      Assert.Equal(1, area.Size.Height);
    }

    [Fact]
    public void ColumnsThenRows_CountsRowsOnlyWithinTheSelectedColumns()
    {
      var space = Grid(new[,]
      {
        { 1, 0, 0 },
        { 0, 0, 1 },   // these rows would extend the row count, but only via column 2
        { 0, 0, 1 },
      });

      // Columns first (two columns), then rows within those columns.
      var area = ColumnStrategies.TakeColumns(2).TakeRowsWhileAnyValue().GetArea(space);

      Assert.Equal(2, area.Size.Width);
      Assert.Equal(1, area.Size.Height);
    }

    [Fact]
    public void CompositionOrder_ChangesTheResultForTheSameGrid()
    {
      // The same grid, the same two boundaries, different orders — different answers. This is the
      // reason both halves of the composition are public.
      var space = Grid(new[,]
      {
        { 1, 1, 0 },
        { 0, 0, 1 },
      });

      var rowsFirst = RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue().GetArea(space);
      var columnsFirst = ColumnStrategies.TakeColumnsWhileAnyValue().TakeRowsWhileAnyValue().GetArea(space);

      Assert.Equal(2, rowsFirst.Size.Width);
      Assert.Equal(1, rowsFirst.Size.Height);

      Assert.Equal(3, columnsFirst.Size.Width);
      Assert.Equal(2, columnsFirst.Size.Height);
    }

    [Fact]
    public void RowsThenColumns_WithAPredicateForm_NarrowsBeforeCounting()
    {
      var space = Grid(new[,]
      {
        { 1, 1, 1, 0 },
        { 0, 0, 0, 1 },
      });

      var area = RowStrategies.TakeRows(1).TakeColumnsWhileAll(HasValue).GetArea(space);

      Assert.Equal(3, area.Size.Width);
      Assert.Equal(1, area.Size.Height);
    }
  }
}
