using System.Collections.Generic;

using Unrect.Fluent;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The reflective table closes a placement pipeline the way every other shape does, so a table
  /// that starts somewhere other than the top-left reads as one sentence:
  /// <c>Down(1).Table&lt;Line&gt;()</c>, <c>Below(mark).Table&lt;Line&gt;(bind)</c>.
  /// </summary>
  public class PlacedReflectiveTableTests
  {
    private sealed record Line(string Name, int Amount);

    private static readonly object?[,] Sheet =
    {
      { "Report", null },
      { "Name", "Amount" },
      { "a", 1 },
      { "b", 2 },
    };

    [Fact]
    public void DownThenTableBindsTheRecordsBelowTheSkippedRow()
    {
      IReadOnlyList<Line> lines = Down(1).Table<Line>().Map(Mixed(Sheet));

      Assert.Equal(new[] { new Line("a", 1), new Line("b", 2) }, lines);
    }

    [Fact]
    public void BelowAMarkThenTableWithABindReadsTheSameRecords()
    {
      var placed = Below(RowSaying("Report")).Table<Line>(bind => bind.Column(l => l.Amount, "Amount"));
      var bare = Down(1).Table<Line>();

      Assert.Equal(bare.Map(Mixed(Sheet)), placed.Map(Mixed(Sheet)));
    }
  }
}
