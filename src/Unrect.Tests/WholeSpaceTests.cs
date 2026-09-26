using Unrect.Core;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests
{
  /// <summary>
  /// The suite's own shortcut, held to its own claim. <see cref="WholeSpace"/> says one thing —
  /// "this whole space, as a region" — and six hundred strategy assertions are written through it,
  /// so if it ever came to mean something slightly else every one of them would quietly change what
  /// it was testing and none of them would fail.
  /// <para>
  /// Each entry point is therefore pinned against the spelling it abbreviates: <c>strategy.X(space)</c>
  /// must be <c>strategy.X(Plane&lt;ISpace&gt;.Of(space))</c>, and not merely something that agrees
  /// today. The fixtures are chosen so the two could disagree if the shortcut sliced, offset or
  /// bounded anything — a strategy that answers the full width and one that stops early, over a grid
  /// with content at both edges.
  /// </para>
  /// </summary>
  public class WholeSpaceTests
  {
    /// <summary>
    /// Four wide and four tall, with a blank band across row 2 and content again below it, so a
    /// while-rule stops in the middle and a to-rule and a landmark both have somewhere to land.
    /// A region narrower or shorter than this by any amount gives a different answer to every rule
    /// below, which is what makes the identity worth asserting rather than assuming.
    /// </summary>
    private static ICellSpace Sheet() => Mixed(new object?[,]
    {
      { "a", "b", 3, "d" },
      { "e", "f", 7, "h" },
      { null, null, null, null },
      { "Total", "n", 9, "p" },
    });

    private static Plane<ISpace> Region() => Plane<ISpace>.Of(Sheet());

    [Fact]
    public void RegionIsThePlaneOfTheWholeSpace()
    {
      // The claim the rest of the class rests on, stated first: the shortcut names the whole space
      // from its own corner and adds nothing. An origin or an extent off by one here would move
      // every assertion in the strategy suites at once.
      var space = Sheet();

      var region = space.Region();

      Assert.Same(space, region.Space);
      Assert.Equal(0, region.Origin.Width);
      Assert.Equal(0, region.Origin.Height);
      Assert.Equal(space.Area.Width, region.Area.Width);
      Assert.Equal(space.Area.Height, region.Area.Height);

      Assert.Equal(Plane<ISpace>.Of(space), region);
    }

    [Fact]
    public void GetSizeForwards()
    {
      var space = Sheet();
      var strategy = SizeStrategies.RowsWhileAnyValue();

      Assert.Equal(strategy.GetSize(Plane<ISpace>.Of(space)).Height, strategy.GetSize(space).Height);
      Assert.Equal(strategy.GetSize(Plane<ISpace>.Of(space)).Width, strategy.GetSize(space).Width);

      // Non-vacuity: the rule really does stop before the bottom, so a shortcut that had handed over
      // a shorter or taller region would show up as a different number rather than as the same one.
      Assert.Equal(2, strategy.GetSize(space).Height);
      Assert.Equal(4, strategy.GetSize(space).Width);
    }

    [Fact]
    public void GetAreaForwards()
    {
      var space = Sheet();
      var strategy = SizeStrategies.RowsWhileAnyValue().ToAreaStrategy();

      Assert.Equal(strategy.GetArea(Plane<ISpace>.Of(space)).Size, strategy.GetArea(space).Size);
      Assert.Equal(2, strategy.GetArea(space).Height);
    }

    [Fact]
    public void GetOffsetForwards()
    {
      // An offset that has to look at content to resolve, so the region it looks through matters.
      var space = Sheet();
      var strategy = OffsetStrategies.To(RowLandmarks.RowSaying("Total"));

      Assert.Equal(strategy.GetOffset(Plane<ISpace>.Of(space)).Size, strategy.GetOffset(space).Size);
      Assert.Equal(3, strategy.GetOffset(space).Height);
    }

    [Fact]
    public void SelectRowsForwards()
    {
      var space = Sheet();
      var strategy = RowStrategies.TakeRowsWhileAnyValue();

      Assert.Equal(strategy.SelectRows(Plane<ISpace>.Of(space)), strategy.SelectRows(space));
      Assert.Equal(2, strategy.SelectRows(space));
    }

    [Fact]
    public void SelectColumnsForwards()
    {
      var space = Sheet();
      var strategy = ColumnStrategies.TakeColumnsWhileAll(point => !point.IsBlank());

      Assert.Equal(strategy.SelectColumns(Plane<ISpace>.Of(space)), strategy.SelectColumns(space));

      // Row 2 is blank, so no column has a value in every row: the answer is zero, and a shortcut
      // that had trimmed the region to its valued rows would say four.
      Assert.Equal(0, strategy.SelectColumns(space));
    }

    [Fact]
    public void FindRowForwards()
    {
      var space = Sheet();
      var landmark = RowLandmarks.RowSaying("Total");

      Assert.Equal(landmark.FindRow(Plane<ISpace>.Of(space)), landmark.FindRow(space));
      Assert.Equal(3, landmark.FindRow(space));
    }

    [Fact]
    public void FindColumnForwards()
    {
      var space = Sheet();
      var landmark = ColumnLandmarks.ColumnSaying("d");

      Assert.Equal(landmark.FindColumn(Plane<ISpace>.Of(space)), landmark.FindColumn(space));
      Assert.Equal(3, landmark.FindColumn(space));
    }
  }
}
