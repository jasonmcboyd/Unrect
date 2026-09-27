using System;

using Unrect.Core;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

using static Unrect.Strategies.OffsetStrategies;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// Where a region starts: the skip-while offsets that declare "however many leading rows are
  /// blank", and the from-the-far-edge anchors that reserve the last columns or rows instead.
  /// </summary>
  public class OffsetStrategyTests
  {
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

      var offset = SkipBlankRows<ISpace>().GetOffset(space);

      Assert.Equal(0, offset.Column);
      Assert.Equal(2, offset.Row);
    }

    [Fact]
    public void SkipBlankRows_OnASpaceThatStartsWithAValue_OffsetsByNothing()
    {
      var space = Grid(new[,] { { 1, 0 }, { 0, 0 } });

      Assert.Equal(0, SkipBlankRows<ISpace>().GetOffset(space).Row);
    }

    [Fact]
    public void SkipBlankRows_OnAnEntirelyBlankSpace_SkipsEveryRow()
    {
      var space = Grid(new[,] { { 0, 0 }, { 0, 0 } });

      Assert.Equal(2, SkipBlankRows<ISpace>().GetOffset(space).Row);
    }

    [Fact]
    public void SkipBlankColumns_OffsetsHorizontallyOnly()
    {
      var space = Grid(new[,]
      {
        { 0, 0, 1 },
        { 0, 0, 0 },
      });

      var offset = SkipBlankColumns<ISpace>().GetOffset(space);

      Assert.Equal(2, offset.Column);
      Assert.Equal(0, offset.Row);
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

      var offset = SkipRowsWhileAny<ISpace>(v => v.IsBlank() || v.AsText() == "1").GetOffset(space);

      Assert.Equal(2, offset.Row);
    }

    [Fact]
    public void SkipColumnsWhileAny_StopsAtTheFirstColumnWithNoMatch()
    {
      var space = Grid(new[,]
      {
        { 1, 0, 2 },
        { 0, 1, 2 },
      });

      var offset = SkipColumnsWhileAny<ISpace>(v => v.AsText() == "1").GetOffset(space);

      Assert.Equal(2, offset.Column);
      Assert.Equal(0, offset.Row);
    }

    // --- SkipToFirstNonBlankCell: the lazy corner heuristic -------------------------------------------
    //
    // Down to the first content row, across to its first non-blank cell; that cell's (column, row) is
    // the offset.

    [Fact]
    public void SkipToFirstNonBlankCell_OnATopLeftAlignedRegion_OffsetsByNothing()
    {
      var space = Grid(new[,]
      {
        { 1, 0 },
        { 0, 0 },
      });

      var offset = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(0, offset.Column);
      Assert.Equal(0, offset.Row);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_OnAColumnIndentedRegion_OffsetsAcrossToTheFirstContentCell()
    {
      var space = Grid(new[,]
      {
        { 0, 0, 3 },
        { 0, 0, 0 },
      });

      var offset = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(2, offset.Column);
      Assert.Equal(0, offset.Row);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_WithLeadingBlankRows_OffsetsDownToTheFirstContentRow()
    {
      var space = Grid(new[,]
      {
        { 0, 0 },
        { 0, 0 },
        { 1, 0 },
      });

      var offset = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(0, offset.Column);
      Assert.Equal(2, offset.Row);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_WithLeadingBlankRowsAndColumns_OffsetsToTheCorner()
    {
      var space = Grid(new[,]
      {
        { 0, 0, 0 },
        { 0, 5, 0 },
        { 0, 0, 0 },
      });

      var offset = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(1, offset.Column);
      Assert.Equal(1, offset.Row);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_OnARaggedRegion_ResolvesToTheFirstRowsCorner_TheDocumentedMiss()
    {
      // The accepted residual: the heuristic finds the
      // FIRST content row's first non-blank cell, which is the region's true corner only when it is
      // top-left-aligned. Here the first content row starts at column 2, but a lower row reaches back
      // to column 0 — so the offset lands on (2, 0) and the lower-left content (the 1 at column 0) is
      // lost. This is EXPECTED, not a bug: the eager SkipBlankRowsAndColumns escape hatch is where a
      // ragged region is meant to go.
      var space = Grid(new[,]
      {
        { 0, 0, 3 },
        { 1, 0, 0 },
      });

      var offset = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(2, offset.Column);
      Assert.Equal(0, offset.Row);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_OnAnEntirelyBlankSpace_ResolvesToTheSameEmptyExtentAsSkipBlankRows()
    {
      // An all-blank space has no content cell to find, so the heuristic skips past every row — the
      // same answer SkipBlankRows gives, and the point of the shared behaviour is that the subspace
      // each opens is empty rather than a throw.
      var space = Grid(new[,]
      {
        { 0, 0 },
        { 0, 0 },
        { 0, 0 },
      });

      var skipToCell = SkipToFirstNonBlankCell<ISpace>().GetOffset(space);
      var skipRows = SkipBlankRows<ISpace>().GetOffset(space);

      Assert.Equal(3, skipToCell.Row);
      Assert.Equal(skipRows.Row, skipToCell.Row);
      Assert.Equal(skipRows.Column, skipToCell.Column);
    }

    // --- SkipToFirstNonBlankCell is lazy (column-cheap): it reads rows only to the first with content

    [Fact]
    public void SkipToFirstNonBlankCell_WithContentInTheFirstRow_TouchesOnlyThatRow()
    {
      // Content at column 1 of row 0: the scan reads (0,0) blank then (1,0) with a value and stops —
      // one row touched, two cells read. A regression that scanned the whole extent would read more.
      var space = new CountingSpace(Grid(new[,]
      {
        { 0, 5 },
        { 0, 0 },
      }));

      SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(1, space.RowsTouched);
      Assert.Equal(2, space.CellReads);
    }

    [Fact]
    public void SkipToFirstNonBlankCell_WithLeadingBlankRows_TouchesOnlyUpToTheFirstContentRow()
    {
      // Two blank rows read across in full, then the third row stops at its first content cell: three
      // rows touched, never the whole height. This is the lazy / column-cheap profile the strategy
      // promises — the height axis is touched no further than the first content row.
      var space = new CountingSpace(Grid(new[,]
      {
        { 0, 0 },
        { 0, 0 },
        { 1, 0 },
      }));

      SkipToFirstNonBlankCell<ISpace>().GetOffset(space);

      Assert.Equal(3, space.RowsTouched);
    }

    // --- Anchoring to the far edge --------------------------------------------------------------------

    [Fact]
    public void FromRight_ReservesTheRightmostColumns()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      var offset = FromRight<ISpace>(2).GetOffset(space);

      Assert.Equal(2, offset.Column);
      Assert.Equal(0, offset.Row);
    }

    [Fact]
    public void FromBottom_ReservesTheBottomRows()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      var offset = FromBottom<ISpace>(1).GetOffset(space);

      Assert.Equal(0, offset.Column);
      Assert.Equal(2, offset.Row);
    }

    [Fact]
    public void FromEndAnchors_ThatExactlyFill_StartAtTheOrigin()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      Assert.Equal(0, FromRight<ISpace>(4).GetOffset(space).Column);
      Assert.Equal(0, FromBottom<ISpace>(3).GetOffset(space).Row);
    }

    [Fact]
    public void FromEndAnchors_ThatDoNotFit_Throw()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 }, { 11, 12, 13, 14 }, { 21, 22, 23, 24 } });

      Assert.ThrowsAny<OutOfBoundsException>(() => FromRight<ISpace>(5).GetOffset(space));
      Assert.ThrowsAny<OutOfBoundsException>(() => FromBottom<ISpace>(4).GetOffset(space));
    }

    [Fact]
    public void FromEndAnchors_RejectNegativeExtentsWhenTheyAreDeclared()
    {
      // Checked at the factory rather than at resolution time: a negative extent is a broken
      // declaration, and there is no space it could ever make sense against.
      Assert.Equal("width", Assert.Throws<ArgumentOutOfRangeException>(() => FromRight<ISpace>(-1)).ParamName);
      Assert.Equal("height", Assert.Throws<ArgumentOutOfRangeException>(() => FromBottom<ISpace>(-1)).ParamName);
    }
  }
}
