using System.Collections.Generic;

using Unrect.Core;
using Unrect.Fluent;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// A separator with a per-span form is begun afresh for every gap: a scan may carry state — a
  /// landmark remembers that it matched, a chain which stage it is on — so one scan reused across
  /// gaps would refuse the second gap's first span and start the item on the separator line.
  /// </summary>
  public class StreamedSeparatorTests
  {
    private static IReadOnlyList<string> Items(object?[,] cells, IOffsetStrategy<ICellSpace> separator)
      => VerticalRepeat(Point().AsText(), separatedBy: separator).Map(Mixed(cells));

    [Fact]
    public void ALandmarkSeparatorFindsEveryGapNotJustTheFirst()
    {
      var cells = new object?[,] { { "A" }, { "---" }, { "B" }, { "---" }, { "C" } };

      Assert.Equal(new[] { "A", "B", "C" }, Items(cells, OffsetStrategies.Past(RowSaying("---"))));
    }

    [Fact]
    public void AChainedSeparatorStartsOverAtEveryGap()
    {
      var cells = new object?[,] { { "A" }, { null }, { "x" }, { "B" }, { null }, { "x" }, { "C" } };
      var gap = OffsetStrategies.Then(OffsetStrategies.SkipBlankRows<ICellSpace>(), OffsetStrategies.ExplicitOffset<ICellSpace>(0, 1));

      Assert.Equal(new[] { "A", "B", "C" }, Items(cells, gap));
    }
  }
}
