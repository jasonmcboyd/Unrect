using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Placement is the anti-trap commitment: a projection's offset and area say where it sits in the
  /// space it is handed, and <see cref="ProjectionEngine"/> applies them exactly once, at every
  /// level, including the top-level <c>Map</c>. These tests pin that rule from both ends — that it
  /// is applied at the root at all, and that it is never applied twice.
  /// </summary>
  public class PlacementTests
  {
    // Values are (row * 10 + column), so every assertion reads as a coordinate.
    /// <summary>
    /// The shared coordinate grid, turned on its side: this file's placements read down a tall
    /// narrow sheet, where the other suites read across a wide one. Only the default differs.
    /// </summary>
    private static ISpace CoordinateGrid(int width = 3, int height = 4)
      => ProjectionTestSpaces.CoordinateGrid(width, height);

    private static IProjection<int> IntCell() => Cell(v => v.GetInt());

    // --- The placement is applied at the root ------------------------------------------------------

    [Fact]
    public void Map_AppliesTheProjectionsOwnOffsetAtTheTopLevel()
    {
      // A projection's placement is applied by one code path at every level, the top one included,
      // so a declaration means what it reads wherever it sits.
      Assert.Equal(11, IntCell().Down(1).Map(CoordinateGrid()));
      Assert.Equal(2, IntCell().Right(1).Map(CoordinateGrid()));
      Assert.Equal(12, IntCell().OffsetBy(Then(SkipRows(1), SkipColumns(1))).Map(CoordinateGrid()));
    }

    [Fact]
    public void Map_AppliesTheProjectionsOwnAreaAtTheTopLevel()
    {
      var block = Range(2, 3, b => (b.Width, b.Height)).Map(CoordinateGrid());

      Assert.Equal((2, 3), block);
    }

    [Fact]
    public void Map_AppliesADefaultedOffsetAtTheTopLevel()
    {
      // Table's default offset skips leading blank rows; at the root that is a real skip, not a
      // no-op.
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
    public void NestedProjection_HasItsOffsetAppliedExactlyOnce()
    {
      // A one-row offset inside a flow must move the child one row, not two. Applying it twice —
      // once to derive the available space and again to slice the extent — was the original trap.
      var second = IntCell().Down(1);
      var result = VerticalFlow(v => $"{v.Next(IntCell())}|{v.Next(second)}").Map(CoordinateGrid(width: 1));

      Assert.Equal("1|21", result);
    }

    [Fact]
    public void NestedProjection_HasItsOffsetAppliedOnceAtEveryDepth()
    {
      var lower = IntCell().Down(1);
      var inner = VerticalFlow(w => $"{w.Next(IntCell())}|{w.Next(lower)}").Down(1);
      var projection = VerticalFlow(v => $"{v.Next(IntCell())}/{v.Next(inner)}");

      // Outer child 1 sits at row 1; the inner flow's first cell at row 2 and its second at row 4.
      Assert.Equal("1/21|41", projection.Map(CoordinateGrid(width: 1, height: 5)));
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
    public void SelectThenOffsetBy_IsEquivalentToOffsetByThenSelect()
    {
      // The Select wrapper's placement is applied by the engine like any other projection's, so it
      // does not matter which side of the Select the modifier lands on.
      var space = Grid(new[,] { { 0 }, { 5 }, { 9 } });

      var selectThenOffset = IntCell().Select(v => v * 2).OffsetBy(SkipRows(1)).Apply(space);
      var offsetThenSelect = IntCell().OffsetBy(SkipRows(1)).Select(v => v * 2).Apply(space);

      Assert.Equal(10, selectThenOffset.Value);
      Assert.Equal(10, offsetThenSelect.Value);
      Assert.Equal(selectThenOffset.Advance.Height, offsetThenSelect.Advance.Height);
      Assert.Equal(selectThenOffset.Advance.Width, offsetThenSelect.Advance.Width);
    }

    [Fact]
    public void SelectSurvivesNamingAndPlacement()
    {
      var space = Grid(new[,] { { 0 }, { 7 } });

      var projection = IntCell().Select(v => v + 1).AfterBlankRows().Named("bumped");

      Assert.Equal(8, projection.Map(space));
      Assert.Equal("bumped", projection.Name);
    }

    // --- Movements compose; OffsetBy, the anchors and Sized replace -----------------------------------
    //
    // The anchors' half of the rule lives in AnchorModifierTests, beside what they anchor on; this
    // file pins the rule itself, through the modifier that states it with nothing else attached.

    [Fact]
    public void RepeatedOffsetModifiers_Compose()
    {
      // Down(1).Down(2) is a three-row offset: each movement carries on from where the projection
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
    public void OffsetBy_ReplacesAnyMovementsAlreadyApplied()
    {
      // OffsetBy is the "my start is where that resolves to" spelling: an assignment, so it
      // discards what came before rather than adding to it, which is also how a projection is told
      // to ignore an offset it defaults to.
      Assert.Equal(21, IntCell().Down(1).OffsetBy(SkipRows(2)).Map(CoordinateGrid(width: 1)));
      Assert.Equal(1, IntCell().Down(3).OffsetBy(SkipRows(0)).Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void AMovementComposesWithAProjectionsDefaultOffset()
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

      // ...and OffsetBy discards the default outright, landing exactly one row down.
      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).OffsetBy(SkipRows(1)).Map(space));
    }

    // --- Saying "no movement" out loud says nothing -----------------------------------------------------
    //
    // A projection that has not been placed takes a movement rather than composing with a phantom
    // no-op, and the test for "has not been placed" is a reference test against one canonical
    // no-movement. So MinOffset has to BE that value: two spellings of nothing would make
    // .OffsetBy(MinOffset()) a declared offset, and every later movement would compose onto it.
    //
    // What a naive probe cannot see: on a sheet where the movement fits, both spellings read the
    // same cell — a composite of "nothing, then one row down" resolves to one row down. The
    // difference is operational and diagnostic, so it is pinned as the mechanism (the shared
    // instance, the reference test) and at the one place it surfaces in an answer: the sentence a
    // movement that does not fit produces. A simplifier who weakens this to "reads the same cell"
    // has pinned nothing.

    [Fact]
    public void MinOffset_IsOneCanonicalNoMovement()
    {
      Assert.Same(OffsetStrategies.MinOffset(), OffsetStrategies.MinOffset());
      Assert.Same(OffsetStrategies.MinOffset(), Placement.Default.Offset);
    }

    [Fact]
    public void OffsetByMinOffset_DeclaresNoOffset()
    {
      Assert.False(IntCell().OffsetBy(OffsetStrategies.MinOffset()).Placement.HasDeclaredOffset);

      // ...while a second way of writing zero movement is a declared offset, because the rule is
      // about the canonical value and not about what an offset happens to resolve to.
      Assert.True(IntCell().OffsetBy(OffsetStrategies.ExplicitOffset(0, 0)).Placement.HasDeclaredOffset);
    }

    [Fact]
    public void AMovementAfterMinOffset_ReplacesItRatherThanComposingOntoIt()
    {
      // Composed, the movement would resolve inside a composite, and an offset that does not fit
      // would be reported by the composite's own bounds check — "ran past", with no extent named —
      // instead of by the engine, which says what was asked for. The value is the same either way;
      // the sentence is not, which is why this is the pin.
      var stated = Assert.Throws<ProjectionException>(() =>
        IntCell().OffsetBy(OffsetStrategies.MinOffset()).Down(5).Map(CoordinateGrid()));
      var bare = Assert.Throws<ProjectionException>(() => IntCell().Down(5).Map(CoordinateGrid()));

      Assert.Equal(bare.Message, stated.Message);
      Assert.Contains("an offset of 0x5 does not fit the available space", stated.Message);

      // The composing spelling, for contrast: the same movement, reported by the composite.
      var composed = Assert.Throws<ProjectionException>(() =>
        IntCell().OffsetBy(OffsetStrategies.ExplicitOffset(0, 0)).Down(5).Map(CoordinateGrid()));

      Assert.Contains("its offset ran past the available space", composed.Message);
    }

    [Fact]
    public void AMovementAfterARealOffset_StillComposes()
    {
      // The guard on the fix: canonicalising nothing must not turn every OffsetBy into a
      // replacement. A declared skip still carries the movement on from where it left off.
      var space = Grid(new[,] { { 0 }, { 0 }, { 1 }, { 2 } });

      Assert.Equal(1, IntCell().OffsetBy(OffsetStrategies.SkipBlankRows()).Map(space));
      Assert.Equal(2, IntCell().OffsetBy(OffsetStrategies.SkipBlankRows()).Down(1).Map(space));
    }

    [Fact]
    public void AMovementOnAnUnplacedProjection_SimplyTakesTheOffset()
    {
      // Nothing to carry on from, so the first movement is not composed with a phantom no-op.
      var applied = IntCell().Down(2).Apply(CoordinateGrid(width: 1));

      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(21, applied.Value);
    }

    [Fact]
    public void RepeatedSizeModifiers_KeepOnlyTheLast()
    {
      var projection = Range(b => (b.Width, b.Height))
        .Sized(AreaStrategies.ExplicitArea(3, 3))
        .Sized(AreaStrategies.ExplicitArea(2, 1));

      Assert.Equal((2, 1), projection.Map(CoordinateGrid()));
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

      Assert.Equal(7, IntCell().OffsetBy(Then(BlankRows(), SkipRows(1))).Map(space));
    }

    [Fact]
    public void Then_WithNoOffsets_IsTheOrigin()
    {
      Assert.Equal(1, IntCell().OffsetBy(Then()).Map(CoordinateGrid()));
    }

    // --- Projections are immutable values ----------------------------------------------------------------

    [Fact]
    public void Named_ReturnsANewProjectionAndLeavesTheOriginalUnnamed()
    {
      var original = IntCell();
      var named = original.Named("code");

      Assert.NotSame(original, named);
      Assert.Null(original.Name);
      Assert.Equal("code", named.Name);
    }

    [Fact]
    public void AMovement_ReturnsANewProjectionAndLeavesTheOriginalPlacement()
    {
      var original = IntCell();
      var moved = original.Down(1);

      Assert.NotSame(original, moved);
      Assert.NotSame(original.Placement, moved.Placement);
      Assert.Equal(1, original.Map(CoordinateGrid(width: 1)));
      Assert.Equal(11, moved.Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void Sized_ReturnsANewProjectionAndLeavesTheOriginalArea()
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
    public void ModifiersRejectANullProjection()
    {
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<int>)null!).Named("x")).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<int>)null!).OffsetBy(SkipRows(1))).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<int>)null!).Sized(AreaStrategies.MaxArea())).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<int>)null!).Select(v => v)).ParamName);
    }

    [Fact]
    public void MovingAProjectionANegativeDistance_IsRejected()
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
      Assert.Null(VerticalRepeat(IntCell()).Placement.Area);
      Assert.NotNull(IntCell().Placement.Area);
    }
  }
}
