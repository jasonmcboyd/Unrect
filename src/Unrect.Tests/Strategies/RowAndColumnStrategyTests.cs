using System;

using System.Globalization;

using Unrect.Core;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// The row and column strategies, which count along one axis. These pin the counting semantics:
  /// what "while all" and "while any" mean, whether the terminating row is included, and what
  /// happens when an explicit count does not fit.
  /// <para>
  /// Every predicate here asks the four questions, through the rendering where it wants a number:
  /// this file is about the erased calculus, whose predicates are handed a cell over
  /// <c>ISpace</c> on purpose. The grids are built from ints and render each one invariantly, so a
  /// comparison against the rendering selects exactly the cells a value rule would. A predicate that
  /// asks a cell's kind or its value names its space and belongs to the typed vocabulary, which is
  /// pinned where it lives.
  /// </para>
  /// </summary>
  public class RowAndColumnStrategyTests
  {
    private static bool HasValue(Point<ISpace> value) => value.HasValue();

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

      Assert.Equal(2, RowStrategies.TakeRowsWhile((s, row) => s[0, row].AsText() is string number && int.Parse(number, CultureInfo.InvariantCulture) < 3).SelectRows(space));
    }

    [Fact]
    public void TakeRowsTo_IncludesTheMatchingRow()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } });

      Assert.Equal(3, RowStrategies.TakeRowsTo((s, row) => s[0, row].AsText() == "3").SelectRows(space));
    }

    [Fact]
    public void TakeRowsTo_WhenNothingMatches_TakesEveryRow()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 } });

      Assert.Equal(2, RowStrategies.TakeRowsTo((s, row) => s[0, row].AsText() == "99").SelectRows(space));
    }

    // --- TakeRowsToText: the band ends on a LABEL --------------------------------------------------
    //
    // The rename is the rule. The old spelling compared a whole Cell, so it would end a band on
    // whatever kind of cell happened to equal the one written into the declaration; the new one asks
    // the content question every other text matcher asks, and so sees text cells alone. That is the
    // difference the fixtures below are chosen to show — an int grid cannot show it at all, which is
    // why the pins these replace could not simply be un-skipped.

    /// <summary>Labels down column 0 with the boundary at row 2, and a second column of noise.</summary>
    private static ICellSpace LabelledRows() => Labels(new string?[,]
    {
      { "a", "x" },
      { "b", "y" },
      { "  Total  ", "z" },
      { "c", "w" },
    });

    [Fact]
    public void TakeRowsToText_IncludesTheRowHoldingTheText()
    {
      // Up to AND including the match, which is the whole of what distinguishes this from a
      // while-rule: the boundary row is content the section reads, not a gap it stops before.
      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "Total").Strategy.SelectRows(LabelledRows()));
    }

    [Fact]
    public void TakeRowsToText_MatchesWholeCellTrimmedAndCaseInsensitively()
    {
      // The one content rule, which this shares with RowContaining, Caption and Field: the fixture's
      // cell is "  Total  " and the declaration writes "total".
      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "total").Strategy.SelectRows(LabelledRows()));

      // ...and whole-cell, not substring: a band must not end on a row that merely mentions the word.
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "Tot").Strategy.SelectRows(LabelledRows()));
    }

    [Fact]
    public void TakeRowsToText_ReadsTheColumnItWasGivenAndNoOther()
    {
      // The column argument is the whole of the addressing, so a match in another column is not one.
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(1, "Total").Strategy.SelectRows(LabelledRows()));
    }

    [Fact]
    public void TakeRowsToText_WhenNothingMatches_TakesEveryRow()
    {
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "no such label").Strategy.SelectRows(LabelledRows()));
    }

    [Fact]
    public void TakeRowsToText_DoesNotEndTheBandOnANumberRenderingTheSameDigits()
    {
      // The discriminating pin, and the reason for the rename. Row 1 holds the NUMBER 42 and says
      // "42"; row 2 holds the TEXT "42". A rule that asked what a cell says would stop at row 1 and
      // swallow a row of data as though it were a boundary label — silently, and on exactly the
      // export where a total happens to be numeric.
      var space = Mixed(new object?[,] { { "a" }, { 42 }, { "42" }, { "b" } });

      // Non-vacuity: the numeric cell really does render the needle, so the refusal is about kind.
      Assert.Equal("42", space.AsText(0, 1));
      Assert.False(space.IsText(0, 1));
      Assert.True(space.IsText(0, 2));

      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, "42").Strategy.SelectRows(space));
    }

    [Fact]
    public void TakeRowsToText_NeedsSomethingToLookFor()
    {
      Assert.Throws<ArgumentNullException>(() => SheetProjectionBuilders<ICellSpace>.TakeRowsToText(0, null!));
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

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhile((s, column) => s[column, 0].AsText() is string number && int.Parse(number, CultureInfo.InvariantCulture) < 3).SelectColumns(space));
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

      Assert.Equal(2, ColumnStrategies.TakeColumnsWhile(0, (cell, column) => cell.AsText() is string number && int.Parse(number, CultureInfo.InvariantCulture) < 3).SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsTo_IncludesTheMatchingColumn()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 } });

      Assert.Equal(3, ColumnStrategies.TakeColumnsTo((s, column) => s[column, 0].AsText() == "3").SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsTo_WhenNothingMatches_TakesEveryColumn()
    {
      var space = Grid(new[,] { { 1, 2 } });

      Assert.Equal(2, ColumnStrategies.TakeColumnsTo((s, column) => s[column, 0].AsText() == "99").SelectColumns(space));
    }

    // --- TakeColumnsToText: the same rule, transposed ------------------------------------------------

    /// <summary>The transpose of <see cref="LabelledRows"/>: labels along row 0, boundary at column 2.</summary>
    private static ICellSpace LabelledColumns() => Labels(new string?[,]
    {
      { "a", "b", "  Total  ", "c" },
      { "x", "y", "z", "w" },
    });

    [Fact]
    public void TakeColumnsToText_IncludesTheColumnHoldingTheText()
    {
      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "Total").Strategy.SelectColumns(LabelledColumns()));
    }

    [Fact]
    public void TakeColumnsToText_MatchesWholeCellTrimmedAndCaseInsensitively()
    {
      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "total").Strategy.SelectColumns(LabelledColumns()));
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "Tot").Strategy.SelectColumns(LabelledColumns()));
    }

    [Fact]
    public void TakeColumnsToText_ReadsTheRowItWasGivenAndNoOther()
    {
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(1, "Total").Strategy.SelectColumns(LabelledColumns()));
    }

    [Fact]
    public void TakeColumnsToText_WhenNothingMatches_TakesEveryColumn()
    {
      Assert.Equal(4, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "no such label").Strategy.SelectColumns(LabelledColumns()));
    }

    [Fact]
    public void TakeColumnsToText_DoesNotEndTheBandOnANumberRenderingTheSameDigits()
    {
      // The column twin of the discriminating pin, over the transposed fixture: column 1 holds the
      // NUMBER 42 and column 2 the TEXT "42".
      var space = Mixed(new object?[,] { { "a", 42, "42", "b" } });

      Assert.Equal("42", space.AsText(1, 0));
      Assert.False(space.IsText(1, 0));
      Assert.True(space.IsText(2, 0));

      Assert.Equal(3, SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, "42").Strategy.SelectColumns(space));
    }

    [Fact]
    public void TakeColumnsToText_NeedsSomethingToLookFor()
    {
      Assert.Throws<ArgumentNullException>(() => SheetProjectionBuilders<ICellSpace>.TakeColumnsToText(0, null!));
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
