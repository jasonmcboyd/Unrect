using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// The push-side twin of <c>TypedPredicateLoweringTests.AMatchersRegionPredicateIsHandedTheSearchedRegionRatherThanTheSheet</c>:
  /// what a matcher's predicate is handed is the region searched, at the sheet's own origin, under
  /// either interpreter. The pull interpreter's test also pins the exact sequence of calls; under
  /// push a landmark is handed the region searched so far on each new span, so an earlier row may
  /// be looked at again, and this twin pins what both must agree on — the region — and not that.
  /// </summary>
  public class PushMatcherRegionTests
  {
    [Fact]
    public void UnderPull_ThePredicateSeesTheSearchedRegion()
    {
      using (ProjectionEngine.UsePull())
        AssertSearchedRegion();
    }

    [Fact]
    public void UnderPush_ThePredicateSeesTheSearchedRegion()
    {
      using (ProjectionEngine.UsePush())
        AssertSearchedRegion();
    }

    private static void AssertSearchedRegion()
    {
      var sheet = CoordinateGrid(4, 6);
      var origins = new List<Offset>();
      var rows = new List<int>();

      var anchored = On(RowWhere((plane, row) =>
      {
        origins.Add(plane.Origin);
        rows.Add(row);

        return plane[0, row].AsText() == "32";
      })).Of(Row(strip => strip[0].AsText()));

      var found = Down(2).Right(1).Of(VerticalFlow(v =>
      {
        var anchored2 = v.Next(anchored);

        return v.Build(read => read.Of(anchored2));
      }));

      Assert.Equal("32", found.Map(sheet));

      // Every plane the predicate saw was the searched region — the sheet from column 1, row 2 on —
      // and the match was that region's second row; rows before it may be looked at more than once,
      // never rows after it.
      Assert.NotEmpty(origins);
      Assert.All(origins, origin => Assert.Equal(new Offset(1, 2), origin));
      Assert.Equal(1, rows.Last());
      Assert.All(rows, row => Assert.True(row <= 1));
    }
  }
}
