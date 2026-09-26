using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
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
      Assert.Equal(Orientation.Vertical, RowStrategies.TakeRows(1).Along);
      Assert.Equal(Orientation.Horizontal, ColumnStrategies.TakeColumns(1).Along);
      Assert.Equal(Orientation.Vertical, RowLandmarks.RowSaying("x").Along);
      Assert.Equal(Orientation.Horizontal, ColumnLandmarks.ColumnSaying("x").Along);
    }

    [Fact]
    public void BelowWantsARowLandmarkAndRightOfWantsAColumnLandmark()
    {
      // Refused where written, before any space is opened, naming what was handed and what was wanted.
      var below = Assert.Throws<ArgumentException>(() => Below(ColumnSaying("x")));
      var rightOf = Assert.Throws<ArgumentException>(() => RightOf(RowSaying("x")));

      Assert.Contains("finds a column where one that finds a row was wanted", below.Message);
      Assert.Equal("landmark", below.ParamName);
      Assert.Contains("finds a row where one that finds a column was wanted", rightOf.Message);
    }

    [Fact]
    public void OnAndUntilTakeEitherAxisAndFollowTheLandmarks()
    {
      // Occupancy and a bound have no direction of their own: the landmark carries it.
      Assert.Equal("3", On(RowSaying("3")).Of(AsText()).Map(Sheet()));
      Assert.Equal("2", On(ColumnSaying("2")).Of(AsText()).Map(Sheet()));
      Assert.Equal(1, Until(ColumnSaying("2")).Of(Row(strip => strip.Count)).Map(Sheet()));
      Assert.Equal(1, Until(RowSaying("3")).Of(Column(strip => strip.Count)).Map(Sheet()));
    }

    [Fact]
    public void AStripAndACompositionWantTheAxisTheirParameterNames()
    {
      var row = Assert.Throws<ArgumentException>(() => Row(TakeRows(1), strip => strip.Count));
      var column = Assert.Throws<ArgumentException>(() => Column(TakeColumns(1), strip => strip.Count));
      var composed = Assert.Throws<ArgumentException>(() => RowsThenColumns(TakeColumns(1), TakeRows(1)));

      Assert.Equal("columns", row.ParamName);
      Assert.Equal("rows", column.ParamName);
      Assert.Contains("a rule about columns where a rule about rows was wanted", composed.Message);
      Assert.Equal("rows", composed.ParamName);
    }
  }
}
