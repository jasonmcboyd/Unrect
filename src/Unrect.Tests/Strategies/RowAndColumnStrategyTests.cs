using System;

using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// The row and column strategies, which count along one axis. These pin the counting semantics:
  /// what "while all" and "while any" mean, whether the terminating row is included, and what
  /// happens when an explicit count does not fit.
  /// </summary>
  public class RowAndColumnStrategyTests
  {
    private static bool HasValue(CellValue value) => value.HasValue;

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

    [Fact]
    public void TakeColumnsWhile_CountsLeadingColumnsSatisfyingACellPredicate()
    {
      // The mirror of TakeRowsWhile(column, predicate): read one row, count along it.
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 0, 0, 0, 0 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhile(0, (cell, column) => cell.GetInt() < 3).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsTo_IncludesTheMatchingColumn()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 } });

      Assert.Equal(3, ColumnStrategies.TakeColumnsTo((s, column) => s[column, 0].GetInt() == 3).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsTo_WhenNothingMatches_TakesEveryColumn()
    {
      var space = Grid(new[,] { { 1, 2 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsTo((s, column) => s[column, 0].GetInt() == 99).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsToValue_IncludesTheColumnHoldingTheValue()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsToValue(0, CellValue.Of(2)).SelectColumns(space));
    }

    [Fact]
    public void AllRowsAndAllColumns_AreTheDeclaredSpellingsOfTheFullExtent()
    {
      // What (_, _) => true used to say opaquely at a dozen call sites.
      var space = Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

      Assert.Equal(2, RowStrategies.AllRows().SelectRows(space));
      Assert.Equal(3, ColumnStrategies.AllColumns().SelectColumns(space));
    }

    [Fact]
    public void AllRowsAndAllColumns_ComposeIntoAnArea()
    {
      // The area-composing forms: one axis chosen, the other taken whole.
      var space = Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

      var fullWidthRow = RowStrategies.TakeRows(1).AllColumns().GetArea(space);
      var fullHeightColumn = ColumnStrategies.TakeColumns(1).AllRows().GetArea(space);

      Assert.Equal(3, fullWidthRow.Size.Width);
      Assert.Equal(1, fullWidthRow.Size.Height);

      Assert.Equal(1, fullHeightColumn.Size.Width);
      Assert.Equal(2, fullHeightColumn.Size.Height);
    }
  }
}
