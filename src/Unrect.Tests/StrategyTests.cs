using System;

using Unrect.Array;
using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Strategies.AreaStrategies;
using static Unrect.Strategies.OffsetStrategies;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Tests
{
  /// <summary>
  /// Strategies are how a shape declares a boundary without walking the grid. These tests pin the
  /// counting semantics of each one: what "while all" and "while any" mean, whether the terminating
  /// row is included, and what happens when an explicit count does not fit.
  /// </summary>
  public class StrategyTests
  {
    // 0 is blank in every grid in this class, so the shape of the literal is the shape of the data.
    private static ISpace Grid(int[,] values) => ArraySpace.Create(values, isBlank: v => v == 0);

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

    // --- Offset strategies ----------------------------------------------------------------------

    [Fact]
    public void SkipBlankRows_OffsetsVerticallyOnly()
    {
      var space = Grid(new[,]
      {
        { 0, 0 },
        { 0, 0 },
        { 1, 0 },
      });

      var offset = SkipBlankRows().GetOffset(space);

      Assert.Equal(0, offset.Size.Width);
      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void SkipBlankRows_OnASpaceThatStartsWithAValue_OffsetsByNothing()
    {
      var space = Grid(new[,] { { 1, 0 }, { 0, 0 } });

      Assert.Equal(0, SkipBlankRows().GetOffset(space).Size.Height);
    }

    [Fact]
    public void SkipBlankRows_OnAnEntirelyBlankSpace_SkipsEveryRow()
    {
      var space = Grid(new[,] { { 0, 0 }, { 0, 0 } });

      Assert.Equal(2, SkipBlankRows().GetOffset(space).Size.Height);
    }

    [Fact]
    public void SkipBlankColumns_OffsetsHorizontallyOnly()
    {
      var space = Grid(new[,]
      {
        { 0, 0, 1 },
        { 0, 0, 0 },
      });

      var offset = SkipBlankColumns().GetOffset(space);

      Assert.Equal(2, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    [Fact]
    public void SkipRowsWhileAny_StopsAtTheFirstRowWithNoMatch()
    {
      var space = Grid(new[,]
      {
        { 0, 1 },
        { 1, 0 },
        { 2, 3 },   // no cell is blank or 1: stop
      });

      var offset = SkipRowsWhileAny(v => v.IsBlank || v.TryGetInt() == 1).GetOffset(space);

      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void SkipColumnsWhileAny_StopsAtTheFirstColumnWithNoMatch()
    {
      var space = Grid(new[,]
      {
        { 1, 0, 2 },
        { 0, 1, 2 },
      });

      var offset = SkipColumnsWhileAny(v => v.TryGetInt() == 1).GetOffset(space);

      Assert.Equal(2, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    // --- Row strategies -------------------------------------------------------------------------

    [Fact]
    public void TakeRowsWhileAll_CountsLeadingRowsInWhichEveryCellMatches()
    {
      var space = Grid(new[,]
      {
        { 1, 1 },
        { 1, 1 },
        { 1, 0 },   // not every cell matches: stop, and do not include this row
        { 1, 1 },
      });

      Assert.Equal(2, RowStrategies.TakeRowsWhileAll(HasValue).SelectRows(space));
    }

    [Fact]
    public void TakeRowsWhileAny_CountsLeadingRowsInWhichAtLeastOneCellMatches()
    {
      var space = Grid(new[,]
      {
        { 1, 0 },
        { 0, 1 },
        { 0, 0 },   // no cell matches: stop
        { 1, 1 },
      });

      Assert.Equal(2, RowStrategies.TakeRowsWhileAny(HasValue).SelectRows(space));
    }

    [Fact]
    public void TakeRowsWhile_CountsLeadingRowsSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } });

      Assert.Equal(2, RowStrategies.TakeRowsWhile((s, row) => s[0, row].GetInt() < 3).SelectRows(space));
    }

    [Fact]
    public void TakeRowsTo_IncludesTheMatchingRow()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } });

      Assert.Equal(3, RowStrategies.TakeRowsTo((s, row) => s[0, row].GetInt() == 3).SelectRows(space));
    }

    [Fact]
    public void TakeRowsTo_WhenNothingMatches_TakesEveryRow()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 } });

      Assert.Equal(2, RowStrategies.TakeRowsTo((s, row) => s[0, row].GetInt() == 99).SelectRows(space));
    }

    [Fact]
    public void TakeRowsToValue_IncludesTheRowHoldingTheValue()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 } });

      Assert.Equal(2, RowStrategies.TakeRowsToValue(0, CellValue.Of(2)).SelectRows(space));
    }

    [Fact]
    public void TakeRows_ReturnsTheRequestedCountWhenItFits()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 } });

      Assert.Equal(2, RowStrategies.TakeRows(2).SelectRows(space));
      Assert.Equal(3, RowStrategies.TakeRows(3).SelectRows(space));
      Assert.Equal(0, RowStrategies.TakeRows(0).SelectRows(space));
    }

    [Fact]
    public void TakeRows_BeyondTheAvailableHeight_ThrowsRatherThanClamping()
    {
      var space = Grid(new[,] { { 1 }, { 2 } });

      Assert.Throws<OutOfBoundsException>(() => RowStrategies.TakeRows(3).SelectRows(space));
    }

    [Fact]
    public void TakeRows_WithANegativeCount_Throws()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => RowStrategies.TakeRows(-1));
    }

    // --- Column strategies ----------------------------------------------------------------------

    [Fact]
    public void TakeColumnsWhileAll_CountsLeadingColumnsInWhichEveryCellMatches()
    {
      var space = Grid(new[,]
      {
        { 1, 1, 1, 1 },
        { 1, 1, 0, 1 },   // column 2 has a blank, so counting stops before it
      });

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAll(HasValue).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsWhileAny_CountsLeadingColumnsInWhichAtLeastOneCellMatches()
    {
      var space = Grid(new[,]
      {
        { 1, 0, 0, 1 },
        { 0, 1, 0, 1 },   // column 2 is empty in both rows: stop
      });

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAny(HasValue).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsWhileAnyValue_IsNotInverted()
    {
      // Regression: this once delegated to the "while all" strategy with a negated predicate, which
      // computes "take while none match" — the exact opposite of the name.
      var space = Grid(new[,] { { 1, 1, 0, 1 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhileAnyValue().SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsWhile_CountsLeadingColumnsSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhile((s, column) => s[column, 0].GetInt() < 3).SelectColumns(space));
    }

    [Fact]
    public void TakeColumns_ReturnsTheRequestedCountWhenItFits()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(2, ColumnStrategies.TakeColumns(2).SelectColumns(space));
      Assert.Equal(3, ColumnStrategies.TakeColumns(3).SelectColumns(space));
      Assert.Equal(0, ColumnStrategies.TakeColumns(0).SelectColumns(space));
    }

    [Fact]
    public void TakeColumns_BeyondTheAvailableWidth_ThrowsRatherThanClamping()
    {
      var space = Grid(new[,] { { 1, 2 } });

      Assert.Throws<OutOfBoundsException>(() => ColumnStrategies.TakeColumns(3).SelectColumns(space));
    }

    [Fact]
    public void TakeColumns_WithANegativeCount_Throws()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => ColumnStrategies.TakeColumns(-1));
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
