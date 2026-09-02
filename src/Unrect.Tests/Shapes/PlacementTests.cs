using System;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Placement is the anti-trap commitment: a shape's offset and area say where it sits in the space
  /// it is handed, and <see cref="ShapeEngine"/> applies them exactly once, at every level, including
  /// the top-level <c>Map</c>. These tests pin that rule from both ends — that it is applied at the
  /// root at all, and that it is never applied twice.
  /// </summary>
  public class PlacementTests
  {
    // Values are (row * 10 + column), so every assertion reads as a coordinate.
    /// <summary>
    /// The shared coordinate grid, turned on its side: this file's placements read down a tall
    /// narrow sheet, where the other suites read across a wide one. Only the default differs.
    /// </summary>
    private static ISpace CoordinateGrid(int width = 3, int height = 4)
      => ShapeTestSpaces.CoordinateGrid(width, height);

    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    // --- The placement is applied at the root ------------------------------------------------------

    [Fact]
    public void Map_AppliesTheShapesOwnOffsetAtTheTopLevel()
    {
      // A shape's placement is applied by one code path at every level, the top one included, so a
      // declaration means what it reads wherever it sits.
      Assert.Equal(11, IntCell().Down(1).Map(CoordinateGrid()));
      Assert.Equal(2, IntCell().Right(1).Map(CoordinateGrid()));
      Assert.Equal(12, IntCell().After(Then(SkipRows(1), SkipColumns(1))).Map(CoordinateGrid()));
    }

    [Fact]
    public void Map_AppliesTheShapesOwnAreaAtTheTopLevel()
    {
      var block = Range(2, 3, b => (b.Width, b.Height)).Map(CoordinateGrid());

      Assert.Equal((2, 3), block);
    }

    [Fact]
    public void Map_AppliesADefaultedOffsetAtTheTopLevel()
    {
      // Table's default offset skips leading blank rows; at the root that is a real skip, not a no-op.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { null, null },
        { "Name", "Amount" },
        { "Acme", 10 },
      });

      Assert.Equal(new[] { "Name", "Amount" }, Table(t => t.ColumnNames).Map(space));
    }

    [Fact]
    public void Apply_ReportsTheResolvedOffsetAndConsumedExtent()
    {
      var applied = IntCell().Down(2).Apply(CoordinateGrid());

      Assert.Equal(21, applied.Value);
      Assert.Equal(0, applied.Offset.Size.Width);
      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
      Assert.Equal(1, applied.Advance.Width);
      Assert.Equal(3, applied.Advance.Height);
    }

    // --- The placement is applied exactly once -----------------------------------------------------

    [Fact]
    public void NestedShape_HasItsOffsetAppliedExactlyOnce()
    {
      // A one-row offset inside a flow must move the child one row, not two. Applying it twice —
      // once to derive the available space and again to slice the extent — was the original trap.
      var second = IntCell().Down(1);
      var result = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(second)}").Map(CoordinateGrid(width: 1));

      Assert.Equal("1|21", result);
    }

    [Fact]
    public void NestedShape_HasItsOffsetAppliedOnceAtEveryDepth()
    {
      var lower = IntCell().Down(1);
      var inner = VerticalFlow(w => $"{w.Next(IntCell())}|{w.Next(lower)}").Down(1);
      var shape = VerticalFlow(v => $"{v.Next(IntCell())}/{v.Next(inner)}");

      // Outer child 1 sits at row 1; the inner flow's first cell at row 2 and its second at row 4.
      Assert.Equal("1/21|41", shape.Map(CoordinateGrid(width: 1, height: 5)));
    }

    // --- Offset on the flow versus offset on the first child ---------------------------------------

    [Fact]
    public void OffsetOnTheFlow_PositionsTheWholeFlow()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var applied = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}").AfterBlankRows().Apply(space);

      Assert.Equal("1|2", applied.Value);
      Assert.Equal(1, applied.Offset.Size.Height);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void OffsetOnTheFirstChild_PositionsThatChildOnly()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var first = IntCell().AfterBlankRows();
      var applied = VerticalFlow(v => $"{v.Next(first)}|{v.Next(IntCell())}").Apply(space);

      // Same values, but the flow itself starts at the origin and therefore consumes the blank row.
      Assert.Equal("1|2", applied.Value);
      Assert.Equal(0, applied.Offset.Size.Height);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void BothSpellings_LandTheContentInTheSamePlace()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var first = IntCell().AfterBlankRows();
      var onFlow = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(IntCell())}").AfterBlankRows().Apply(space);
      var onChild = VerticalFlow(v => $"{v.Next(first)}|{v.Next(IntCell())}").Apply(space);

      Assert.Equal(onFlow.Value, onChild.Value);
      Assert.Equal(onFlow.Advance.Height, onChild.Advance.Height);
    }

    // --- Select commutes with the placement modifiers ----------------------------------------------

    [Fact]
    public void SelectThenAfter_IsEquivalentToAfterThenSelect()
    {
      // The Select wrapper's placement is applied by the engine like any other shape's, so it does
      // not matter which side of the Select the modifier lands on.
      var space = Grid(new[,] { { 0 }, { 5 }, { 9 } });

      var selectThenAfter = IntCell().Select(v => v * 2).After(SkipRows(1)).Apply(space);
      var afterThenSelect = IntCell().After(SkipRows(1)).Select(v => v * 2).Apply(space);

      Assert.Equal(10, selectThenAfter.Value);
      Assert.Equal(10, afterThenSelect.Value);
      Assert.Equal(selectThenAfter.Advance.Height, afterThenSelect.Advance.Height);
      Assert.Equal(selectThenAfter.Advance.Width, afterThenSelect.Advance.Width);
    }

    [Fact]
    public void SelectSurvivesNamingAndPlacement()
    {
      var space = Grid(new[,] { { 0 }, { 7 } });

      var shape = IntCell().Select(v => v + 1).AfterBlankRows().Named("bumped");

      Assert.Equal(8, shape.Map(space));
      Assert.Equal("bumped", shape.Name);
    }

    // --- Movements compose; After and Sized replace --------------------------------------------------

    [Fact]
    public void RepeatedOffsetModifiers_Compose()
    {
      // Down(1).Down(2) is a three-row offset: each movement carries on from where the shape
      // already sits, so the modifiers read as a sequence of steps rather than a last-one-wins.
      Assert.Equal(31, IntCell().Down(1).Down(2).Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void CrossAxisModifiers_ComposeIntoADiagonalAnchor()
    {
      // "Down one and right one", in either spelling — not "right one".
      Assert.Equal(12, IntCell().Down(1).Right(1).Map(CoordinateGrid()));
      Assert.Equal(12, IntCell().Right(1).Down(1).Map(CoordinateGrid()));
    }

    [Fact]
    public void After_ReplacesAnyMovementsAlreadyApplied()
    {
      // After is the "put it exactly here" spelling: it discards what came before rather than
      // adding to it, which is also how a shape is told to ignore an offset it defaults to.
      Assert.Equal(21, IntCell().Down(1).After(SkipRows(2)).Map(CoordinateGrid(width: 1)));
      Assert.Equal(1, IntCell().Down(3).After(SkipRows(0)).Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void AMovementComposesWithAShapesDefaultOffset()
    {
      // A Table already skips the blank rows in front of it; Down(1) carries on one row further.
      // Were the modifier to replace, it would land on the blank row's successor instead.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { "Investor", "Amount" },
        { "Acme", "10" },
        { "Beta", "20" },
      });

      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).Map(space));
      Assert.Equal(new[] { "Acme", "10" }, Table(t => t.ColumnNames).Down(1).Map(space));

      // ...and After discards the default outright, landing exactly one row down.
      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).After(SkipRows(1)).Map(space));
    }

    [Fact]
    public void AMovementOnAnUnplacedShape_SimplyTakesTheOffset()
    {
      // Nothing to carry on from, so the first movement is not composed with a phantom no-op.
      var applied = IntCell().Down(2).Apply(CoordinateGrid(width: 1));

      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(21, applied.Value);
    }

    [Fact]
    public void RepeatedSizeModifiers_KeepOnlyTheLast()
    {
      var shape = Range(b => (b.Width, b.Height))
        .Sized(AreaStrategies.ExplicitArea(3, 3))
        .Sized(AreaStrategies.ExplicitArea(2, 1));

      Assert.Equal((2, 1), shape.Map(CoordinateGrid()));
    }

    [Fact]
    public void RepeatedNames_KeepOnlyTheLast()
    {
      Assert.Equal("second", IntCell().Named("first").Named("second").Name);
    }

    [Fact]
    public void Then_ComposesOffsetsAgainstTheSpaceEachOneLeaves()
    {
      // "Past the blank band, then one row further."
      var space = Grid(new[,] { { 0 }, { 0 }, { 9 }, { 7 } });

      Assert.Equal(7, IntCell().After(Then(BlankRows(), SkipRows(1))).Map(space));
    }

    [Fact]
    public void Then_WithNoOffsets_IsTheOrigin()
    {
      Assert.Equal(1, IntCell().After(Then()).Map(CoordinateGrid()));
    }

    // --- Shapes are immutable values ----------------------------------------------------------------

    [Fact]
    public void Named_ReturnsANewShapeAndLeavesTheOriginalUnnamed()
    {
      var original = IntCell();
      var named = original.Named("code");

      Assert.NotSame(original, named);
      Assert.Null(original.Name);
      Assert.Equal("code", named.Name);
    }

    [Fact]
    public void After_ReturnsANewShapeAndLeavesTheOriginalPlacement()
    {
      var original = IntCell();
      var moved = original.Down(1);

      Assert.NotSame(original, moved);
      Assert.NotSame(original.Placement, moved.Placement);
      Assert.Equal(1, original.Map(CoordinateGrid(width: 1)));
      Assert.Equal(11, moved.Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void Sized_ReturnsANewShapeAndLeavesTheOriginalArea()
    {
      var original = Range(b => (b.Width, b.Height));
      var resized = original.Sized(AreaStrategies.ExplicitArea(1, 1));

      Assert.Equal((3, 4), original.Map(CoordinateGrid()));
      Assert.Equal((1, 1), resized.Map(CoordinateGrid()));
    }

    [Fact]
    public void WithName_RejectsNull()
    {
      Assert.Throws<ArgumentNullException>(() => IntCell().WithName(null!));
    }

    [Fact]
    public void WithPlacement_RejectsNull()
    {
      Assert.Throws<ArgumentNullException>(() => IntCell().WithPlacement(null!));
    }

    // --- Placement itself ---------------------------------------------------------------------------

    [Fact]
    public void PlacementDefault_HasNoDeclaredArea()
    {
      Assert.Null(Placement.Default.Area);
      Assert.NotNull(Placement.Default.Offset);
    }

    [Fact]
    public void PlacementOf_DeclaresAnAreaAtTheOrigin()
    {
      var area = AreaStrategies.ExplicitArea(2, 2);
      var placement = Placement.Of(area);

      Assert.Same(area, placement.Area);
    }

    [Fact]
    public void PlacementModifiers_ReturnNewInstances()
    {
      var offset = OffsetStrategies.ExplicitOffset(1, 1);
      var area = AreaStrategies.ExplicitArea(2, 2);

      var withOffset = Placement.Default.WithOffset(offset);
      var withArea = withOffset.WithArea(area);

      Assert.Same(offset, withOffset.Offset);
      Assert.Null(withOffset.Area);
      Assert.Same(offset, withArea.Offset);
      Assert.Same(area, withArea.Area);
      Assert.Null(Placement.Default.Area);
    }

    [Fact]
    public void Placement_RejectsANullOffset()
    {
      Assert.Throws<ArgumentNullException>(() => new Placement(null!, null));
    }

    // --- Argument guards blame the parameter the caller wrote --------------------------------------------

    [Fact]
    public void Sized_RejectsANullArea()
    {
      // Only the constructor may take a null area, where it means "derive the extent". Anywhere
      // else a null would quietly turn a declared extent into a derived one.
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => IntCell().Sized(null!)).ParamName);
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => Placement.Default.WithArea(null!)).ParamName);
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => Placement.Of(null!)).ParamName);
    }

    [Fact]
    public void ModifiersRejectANullShape()
    {
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Named("x")).ParamName);
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).After(SkipRows(1))).ParamName);
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Sized(AreaStrategies.MaxArea())).ParamName);
      Assert.Equal("shape", Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Select(v => v)).ParamName);
    }

    [Fact]
    public void MovingAShapeANegativeDistance_IsRejected()
    {
      Assert.Equal("rows", Assert.Throws<ArgumentOutOfRangeException>(() => IntCell().Down(-1)).ParamName);
      Assert.Equal("columns", Assert.Throws<ArgumentOutOfRangeException>(() => IntCell().Right(-1)).ParamName);
      Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => SkipRows(-1)).ParamName);
      Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => SkipColumns(-1)).ParamName);
    }

    [Fact]
    public void FlowsAndRepeatsDeriveTheirExtent()
    {
      // A null Area is what lets a flow size itself from its children — and therefore what lets a
      // Repeat item be declared without any placement at all.
      Assert.Null(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Placement.Area);
      Assert.Null(Repeat(IntCell()).Placement.Area);
      Assert.NotNull(IntCell().Placement.Area);
    }
  }
}
