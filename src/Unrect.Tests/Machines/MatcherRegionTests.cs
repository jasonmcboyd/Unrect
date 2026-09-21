using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// What a matcher's predicate is handed is the region searched, at the sheet's own origin. A
  /// landmark is handed the region searched so far on each new span, so an earlier row may be
  /// looked at again; this pins the region, not the sequence of calls.
  /// </summary>
  public class MatcherRegionTests
  {
    [Fact]
    public void ThePredicateSeesTheSearchedRegion()
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

        return anchored2;
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
