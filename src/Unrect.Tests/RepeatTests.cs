using System.Linq;
using System.Threading.Tasks;

using Unrect.Array;
using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.RegionBuilderFactory;
using static Unrect.Strategies.AreaStrategies;
using static Unrect.Strategies.OffsetStrategies;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Tests
{
  /// <summary>
  /// <c>Repeat</c> applies one declared block as many times as the data supports. The block's own
  /// offset and area strategies discover each block's extent, so block lengths may differ; the
  /// interesting behaviour is termination — trailing blank bands, empty blocks, and shapes that
  /// consume nothing must all stop rather than run away.
  /// </summary>
  public class RepeatTests
  {
    // 0 is blank in every grid in this class.
    private static ISpace Grid(int[,] values) => ArraySpace.Create(values, isBlank: v => v == 0);

    /// <summary>
    /// The canonical repeating block: skip any blank separator rows, take rows until the block runs
    /// out of values, then split it into a one-cell code and the remaining data rows.
    /// </summary>
    private static StackRegionBuilder2<Region, Region> BlockBuilder() =>
      Vertical(
        SkipBlankRows(),
        RowsWhileAnyValue().ToAreaStrategy(),
        Builder(0, 0, 1, 1),
        Builder(RowsWhileAnyValue().ToAreaStrategy()));

    // --- Varying block lengths ---------------------------------------------------------------------

    [Fact]
    public void Repeat_YieldsOneBlockPerRunOfNonBlankRows()
    {
      var space = Grid(new[,]
      {
        { 1, 0 },   // block 1: code row + 1 data row
        { 5, 6 },
        { 0, 0 },
        { 2, 0 },   // block 2: code row + 2 data rows
        { 7, 8 },
        { 9, 1 },
        { 0, 0 },
        { 3, 0 },   // block 3: code row + 1 data row
        { 4, 4 },
      });

      var blocks = Repeat(BlockBuilder()).Build(space).Subregions;

      Assert.Equal(3, blocks.Length);
      Assert.Equal(new[] { 1, 2, 3 }, blocks.Select(b => b.Subregion1.Space[0, 0].GetInt()).ToArray());
      Assert.Equal(new[] { 2, 3, 2 }, blocks.Select(b => b.Space.Area.Size.Height).ToArray());
      Assert.Equal(new[] { 1, 2, 1 }, blocks.Select(b => b.Subregion2.Space.Area.Size.Height).ToArray());
      Assert.Equal(new[] { 5, 7, 4 }, blocks.Select(b => b.Subregion2.Space[0, 0].GetInt()).ToArray());
    }

    [Fact]
    public void Repeat_ExposesTheWholeSpaceOnTheSuperRegion()
    {
      var space = Grid(new[,] { { 1, 0 }, { 5, 6 } });

      var region = Repeat(BlockBuilder()).Build(space);

      Assert.Equal(2, region.Space.Area.Size.Width);
      Assert.Equal(2, region.Space.Area.Size.Height);
      Assert.Equal(region.Subregions.Length, region.GetSubregions().Count());
    }

    // --- Termination --------------------------------------------------------------------------------

    [Fact]
    public void Repeat_WithATrailingBlankBand_YieldsNoExtraBlockAndDoesNotThrow()
    {
      var space = Grid(new[,]
      {
        { 1, 0 },
        { 5, 6 },
        { 0, 0 },
        { 2, 0 },
        { 7, 8 },
        { 0, 0 },   // trailing blank band: the block offset skips it and then finds nothing
        { 0, 0 },
      });

      var blocks = Repeat(BlockBuilder()).Build(space).Subregions;

      Assert.Equal(2, blocks.Length);
    }

    [Fact]
    public void Repeat_OnAnAllBlankSpace_YieldsNoBlocks()
    {
      var space = Grid(new[,] { { 0, 0 }, { 0, 0 } });

      var blocks = Repeat(Builder(RowsWhileAnyValue().ToAreaStrategy())).Build(space).Subregions;

      Assert.Empty(blocks);
    }

    [Fact]
    public void Repeat_OnAnEmptySpace_YieldsNoBlocks()
    {
      var space = Grid(new[,] { { 1, 1 } }).GetSubspace(new Offset(0, 0), new Area(0, 0));

      Assert.Empty(Repeat(Builder()).Build(space).Subregions);
    }

    // The two termination tests below run their build on a worker thread so that a regression which
    // reintroduces an infinite loop fails the run on a timeout instead of hanging it forever.
    // (xUnit only honours Timeout on async tests, hence the Task.Run.)

    [Fact(Timeout = 30000)]
    public async Task Repeat_WithAZeroAreaShape_TerminatesInsteadOfLooping()
    {
      // A block that consumes nothing would repeat forever; the builder stops instead of hanging.
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 } });

      var blocks = await Task.Run(() => Repeat(Builder(MinArea())).Build(space).Subregions);

      Assert.Empty(blocks);
    }

    [Fact(Timeout = 30000)]
    public async Task Repeat_WithAZeroHeightShape_TerminatesInsteadOfLooping()
    {
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 } });

      var blocks = await Task.Run(() => Repeat(Builder(ExplicitArea(2, 0))).Build(space).Subregions);

      Assert.Empty(blocks);
    }

    [Fact]
    public void Repeat_WhenTheNextBlockDoesNotFit_StopsWithoutThrowing()
    {
      // The final row cannot satisfy a two-row block, so it is left unconsumed rather than being an
      // error: "no more blocks" is the stopping condition for a repeat.
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 }, { 4 }, { 5 } });

      var blocks = Repeat(Builder(ExplicitArea(1, 2))).Build(space).Subregions;

      Assert.Equal(2, blocks.Length);
      Assert.Equal(1, blocks[0].Space[0, 0].GetInt());
      Assert.Equal(3, blocks[1].Space[0, 0].GetInt());
    }

    // --- Horizontal repetition -----------------------------------------------------------------------

    [Fact]
    public void RepeatHorizontal_YieldsBlocksLeftToRight()
    {
      var space = Grid(new[,]
      {
        { 1, 2, 3, 4, 5, 6 },
        { 7, 8, 9, 10, 11, 12 },
      });

      var blocks = RepeatHorizontal(Builder(ExplicitArea(2, 2))).Build(space).Subregions;

      Assert.Equal(3, blocks.Length);
      Assert.Equal(new[] { 1, 3, 5 }, blocks.Select(b => b.Space[0, 0].GetInt()).ToArray());
      Assert.All(blocks, b => Assert.Equal(2, b.Space.Area.Size.Width));
    }

    [Fact]
    public void RepeatHorizontal_SkipsBlankSeparatorColumns()
    {
      var space = Grid(new[,] { { 1, 2, 0, 3, 4 } });

      var blocks = RepeatHorizontal(Builder(SkipBlankColumns(), ExplicitArea(2, 1))).Build(space).Subregions;

      Assert.Equal(2, blocks.Length);
      Assert.Equal(1, blocks[0].Space[0, 0].GetInt());
      Assert.Equal(3, blocks[1].Space[0, 0].GetInt());
    }
  }
}
