using System;

using Unrect.Core;
using Unrect.Fluent;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// A line rule and a line landmark carry their own axis, and a member that wants one axis
  /// refuses the other where the declaration is written — the compile-time check the row and
  /// column pairs used to make, made at construction now, with both axes named.
  /// </summary>
  public class LineAxisTests
  {
    private static ICellSpace Sheet() => Grid(new[,] { { 1, 2 }, { 3, 4 } });

    [Fact]
    public void ARuleAndALandmarkSayWhichAxisTheyAreAbout()
    {
      Assert.Equal(Orientation.Vertical, RowStrategies.TakeRows<ICellSpace>(1).Along);
      Assert.Equal(Orientation.Horizontal, ColumnStrategies.TakeColumns<ICellSpace>(1).Along);
      Assert.Equal(Orientation.Vertical, RowLandmarks.RowSaying<ICellSpace>("x").Along);
      Assert.Equal(Orientation.Horizontal, ColumnLandmarks.ColumnSaying<ICellSpace>("x").Along);
    }

    [Fact]
    public void BelowWantsARowLandmarkAndRightOfWantsAColumnLandmark()
    {
      // Refused where written, before any space is opened, naming what was handed and what was wanted.
      var below = Assert.Throws<ArgumentException>(() => Below(Columns.Saying("x")));
      var rightOf = Assert.Throws<ArgumentException>(() => RightOf(Rows.Saying("x")));

      Assert.Contains("finds a column where one that finds a row was wanted", below.Message);
      Assert.Equal("landmark", below.ParamName);
      Assert.Contains("finds a row where one that finds a column was wanted", rightOf.Message);
    }

    [Fact]
    public void OnAndUntilTakeEitherAxisAndFollowTheLandmarks()
    {
      // Occupancy and a bound have no direction of their own: the landmark carries it.
      Assert.Equal("3", On(Rows.Saying("3")).Of(Point().AsText()).Map(Sheet()));
      Assert.Equal("2", On(Columns.Saying("2")).Of(Point().AsText()).Map(Sheet()));
      Assert.Equal(1, Until(Columns.Saying("2")).Of(Row(strip => strip.Count)).Map(Sheet()));
      Assert.Equal(1, Until(Rows.Saying("3")).Of(Column(strip => strip.Count)).Map(Sheet()));
    }

    [Fact]
    public void AStripAndACompositionWantTheAxisTheirParameterNames()
    {
      var row = Assert.Throws<ArgumentException>(() => Row(Rows.Take(1), strip => strip.Count));
      var column = Assert.Throws<ArgumentException>(() => Column(Columns.Take(1), strip => strip.Count));
      var composed = Assert.Throws<ArgumentException>(() => Rows.ThenColumns(Columns.Take(1), Rows.Take(1)));

      Assert.Equal("columns", row.ParamName);
      Assert.Equal("rows", column.ParamName);
      Assert.Contains("a rule about columns where a rule about rows was wanted", composed.Message);
      Assert.Equal("rows", composed.ParamName);
    }
  }
}
