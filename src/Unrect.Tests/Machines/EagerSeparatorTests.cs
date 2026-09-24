using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// A repeat whose separator has no per-span form — a lambda over the gap — is held, and at close
  /// walks its gathered extent asking the separator over each gap the way the whole-extent walk
  /// always did. Same occurrences, same stop, one hold the cost report names.
  /// </summary>
  public class EagerSeparatorTests
  {
    private static readonly object?[,] Blocks =
    {
      { "a", 1m },
      { "b", 2m },
      { null, null },
      { "c", 3m },
      { null, null },
      { null, null },
      { "d", 4m },
      { null, null },
      { "x", null },
    };

    private static IProjectionDefinition<ICellSpace, IReadOnlyList<string>> Block()
      => Sized(RowsWhileAnyValue()).Of(Range(block => (IReadOnlyList<string>)block.Rows.Select(row => row[0].AsText()!).ToList()));

    private static IProjectionDefinition<ICellSpace, IReadOnlyList<IReadOnlyList<string>>> Repeat(IOffsetStrategy<ICellSpace> separator)
      => VerticalRepeat(Block(), separatedBy: separator);

    private static IProjectionDefinition<ICellSpace, IReadOnlyList<IReadOnlyList<string>>> Repeat(IOffsetStrategy separator)
      => VerticalRepeat(Block(), separatedBy: separator);

    /// <summary>The gap is however many leading rows are wholly blank — spelled as a lambda, so it has no per-span form.</summary>
    private static IOffsetStrategy<ICellSpace> BlankGap()
      => SelectOffset(plane =>
      {
        var rows = 0;

        while (rows < plane.Area.Height && Enumerable.Range(0, plane.Width).All(column => plane[column, rows].IsBlank()))
          rows++;

        return new Size(0, rows);
      });

    [Fact]
    public void ALambdaSeparatorReadsTheSameOccurrencesAsItsPerSpanTwin()
    {
      var space = Mixed(Blocks);

      var eager = Repeat(BlankGap()).Map(space);
      var streamed = Repeat(BlankRows()).Map(space);

      Assert.Equal(streamed.Select(block => string.Join(",", block)), eager.Select(block => string.Join(",", block)));
      Assert.Equal(new[] { "a,b", "c", "d", "x" }, eager.Select(block => string.Join(",", block)));
    }

    [Fact]
    public void ASeparatorWithNoRoomEndsTheRunRatherThanFailing()
    {
      // A separator that always asks for more rows than remain: the first occurrence stands alone.
      var space = Mixed(Blocks);
      var greedy = Repeat(SelectOffset(plane => new Size(0, plane.Area.Height + 1)));

      Assert.Equal(new[] { "a,b" }, greedy.Map(space).Select(block => string.Join(",", block)));
    }

    [Fact]
    public void TheCostReportNamesTheHold()
    {
      var held = CostReport.Of(Repeat(BlankGap())).Lines[0];
      var streamed = CostReport.Of(Repeat(BlankRows())).Lines[0];

      Assert.False(held.Streams);
      Assert.Equal("its separator has no per-span form", held.Hold);
      Assert.Equal(Axes.Vertical, held.Axis);
      Assert.True(streamed.Streams);
    }
  }
}
