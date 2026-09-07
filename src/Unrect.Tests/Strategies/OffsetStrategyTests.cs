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
