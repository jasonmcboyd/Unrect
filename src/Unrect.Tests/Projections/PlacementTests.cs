using System;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Placement is the anti-trap commitment: a projection's offset and area say where it sits in the
  /// space it is handed, and the engine's placement machine applies them exactly once, at every
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
    private static ISheetCells CoordinateGrid(int width = 3, int height = 4)
      => ProjectionTestSpaces.CoordinateGrid(width, height);

    // --- The placement is applied at the root ------------------------------------------------------

    [Fact]
    public void Map_AppliesTheProjectionsOwnOffsetAtTheTopLevel()
    {
      // A projection's placement is applied by one code path at every level, the top one included,
      // so a declaration means what it reads wherever it sits.
      Assert.Equal(11, Down(1).Of(IntCell()).Map(CoordinateGrid()));
      Assert.Equal(2, Right(1).Of(IntCell()).Map(CoordinateGrid()));
      Assert.Equal(12, OffsetBy(Then(SkipRows(1), SkipColumns(1))).Of(IntCell()).Map(CoordinateGrid()));
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
      var applied = Down(2).Of(IntCell()).Apply(CoordinateGrid());

      Assert.Equal(21, applied.Value);
      Assert.Equal(0, applied.Offset.Size.Width);
      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
      Assert.Equal(1, applied.Advance.Width);
      Assert.Equal(3, applied.Advance.Height);
    }

    // --- Whether a placement fits is asked a row at a time -----------------------------------------
    //
    // The fit test reads the available space through the forward probes (its width, and whether it
    // has a row at the far edge of what is being asked for) rather than off ISheetCells.Area, so that an
    // extent still being discovered is asked for one row instead of for all of them. What it ANSWERS
    // must not depend on which kind of space it was asked about — so every case below is asserted
    // twice, over a measured grid and over a bound the engine is discovering, and the boundary case
    // is in the table on purpose: an extent exactly as tall as what is available fits.

    [Theory]
    [InlineData(3, 4, true)]      // the whole extent, which is the boundary case: equal fits
    [InlineData(3, 2, true)]
    [InlineData(4, 4, false)]     // one column too wide
    [InlineData(3, 5, false)]     // one row too tall
    public void AnExtentFitsWhenItIsNoBiggerThanTheSpaceAvailable(int width, int height, bool fits)
    {
      var extent = $"{width}x{height}";
      var block = Range(width, height, b => $"{b.Width}x{b.Height}");

      AssertFit(fits, extent, block, CoordinateGrid());

      // The same declaration inside a discovered bound of exactly the same 3x4, walked a row at a
      // time. RowsWhileAnyValue takes every row of the coordinate grid, so the two spaces differ in
      // how their extent is arrived at and in nothing else.
      AssertFit(fits, extent, Sized(RowsWhileAnyValue()).Of(VerticalFlow(v =>
      {
        var block2 = v.Next(block);

        return v.Build(read => read.Of(block2));
      })), CoordinateGrid());
    }

    private static void AssertFit(bool fits, string extent, IProjectionDefinition<ISheetCells, string> declaration, ISheetCells space)
    {
      if (fits)
      {
        Assert.Equal(extent, declaration.Map(space));

        return;
      }

      var failure = Assert.Throws<ProjectionException>(() => declaration.Map(space));

      Assert.Contains($"an extent of {extent} does not fit here", failure.Message);
    }

    // --- The placement is applied exactly once -----------------------------------------------------

    [Fact]
    public void NestedProjection_HasItsOffsetAppliedExactlyOnce()
    {
      // A one-row offset inside a flow must move the child one row, not two. Applying it twice —
      // once to derive the available space and again to slice the extent — was the original trap.
      var second = Down(1).Of(IntCell());
      var result = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var second2 = v.Next(second);

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(second2)}");
      }).Map(CoordinateGrid(width: 1));

      Assert.Equal("1|21", result);
    }

    [Fact]
    public void NestedProjection_HasItsOffsetAppliedOnceAtEveryDepth()
    {
      var lower = Down(1).Of(IntCell());
      var inner = Down(1).Of(VerticalFlow(w =>
      {
        var intCell = w.Next(IntCell());
        var lower2 = w.Next(lower);

        return w.Build(read => $"{read.Of(intCell)}|{read.Of(lower2)}");
      }));
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var inner2 = v.Next(inner);

        return v.Build(read => $"{read.Of(intCell)}/{read.Of(inner2)}");
      });

      // Outer child 1 sits at row 1; the inner flow's first cell at row 2 and its second at row 4.
      Assert.Equal("1/21|41", projection.Map(CoordinateGrid(width: 1, height: 5)));
    }

    // --- Offset on the flow versus offset on the first child ---------------------------------------

    [Fact]
    public void OffsetOnTheFlow_PositionsTheWholeFlow()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var applied = AfterBlankRows().Of(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      })).Apply(space);

      Assert.Equal("1|2", applied.Value);
      Assert.Equal(1, applied.Offset.Size.Height);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void OffsetOnTheFirstChild_PositionsThatChildOnly()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var first = AfterBlankRows().Of(IntCell());
      var applied = VerticalFlow(v =>
      {
        var first2 = v.Next(first);
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(first2)}|{read.Of(intCell)}");
      }).Apply(space);

      // Same values, but the flow itself starts at the origin and therefore consumes the blank row.
      Assert.Equal("1|2", applied.Value);
      Assert.Equal(0, applied.Offset.Size.Height);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void BothSpellings_LandTheContentInTheSamePlace()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 }, { 0 } });

      var first = AfterBlankRows().Of(IntCell());
      var onFlow = AfterBlankRows().Of(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      })).Apply(space);
      var onChild = VerticalFlow(v =>
      {
        var first2 = v.Next(first);
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(first2)}|{read.Of(intCell)}");
      }).Apply(space);

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

      var selectThenOffset = OffsetBy(SkipRows(1)).Of(IntCell().Select(v => v * 2)).Apply(space);
      var offsetThenSelect = OffsetBy(SkipRows(1)).Of(IntCell()).Select(v => v * 2).Apply(space);

      Assert.Equal(10, selectThenOffset.Value);
      Assert.Equal(10, offsetThenSelect.Value);
      Assert.Equal(selectThenOffset.Advance.Height, offsetThenSelect.Advance.Height);
      Assert.Equal(selectThenOffset.Advance.Width, offsetThenSelect.Advance.Width);
    }

    [Fact]
    public void SelectSurvivesNamingAndPlacement()
    {
      var space = Grid(new[,] { { 0 }, { 7 } });

      var projection = AfterBlankRows().Of(IntCell().Select(v => v + 1)).Named("bumped");

      Assert.Equal(8, projection.Map(space));
      Assert.Equal("bumped", projection.Name);
    }

    // --- Movements compose; OffsetBy, the anchors and Sized replace a DEFAULT and refuse a declaration ---
    //
    // The anchors' half of the rule lives in AnchorModifierTests, beside what they anchor on; this
    // file pins the rule itself, through the modifier that states it with nothing else attached.
    // What a placement replaces is the projection's own definition — a Table's skipped blank rows, a
    // Range's constructor extent — never a second modifier; that is refused at construction.

    [Fact]
    public void RepeatedOffsetModifiers_Compose()
    {
      // Down(1).Down(2) is a three-row offset: each movement carries on from where the projection
      // already sits, so the modifiers read as a sequence of steps rather than a last-one-wins.
      Assert.Equal(31, Down(1).Down(2).Of(IntCell()).Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void CrossAxisModifiers_ComposeIntoADiagonalAnchor()
    {
      // "Down one and right one", in either spelling — not "right one".
      Assert.Equal(12, Down(1).Right(1).Of(IntCell()).Map(CoordinateGrid()));
      Assert.Equal(12, Right(1).Down(1).Of(IntCell()).Map(CoordinateGrid()));
    }

    [Fact]
    public void AMovementDeclaresThePositionOnceAndComposesFromThere()
    {
      // A second statement of a position over a movement — Down(1).OffsetBy(SkipRows(2)) — was a
      // runtime refusal while placement rode postfix modifiers; it is now a COMPILE-time refusal
      // (OffsetBy is an entry, not a stage), pinned in
      // spike/PlacementGauntlet/MustNotCompilePipeline.cs (AA). What survives is the composing
      // spelling: the one declared movement stands, measured from the projection's start.
      Assert.Equal(21, Down(2).Of(IntCell()).Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void AMovementReplacesAProjectionsDefaultOffset()
    {
      // The law (spec §7): a pipeline offset REPLACES the shape's own constructor default; it does
      // not compose onto it. A Table's default self-locates onto the first non-blank cell
      // (SkipToFirstNonBlankCell); a bare Down(1) discards that self-locate and lands one row down
      // from the origin — the header row here, not the data row a composing spelling would reach.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { "Investor", "Amount" },
        { "Acme", "10" },
        { "Beta", "20" },
      });

      // The default self-locates past the leading blank row onto the header.
      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).Map(space));

      // Down(1) REPLACES the self-locate default: row 1 from the origin is the header, not the data
      // row (["Acme", "10"]) the old composing semantics reached.
      Assert.Equal(new[] { "Investor", "Amount" }, Down(1).Of(Table(t => t.ColumnNames)).Map(space));

      // ...and OffsetBy discards the default outright likewise, landing exactly one row down.
      Assert.Equal(new[] { "Investor", "Amount" }, OffsetBy(SkipRows(1)).Of(Table(t => t.ColumnNames)).Map(space));

      // The other half of the law: composition happens only when offsets are explicitly chained in
      // the pipeline. Making the self-locate explicit marks the offset declared, so Down(1) then
      // COMPOSES onto it — self-locate to the header, then one row down to the first data row.
      Assert.Equal(
        new[] { "Acme", "10" },
        SkipToFirstNonBlankCell().Down(1).Of(Table(t => t.ColumnNames)).Map(space));
    }

    [Fact]
    public void TheReplaceLaw_OnTheColumnAxis_TheThreeCanonicalScenarios()
    {
      // Spec §7's three canonical scenarios, on the column axis so replace and compose land on
      // different columns. Content is indented one column (the leftmost column is entirely blank),
      // so a bare Right(1) that REPLACES the self-locate reads a different column set than one that
      // COMPOSES onto it.
      var space = Mixed(new object?[,]
      {
        { null, "Investor", "Amount" },
        { null, "Acme", 10 },
        { null, "Beta", 20 },
      });

      // (1) Table() with no pipeline offset: SkipToFirstNonBlankCell self-locates onto the first
      // content cell (column 1), so the discovered block reads both content columns.
      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).Map(space));

      // (2) Right(1).Of(Table()) REPLACES the default: the table's left edge is at column 1, NOT
      // self-locate + 1. It reads the same two columns as the default. Were Right(1) to compose onto
      // the self-locate (the old semantics), the origin would be column 2 and the read would be the
      // single column ["Amount"].
      Assert.Equal(new[] { "Investor", "Amount" }, Right(1).Of(Table(t => t.ColumnNames)).Map(space));

      // (3) SkipToFirstNonBlankCell().Right(1).Of(Table()) COMPOSES: the explicit skip replaces the
      // default and marks the offset declared, then Right(1) composes onto it — self-locate to
      // column 1, then one column right to column 2 — reading the single column ["Amount"].
      Assert.Equal(
        new[] { "Amount" },
        SkipToFirstNonBlankCell().Right(1).Of(Table(t => t.ColumnNames)).Map(space));
    }

    [Fact]
    public void TheReplaceLaw_HoldsUniformlyForEveryShapeWithAConstructorDefault()
    {
      // Spec §7: "The law applies to EVERY one of them uniformly." Exactly three shapes carry a
      // non-trivial constructor default — Table (SkipToFirstNonBlankCell), Caption (To(RowContaining))
      // and Fields (Then(To(ColumnWhere), To(RowWhere))) — and all three obey the one rule: a bare
      // declared movement REPLACES the default and starts the chain from the origin. Pinned side by
      // side here so the "uniform across defaulted shapes" claim lives in one place (Caption and
      // Fields each also carry their own pin in their own suites).
      //
      // Every fixture is DISCRIMINATING: row 0 is junk/blank so the default self-locates to row 1,
      // where a COMPOSING Down(1) would reach row 2 while a REPLACING Down(1) lands at row 1 from the
      // origin. The three read different content under the two semantics, so none can pass under both.

      // Table: the default self-locates onto the first content row (the header at row 1). Down(1)
      // replaces -> row 1 -> the header binds as the first content row; a compose would reach row 2
      // and bind ["Acme", "10"] as the header.
      var tableSheet = Mixed(new object?[,]
      {
        { null, null },
        { "Investor", "Amount" },
        { "Acme", "10" },
      });
      Assert.Equal(new[] { "Investor", "Amount" }, Down(1).Of(Table(t => t.ColumnNames)).Map(tableSheet));

      // Caption: the default seeks the row containing its text (row 1). Down(1) replaces -> asserts
      // at row 1 -> the verbatim "  EIN:  "; a compose would reach row 2 and yield the verbatim "EIN:".
      var captionSheet = Mixed(new object?[,]
      {
        { "junk" },
        { "  EIN:  " },
        { "EIN:" },
      });
      Assert.Equal("  EIN:  ", Down(1).Of(Caption("ein:")).Map(captionSheet));

      // Fields: the default self-anchors on the first label (row 1). Down(1) replaces -> reads the
      // row-1 card ("target"); a compose would anchor to row 1 then reach row 2 ("other").
      var fieldsSheet = Mixed(new object?[,]
      {
        { null, null },
        { "EIN:", "target" },
        { "EIN:", "other" },
      });
      Assert.Equal("target", Down(1).Of(Fields(Field("EIN"))).Map(fieldsSheet)["EIN"].Text());
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
      Assert.False(OffsetBy(OffsetStrategies.MinOffset()).Of(IntCell()).Placement.HasDeclaredOffset);

      // ...while a second way of writing zero movement is a declared offset, because the rule is
      // about the canonical value and not about what an offset happens to resolve to.
      Assert.True(OffsetBy(OffsetStrategies.ExplicitOffset(0, 0)).Of(IntCell()).Placement.HasDeclaredOffset);
    }

    [Fact]
    public void AMovementAfterMinOffset_ReplacesItRatherThanComposingOntoIt()
    {
      // Composed, the movement would resolve inside a composite, and an offset that does not fit
      // would be reported by the composite's own bounds check — "ran past", with no extent named —
      // instead of by the engine, which says what was asked for. The value is the same either way;
      // the sentence is not, which is why this is the pin.
      var stated = Assert.Throws<ProjectionException>(() =>
        OffsetBy(OffsetStrategies.MinOffset()).Down(5).Of(IntCell()).Map(CoordinateGrid()));
      var bare = Assert.Throws<ProjectionException>(() => Down(5).Of(IntCell()).Map(CoordinateGrid()));

      Assert.Equal(bare.Message, stated.Message);
      Assert.Contains("an offset of 0x5 does not fit the available space", stated.Message);

      // The composing spelling, for contrast: the same movement, reported by the composite.
      var composed = Assert.Throws<ProjectionException>(() =>
        OffsetBy(OffsetStrategies.ExplicitOffset(0, 0)).Down(5).Of(IntCell()).Map(CoordinateGrid()));

      Assert.Contains("its offset ran past the available space", composed.Message);
    }

    [Fact]
    public void AMovementAfterARealOffset_StillComposes()
    {
      // The guard on the fix: canonicalising nothing must not turn every OffsetBy into a
      // replacement. A declared skip still carries the movement on from where it left off.
      var space = Grid(new[,] { { 0 }, { 0 }, { 1 }, { 2 } });

      Assert.Equal(1, OffsetBy(OffsetStrategies.SkipBlankRows()).Of(IntCell()).Map(space));
      Assert.Equal(2, OffsetBy(OffsetStrategies.SkipBlankRows()).Down(1).Of(IntCell()).Map(space));
    }

    [Fact]
    public void AMovementOnAnUnplacedProjection_SimplyTakesTheOffset()
    {
      // Nothing to carry on from, so the first movement is not composed with a phantom no-op.
      var applied = Down(2).Of(IntCell()).Apply(CoordinateGrid(width: 1));

      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(21, applied.Value);
    }

    // Two extents — Sized(3,3).Sized(2,1) — do not stack, and the contradiction is now caught at
    // COMPILE time (a size-set stage refuses a second Sized), pinned in
    // spike/PlacementGauntlet/MustNotCompilePipeline.cs (AB). The silent-replacement-of-a-default
    // path that survives is Sized_ReplacesAShapesOwnExtentWithoutAWord, below.

    [Fact]
    public void Sized_ReplacesAShapesOwnExtentWithoutAWord()
    {
      // The extent family's default-path guard, and the discriminator the refusal above turns on: an
      // extent a shape states in its own constructor is part of its definition, not a statement
      // about this use of it, so .Sized is free to replace it and says nothing. Only a second
      // .Sized — a modifier over a modifier — is the contradiction.
      Assert.Equal((3, 1), Range(3, 1, b => (b.Width, b.Height)).Map(CoordinateGrid()));
      Assert.Equal(
        (1, 2),
        Sized(AreaStrategies.ExplicitArea(1, 2)).Of(Range(3, 1, b => (b.Width, b.Height))).Map(CoordinateGrid()));

      // ...and a derived extent likewise: FlowProjectionTests.Sized_OverridesWhatTheFlowDerived and
      // OverlayProjectionTests.Sized_OverridesTheBoundingBox are the composite half of the same
      // claim.
      Assert.Equal((2, 1), Sized(AreaStrategies.ExplicitArea(2, 1)).Of(Range(b => (b.Width, b.Height))).Map(CoordinateGrid()));
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

      Assert.Equal(7, OffsetBy(Then(BlankRows(), SkipRows(1))).Of(IntCell()).Map(space));
    }

    [Fact]
    public void Then_WithNoOffsets_IsTheOrigin()
    {
      Assert.Equal(1, OffsetBy(Then()).Of(IntCell()).Map(CoordinateGrid()));
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
      var moved = Down(1).Of(original);

      Assert.NotSame(original, moved);
      Assert.NotSame(original.Placement, moved.Placement);
      Assert.Equal(1, original.Map(CoordinateGrid(width: 1)));
      Assert.Equal(11, moved.Map(CoordinateGrid(width: 1)));
    }

    [Fact]
    public void Sized_ReturnsANewProjectionAndLeavesTheOriginalArea()
    {
      var original = Range(b => (b.Width, b.Height));
      var resized = Sized(AreaStrategies.ExplicitArea(1, 1)).Of(original);

      Assert.Equal((3, 4), original.Map(CoordinateGrid()));
      Assert.Equal((1, 1), resized.Map(CoordinateGrid()));
    }

    [Fact]
    public void With_RejectsNull()
    {
      Assert.Throws<ArgumentNullException>(() => IntCell().With(null!));
    }

    [Fact]
    public void AnnotationsRejectNull()
    {
      Assert.Throws<ArgumentNullException>(() => Annotations.Default.WithName(null!));
      Assert.Throws<ArgumentNullException>(() => Annotations.Default.WithUnitName(null!));
      Assert.Throws<ArgumentNullException>(() => Annotations.Default.WithPlacement(null!));
    }

    [Fact]
    public void AnnotationsAreOneRecordReadThroughTheFace()
    {
      var projection = Sized(AreaStrategies.ExplicitArea(1, 1)).Of(IntCell()).Named("cell").AsUnit("Unit").AsScaffolding();

      Assert.Same(projection.Annotations.Placement, projection.Placement);
      Assert.Equal("cell", projection.Annotations.Name);
      Assert.Equal("Unit", projection.Annotations.UnitName);
      Assert.True(projection.Annotations.IsScaffolding);
      Assert.Same(projection.Annotations, projection.With(projection.Annotations).Annotations);
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
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => Sized((Unrect.Core.IAreaStrategy)null!).Of(IntCell())).ParamName);
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => Placement.Default.WithArea(null!)).ParamName);
      Assert.Equal("area", Assert.Throws<ArgumentNullException>(() => Placement.Of(null!)).ParamName);
    }

    [Fact]
    public void ModifiersRejectANullProjection()
    {
      // The post-read modifiers keep their null-projection guard, and so now does the geometry
      // pipeline's terminal: .Of blames "projection" at construction rather than letting a null
      // surface later as a NullReferenceException when the steps run. Both the offset door
      // (OffsetBy) and the extent door (Sized) reach that one guard.
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjectionDefinition<ISheetCells, int>)null!).Named("x")).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjectionDefinition<ISheetCells, int>)null!).Select(v => v)).ParamName);
      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentNullException>(() => OffsetBy(SkipRows(1)).Of<int>(null!)).ParamName);
      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentNullException>(() => Sized(AreaStrategies.ExplicitArea(1, 1)).Of<int>(null!)).ParamName);
    }

    [Fact]
    public void MovingAProjectionANegativeDistance_IsRejected()
    {
      Assert.Equal("rows", Assert.Throws<ArgumentOutOfRangeException>(() => Down(-1).Of(IntCell())).ParamName);
      Assert.Equal("columns", Assert.Throws<ArgumentOutOfRangeException>(() => Right(-1).Of(IntCell())).ParamName);
      Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => SkipRows(-1)).ParamName);
      Assert.Equal("count", Assert.Throws<ArgumentOutOfRangeException>(() => SkipColumns(-1)).ParamName);
    }

    [Fact]
    public void FlowsAndRepeatsDeriveTheirExtent()
    {
      // A null Area is what lets a flow size itself from its children — and therefore what lets a
      // Repeat item be declared without any placement at all.
      Assert.Null(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}");
      }).Placement.Area);
      Assert.Null(VerticalRepeat(IntCell()).Placement.Area);
      Assert.NotNull(IntCell().Placement.Area);
    }
  }
}
