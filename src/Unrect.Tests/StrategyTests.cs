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

    // --- Seeking: anchoring on presence -----------------------------------------------------------
    //
    // A skip-while anchors on absence and is defeated by anything inserted above the thing being
    // looked for. A seek scans to the first match, and the region starts AT it.

    /// <summary>A grid of labels; the array adapter treats null and "" as empty cells.</summary>
    private static ISpace Text(string?[,] values) => ArraySpace.Create(values);

    private static ISpace Labelled() => Text(new string?[,]
    {
      { "junk", null },
      { "an inserted proof row", null },
      { "  SECTION  ", null },
      { "a", "b" },
    });

    [Fact]
    public void SeekRowContaining_LandsOnTheRowThatHoldsTheLabel()
    {
      // The offset stops short of the match, so the region it places starts AT the label rather
      // than after it — the two junk rows above are exactly what a skip-while would have tripped on.
      var offset = SeekRowContaining("SECTION").GetOffset(Labelled());

      Assert.Equal(0, offset.Size.Width);
      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void SeekRowContaining_TrimsBothSidesAndIgnoresCase()
    {
      // The sheet says "  SECTION  "; the declaration may say it any way that reads well.
      Assert.Equal(2, SeekRowContaining("Section").GetOffset(Labelled()).Size.Height);
      Assert.Equal(2, SeekRowContaining("section").GetOffset(Labelled()).Size.Height);
      Assert.Equal(2, SeekRowContaining("  section  ").GetOffset(Labelled()).Size.Height);
    }

    [Theory]
    [InlineData("ecti")]
    [InlineData("SEC")]
    [InlineData("SECTION HEADER")]
    public void SeekRowContaining_MatchesWholeCellsNotSubstrings(string needle)
    {
      // Labels are whole cell values; substring matching would anchor on the first cell that merely
      // mentions the word. Anything fancier is what the predicate overload is for.
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekRowContaining(needle).GetOffset(Labelled()));
    }

    [Fact]
    public void SeekRowWhere_LandsOnTheFirstRowWithAMatchingCell()
    {
      // Column 1 is empty until the last row, so this finds a row by a cell that is not the first.
      Assert.Equal(3, SeekRowWhere(cell => cell.TryGetString() == "b").GetOffset(Labelled()).Size.Height);
    }

    [Fact]
    public void SeekRow_LandsOnTheFirstRowSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 } });

      Assert.Equal(2, SeekRow((s, row) => s[0, row].GetInt() == 3).GetOffset(space).Size.Height);
    }

    [Fact]
    public void SeekingLandsOnTheMatch_AndComposesToLandAfterIt()
    {
      // "The row after the label" is not a separate strategy; it is the seek and then a step.
      var space = Labelled();

      Assert.Equal(2, SeekRowContaining("Section").GetOffset(space).Size.Height);
      Assert.Equal(3, Then(SeekRowContaining("Section"), ExplicitOffset(0, 1)).GetOffset(space).Size.Height);
    }

    [Theory]
    [InlineData("Nope")]
    [InlineData("")]
    public void SeekRowContaining_WithNoMatch_Throws(string needle)
    {
      // A missing anchor is a placement failure: strict callers report it, and a repeat stops.
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekRowContaining(needle).GetOffset(Labelled()));
    }

    [Fact]
    public void SeekRowStrategies_WithNoMatch_AllThrow()
    {
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekRow((_, _) => false).GetOffset(Labelled()));
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekRowWhere(_ => false).GetOffset(Labelled()));
    }

    // --- The column twins ---------------------------------------------------------------------------

    private static ISpace LabelledColumns() => Text(new string?[,]
    {
      { "a", "b", "  TOTAL  ", "d" },
      { null, null, null, null },
    });

    [Fact]
    public void SeekColumnContaining_LandsOnTheColumnThatHoldsTheLabel()
    {
      var offset = SeekColumnContaining("Total").GetOffset(LabelledColumns());

      Assert.Equal(2, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    [Fact]
    public void SeekColumnContaining_TrimsBothSidesAndIgnoresCase()
    {
      Assert.Equal(2, SeekColumnContaining("total").GetOffset(LabelledColumns()).Size.Width);
      Assert.Equal(2, SeekColumnContaining("  Total  ").GetOffset(LabelledColumns()).Size.Width);
    }

    [Fact]
    public void SeekColumnContaining_MatchesWholeCellsNotSubstrings()
    {
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekColumnContaining("Tot").GetOffset(LabelledColumns()));
    }

    [Fact]
    public void SeekColumnWhere_LandsOnTheFirstColumnWithAMatchingCell()
    {
      Assert.Equal(3, SeekColumnWhere(cell => cell.TryGetString() == "d").GetOffset(LabelledColumns()).Size.Width);
    }

    [Fact]
    public void SeekColumn_LandsOnTheFirstColumnSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(1, SeekColumn((s, column) => s[column, 0].GetInt() == 2).GetOffset(space).Size.Width);
    }

    [Fact]
    public void SeekColumnStrategies_WithNoMatch_AllThrow()
    {
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekColumnContaining("Nope").GetOffset(LabelledColumns()));
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekColumn((_, _) => false).GetOffset(LabelledColumns()));
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekColumnWhere(_ => false).GetOffset(LabelledColumns()));
    }

    [Fact]
    public void SeekFactories_RejectNullArguments()
    {
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => SeekRow(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => SeekRowWhere(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => SeekRowContaining(null!)).ParamName);
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => SeekColumn(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => SeekColumnWhere(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => SeekColumnContaining(null!)).ParamName);
    }

    // --- Anchoring to the far edge --------------------------------------------------------------------

    [Fact]
    public void FromRight_ReservesTheRightmostColumns()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      var offset = FromRight(2).GetOffset(space);

      Assert.Equal(2, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    [Fact]
    public void FromBottom_ReservesTheBottomRows()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      var offset = FromBottom(1).GetOffset(space);

      Assert.Equal(0, offset.Size.Width);
      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void FromEndAnchors_ThatExactlyFill_StartAtTheOrigin()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      Assert.Equal(0, FromRight(4).GetOffset(space).Size.Width);
      Assert.Equal(0, FromBottom(3).GetOffset(space).Size.Height);
    }

    [Fact]
    public void FromEndAnchors_ThatDoNotFit_Throw()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      Assert.ThrowsAny<OutOfBoundsException>(() => FromRight(5).GetOffset(space));
      Assert.ThrowsAny<OutOfBoundsException>(() => FromBottom(4).GetOffset(space));
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

    // --- Landmarks: the same content rules, without the offset -------------------------------------
    //
    // A landmark says where a shape ends, where a seek says where one starts. The trio mirrors the
    // seeks exactly and matches on the same rules; the difference is that a landmark reports "not
    // found" as null and lets the shape bounding itself decide, where a seek throws.

    private static ISpace RowsWithATotal() => Text(new string?[,]
    {
      { "x", "y" },
      { "  TOTAL  ", null },
      { "z", null },
    });

    private static ISpace ColumnsWithATotal() => Text(new string?[,]
    {
      { "a", "  TOTAL  ", "c" },
      { null, null, "z" },
    });

    [Fact]
    public void RowWhere_FindsTheFirstRowSatisfyingAPositionalPredicate()
    {
      Assert.Equal(2, RowLandmarks.RowWhere((space, row) => space[0, row].TryGetString() == "z").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowWithCell_FindsTheFirstRowWithAMatchingCell()
    {
      // Column 1 is empty except on the first row, so this finds a row by a cell that is not its
      // first — the reason the "any cell" form exists at all.
      Assert.Equal(0, RowLandmarks.RowWithCell(cell => cell.TryGetString() == "y").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowContaining_MatchesWholeCellsTrimmedAndCaseInsensitively()
    {
      // The sheet says "  TOTAL  "; the declaration may say it any way that reads well.
      Assert.Equal(1, RowLandmarks.RowContaining("Total").FindRow(RowsWithATotal()));
      Assert.Equal(1, RowLandmarks.RowContaining("  total  ").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowContaining_MatchesWholeCellsNotSubstrings()
    {
      Assert.Null(RowLandmarks.RowContaining("TOT").FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowContaining("TOTALS").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowLandmarks_ReportAMissAsNullRatherThanThrowing()
    {
      // The whole difference from a seek: a missing end is a question for the shape being bounded,
      // not a failure in itself.
      Assert.Null(RowLandmarks.RowWhere((_, _) => false).FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowWithCell(_ => false).FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowContaining("Nope").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void ColumnWhere_FindsTheFirstColumnSatisfyingAPositionalPredicate()
    {
      Assert.Equal(2, ColumnLandmarks.ColumnWhere((space, column) => space[column, 0].TryGetString() == "c").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnWithCell_FindsTheFirstColumnWithAMatchingCell()
    {
      Assert.Equal(2, ColumnLandmarks.ColumnWithCell(cell => cell.TryGetString() == "z").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnContaining_MatchesWholeCellsTrimmedAndCaseInsensitively()
    {
      Assert.Equal(1, ColumnLandmarks.ColumnContaining("Total").FindColumn(ColumnsWithATotal()));
      Assert.Equal(1, ColumnLandmarks.ColumnContaining("  total  ").FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnContaining("TOT").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnLandmarks_ReportAMissAsNullRatherThanThrowing()
    {
      Assert.Null(ColumnLandmarks.ColumnWhere((_, _) => false).FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnWithCell(_ => false).FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnContaining("Nope").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ALandmarkDescribesWhatItLookedForTheWayASeekDoes()
    {
      // The descriptions are the negative noun phrases the failure templates are built from, so a
      // bound reads beside a seek rather than in its own dialect.
      Assert.Equal("no matching row", RowLandmarks.RowWhere((_, _) => false).Description);
      Assert.Equal("no row with a matching cell", RowLandmarks.RowWithCell(_ => false).Description);
      Assert.Equal("no row containing \'Total\'", RowLandmarks.RowContaining("Total").Description);

      Assert.Equal("no matching column", ColumnLandmarks.ColumnWhere((_, _) => false).Description);
      Assert.Equal("no column with a matching cell", ColumnLandmarks.ColumnWithCell(_ => false).Description);
      Assert.Equal("no column containing \'Total\'", ColumnLandmarks.ColumnContaining("Total").Description);
    }

    [Fact]
    public void ASeekAndALandmarkAgreeOnWhatContainingMeans()
    {
      // The deliberate cross-check: both go through the same CellMatching helper, so "containing"
      // cannot come to mean two things. Where the seek lands is where the landmark is found.
      var space = RowsWithATotal();

      foreach (var needle in new[] { "Total", "  total  ", "TOTAL" })
      {
        Assert.Equal(1, RowLandmarks.RowContaining(needle).FindRow(space));
        Assert.Equal(1, SeekRowContaining(needle).GetOffset(space).Size.Height);
      }

      // ...and they agree on what does not match, one by returning null and one by throwing.
      Assert.Null(RowLandmarks.RowContaining("TOT").FindRow(space));
      Assert.ThrowsAny<OutOfBoundsException>(() => SeekRowContaining("TOT").GetOffset(space));
    }

    [Fact]
    public void LandmarkFactories_RejectNullArguments()
    {
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowWhere(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowWithCell(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowContaining(null!)).ParamName);
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnWhere(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnWithCell(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnContaining(null!)).ParamName);
    }

    [Fact]
    public void FromEndAnchors_RejectNegativeExtentsWhenTheyAreDeclared()
    {
      // Checked at the factory rather than at resolution time: a negative extent is a broken
      // declaration, and there is no space it could ever make sense against.
      Assert.Equal("width", Assert.Throws<ArgumentOutOfRangeException>(() => FromRight(-1)).ParamName);
      Assert.Equal("height", Assert.Throws<ArgumentOutOfRangeException>(() => FromBottom(-1)).ParamName);
    }
  }
}
