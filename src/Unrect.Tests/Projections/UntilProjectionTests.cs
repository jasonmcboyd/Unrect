using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>.On</c> and <c>.Below</c> say where a projection starts by content; <c>.Until</c> says
  /// where it ends by one. The bound is exclusive and consumed in full, so the projection that
  /// follows begins <em>at</em> the landmark and can anchor on it — which is the whole reason this
  /// is a bound on the projection rather than a "stop before" option on the repeat.
  /// </summary>
  public class UntilProjectionTests
  {
    // A, B, Total, C, End — two rows, a caption, two more.
    private static ISheetCells Sections() => Mixed(new object?[,] { { "A" }, { "B" }, { "Total" }, { "C" }, { "End" } });

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<string>> Lines() => VerticalRepeat(TextCell());

    // --- The bound ------------------------------------------------------------------------------

    [Fact]
    public void TheBoundedProjectionStopsBeforeTheLandmark()
    {
      // The landmark row is never inside what the projection may read.
      var applied = Until(RowContaining("Total")).Of(Lines()).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(2, applied.Consumed.Height);
      Assert.Equal(1, applied.Consumed.Width);
    }

    [Fact]
    public void TheFollowingSiblingStartsAtTheLandmark()
    {
      // Consumed is the bound, not what the inner projection read, so the next child's own seek
      // finds the caption at distance zero. This is what Until is for.
      var section = Until(RowContaining("Total")).Of(Lines());
      var caption = On(RowContaining("Total")).Of(TextCell());

      var read = VerticalFlow(v =>
      {
        var section2 = v.Next(section);
        var caption2 = v.Next(caption);

        return v.Build(read2 => $"[{string.Join(",", read2.Of(section2))}]+{read2.Of(caption2)}");
      }).Map(Sections());

      Assert.Equal("[A,B]+Total", read);
    }

    [Fact]
    public void ALandmarkOnTheVeryFirstRowLeavesNothingToRead()
    {
      // Not an error in itself: a repeat yields an empty list, and a Cell fails because a 1x1
      // extent does not fit in a zero-row one. Both are correct.
      var space = Mixed(new object?[,] { { "Total" }, { "A" } });

      var applied = Until(RowContaining("Total")).Of(Lines()).Apply(space);

      Assert.Empty(applied.Value);
      Assert.Equal(0, applied.Consumed.Height);

      Assert.Throws<ProjectionException>(() => Until(RowContaining("Total")).Of(TextCell()).Map(space));
    }

    [Fact]
    public void ARepeatBoundedByALandmarkStopsBeforeTrailingContent()
    {
      // The open question this closes. A blank band is a separator and never a terminator, so an
      // unbounded repeat swallows the caption and everything after it; the bound is the terminator
      // that semantics deliberately withholds.
      var space = Mixed(new object?[,] { { "A" }, { "B" }, { null }, { "Total" }, { "C" } });

      Assert.Equal(
        new[] { "A", "B", "Total", "C" },
        VerticalRepeat(TextCell(), separatedBy: BlankRows()).Map(space));

      Assert.Equal(
        new[] { "A", "B" },
        Until(RowContaining("Total")).Of(VerticalRepeat(TextCell(), separatedBy: BlankRows())).Map(space));
    }

    // --- A missing landmark ----------------------------------------------------------------------------

    [Fact]
    public void AMissingLandmarkBlamesTheProjectionItWasBounding()
    {
      // "Until" is not what the user was looking for; the bound is part of the bounded projection's
      // declaration, so the bounded projection owns the failure.
      var failure = Assert.Throws<ProjectionException>(() =>
        Until(RowContaining("Nope")).Of(Lines().Named("items")).Map(Sections()));

      Assert.Equal("'items'", failure.Subject);
      Assert.Equal("'items' (VerticalRepeat)", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
      Assert.Contains("no row containing 'Nope' exists to end this projection", failure.Message);
    }

    [Fact]
    public void AnUnnamedBoundIsTransparentAndBlamesTheInnerProjection()
    {
      var failure = Assert.Throws<ProjectionException>(() => Until(RowContaining("Nope")).Of(Lines()).Map(Sections()));

      Assert.Equal("VerticalRepeat", failure.Subject);
      Assert.Equal("VerticalRepeat", failure.Path);
      Assert.DoesNotContain("Until", failure.Path);
    }

    [Fact]
    public void ANamedBoundSpeaksForItself()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Until(RowContaining("Nope")).Of(Lines()).Named("section").Map(Sections()));

      Assert.Equal("'section'", failure.Subject);
      Assert.Equal("'section' (Until)", failure.Path);
    }

    [Fact]
    public void AMissingLandmarkIsAbsorbable()
    {
      // A landmark that is not there is a disagreement about the shape of the data, which is
      // precisely what a tolerance boundary is for — not a bug in the reading code.
      Assert.Null(Until(RowContaining("Nope")).Of(Lines()).Optional().Map(Sections()));
    }

    [Fact]
    public void AMissingLandmarkInsideARepeatItemIsLoudRatherThanAStop()
    {
      // A missing start is exhaustion; a missing end is drift. The item was found, so the failure
      // is deeper than the item's own placement.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(Until(RowContaining("Nope")).Of(TextCell())).Map(Sections()));

      Assert.Contains("VerticalRepeat[0]", failure.Path);
      Assert.Contains("no row containing 'Nope' exists to end this projection", failure.Message);
    }

    // --- orEnd ------------------------------------------------------------------------------------------

    [Fact]
    public void OrEnd_RunsToTheEndOfTheSpaceAndSaysSo()
    {
      var projection = Until(RowContaining("Nope"), orEnd: true).Of(Lines());
      var result = projection.MapWithDiagnostics(Sections());

      Assert.Equal(new[] { "A", "B", "Total", "C", "End" }, result.Value);

      var info = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("VerticalRepeat", info.Subject);
      Assert.Equal(
        "no row containing 'Nope' exists to end this projection, so it ran to the end of the space",
        info.Message);
      Assert.Equal("VerticalRepeat", info.Path);
      Assert.Equal("A1", info.Location.A1);
    }

    [Fact]
    public void OrEnd_IsSilentWhenTheLandmarkIsThere()
    {
      // Only the ordinary unconsumed-space Info remains, which is about the rows after the bound
      // rather than about the bound itself.
      var result = Until(RowContaining("Total"), orEnd: true).Of(Lines()).MapWithDiagnostics(Sections());

      Assert.Equal(new[] { "A", "B" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("exists to end this projection"));
    }

    [Fact]
    public void OrEnd_IsDeclaredAlternationRatherThanTolerance()
    {
      // Info, not Warning: nothing failed and nothing was absorbed. The declaration said this was
      // allowed to happen.
      var result = Until(RowContaining("Nope"), orEnd: true).Of(Lines()).MapWithDiagnostics(Sections());

      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    // --- Composition -------------------------------------------------------------------------------------
    //
    // The offset and the extent families each have a DEFAULT path the refusal leaves alone — a shape
    // states an offset or an extent in its own constructor, and a modifier replaces it silently. The
    // bound has no such path to guard: nothing builds an BoundedDefinition but .Until/.UntilColumn, so
    // a projection has no default end and the FIRST bound is always the declaration. That asymmetry
    // is why these pins are all refusals with no silent-replacement twin.

    [Fact]
    public void ALaterBoundIsRefusedRatherThanReplacingAnEarlierOne()
    {
      // Was ALaterBoundReplacesAnEarlierOneRatherThanNesting, and what it pinned is why this
      // flipped: the pair read as the LAST bound alone — "A","B", two rows consumed — with the first
      // landmark dropped and never sought. A projection has one end, so a second one is refused
      // where it is written. Nesting is how two ends are said, and
      // AWrapperBetweenTwoBoundsIsTheDifferenceBetweenARefusalAndANesting owns that half. The chained
      // spelling is now a compile-time refusal (BoundStage.Until, MustNotCompilePipeline.cs AD); the
      // runtime guard survives on the nested spelling, where the outer bound meets an already-bounded
      // projection.
      var failure = Assert.Throws<ArgumentException>(() =>
        Until(RowContaining("Total")).Of(Until(RowContaining("End")).Of(Lines())));

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("already ends at a landmark, and Until would replace that end", failure.Message);
      Assert.Contains("Bound it once", failure.Message);
    }

    [Fact]
    public void ANameBetweenTwoBoundsDoesNotSeparateThem()
    {
      // Was ReplacingABoundKeepsTheNameAlreadyOnIt: the replacement cloned the wrapper, so the name
      // written on the discarded bound outlived it. A clone is not a layer, so the second bound
      // still meets the bound wrapper itself and is refused — and the refusal borrows the user's own
      // word for the projection, which is the whole of what a name is worth at construction time.
      var failure = Assert.Throws<ArgumentException>(() =>
        Until(RowContaining("Total")).Of(Until(RowContaining("End")).Of(Lines()).Named("keep")));

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("'keep' already ends at a landmark", failure.Message);
      Assert.Contains("Bound it once", failure.Message);
    }

    [Fact]
    public void SizedAfterUntil_IsWhatTheParentSees()
    {
      // The extent written OUTSIDE the bound is what the parent consumes: the wrapper has a declared
      // area, so the engine consumes it in full and the landmark search happens inside it. Spelled by
      // nesting, because a size applied after the bound lands on the bound wrapper.
      var applied = Sized(AreaStrategies.ExplicitArea(1, 4)).Of(Until(RowContaining("Total")).Of(Lines())).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(4, applied.Consumed.Height);
    }

    [Fact]
    public void UntilAfterSized_BoundsTheDeclaredExtent()
    {
      var applied = Sized(AreaStrategies.ExplicitArea(1, 2)).Until(RowContaining("Total")).Of(Lines()).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void ADeclaredExtentLargerThanTheBoundIsAContradictionAndFails()
    {
      // Until is outermost, so the inner projection is handed the two rows before the landmark and
      // its own four-row extent no longer fits. Two halves of one declaration disagreeing is an
      // error.
      var failure = Assert.Throws<ProjectionException>(() =>
        Sized(AreaStrategies.ExplicitArea(1, 4)).Until(RowContaining("Total")).Of(Lines()).Map(Sections()));

      Assert.Contains("an extent of 1x4 does not fit here", failure.Message);
    }

    [Fact]
    public void AnOffsetThenUntil_AnchorsInsideWhatTheLandmarkLeft()
    {
      // The reading order — start here, stop there. Spelled with OffsetBy because what this pins is
      // that a bound composes with a placement of any kind; an anchor would read better in a
      // declaration, and AnchorModifierTests owns that half.
      var space = Mixed(new object?[,] { { "skip" }, { "A" }, { "B" }, { "Total" } });

      var section = OffsetBy(SkipRows(1)).Until(RowContaining("Total")).Of(Lines());

      Assert.Equal(new[] { "A", "B" }, section.Map(space));
    }

    // --- The column twin ------------------------------------------------------------------------------------

    [Fact]
    public void UntilColumn_BoundsAcrossInsteadOfDown()
    {
      var space = Mixed(new object?[,] { { "a", "b", "Total", "d" } });

      var cells = UntilColumn(ColumnContaining("Total")).Of(HorizontalRepeat(TextCell()));
      var applied = HorizontalFlow(h =>
      {
        var cells2 = h.Next(cells);

        return h.Build(read => string.Join(",", read.Of(cells2)));
      }).Apply(space);

      Assert.Equal("a,b", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
    }

    // --- Switching the axis ---------------------------------------------------------------------------------
    //
    // A projection has one end, and the axis a bound cuts comes with its landmark — so a bound of the
    // other kind is still a SECOND end, and is refused like any other. Until 2026-09-09 the switch
    // replaced instead (the row bound below left two rows, the column bound that replaced it left two
    // columns and all three rows, and the discarded landmark was never sought); that was later
    // flipped to a refusal. Both axes at once is
    // spelled by nesting, which the last test in this section pins.

    // 3 columns by 3 rows: a b Total / c d e / Stop f g.
    private static ISheetCells BothAxes() => Mixed(new object?[,]
    {
      { "a", "b", "Total" },
      { "c", "d", "e" },
      { "Stop", "f", "g" },
    });

    private static IProjectionDefinition<ISheetCells, string> BlockExtent() => Range(b => $"{b.Width}x{b.Height}");

    [Fact]
    public void AColumnBoundOverARowBoundIsRefused()
    {
      // Was AColumnBoundReplacesARowBound, which read "2x3": the column bound won outright and the
      // row landmark was dropped unsought.
      var failure = Assert.Throws<ArgumentException>(() =>
        UntilColumn(ColumnContaining("Total")).Of(Until(RowContaining("Stop")).Of(BlockExtent())));

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("already ends at a landmark, and UntilColumn would replace that end", failure.Message);
      Assert.Contains("Bound it once", failure.Message);
    }

    [Fact]
    public void ARowBoundOverAColumnBoundIsRefused()
    {
      // Was ARowBoundReplacesAColumnBound, which read "3x2". The mirror, and the reason the refusal
      // is not about the axis: what a second bound would replace is the projection's one END,
      // whichever direction it happens to cut.
      var failure = Assert.Throws<ArgumentException>(() =>
        Until(RowContaining("Stop")).Of(UntilColumn(ColumnContaining("Total")).Of(BlockExtent())));

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("already ends at a landmark, and Until would replace that end", failure.Message);
    }

    [Fact]
    public void ASecondBoundIsRefusedBeforeEitherLandmarkIsSoughtFor()
    {
      // Was AMissAfterASwitchNamesTheLandmarkThatIsActuallyInForce, which pinned that a miss after a
      // switch blamed the landmark still in force ("no column containing 'Nope' …") rather than the
      // discarded one — the reader must not be sent looking for something the declaration stopped
      // asking about. With the switch refused there is no "in force" to choose between: the refusal
      // happens at construction, before any space exists to search, so it names the modifier and the
      // receiver and neither landmark's text.
      var failure = Assert.Throws<ArgumentException>(() =>
        UntilColumn(ColumnContaining("Nope")).Of(Until(RowContaining("Stop")).Of(BlockExtent())));

      Assert.Contains("already ends at a landmark, and UntilColumn would replace that end", failure.Message);
      Assert.DoesNotContain("Stop", failure.Message);
      Assert.DoesNotContain("Nope", failure.Message);
    }

    [Fact]
    public void ANameDoesNotSeparateTheTwoBoundsAcrossAnAxisEither()
    {
      // Was ANameSurvivesASwitchOfAxis: the replacement cloned the wrapper, so 'band' outlived the
      // bound it was written on and a later miss reported "'band' (UntilColumn)". What the same
      // declaration shows now is sharper — naming a wrapper makes it OPAQUE to the path renderer but
      // does not make it a LAYER, so the second bound still meets the first and is refused, in the
      // user's own word for the projection.
      var failure = Assert.Throws<ArgumentException>(() =>
        UntilColumn(ColumnContaining("Nope")).Of(Until(RowContaining("Stop")).Of(BlockExtent()).Named("band")));

      Assert.Equal("projection", failure.ParamName);
      Assert.Contains("'band' already ends at a landmark, and UntilColumn would replace that end", failure.Message);
    }

    [Fact]
    public void BoundingBothAxesIsSpelledByNesting()
    {
      // The positive pin the refusals above point at, and the thing a switch of axis was reaching
      // for: a wrapper between the two bounds makes the second one bound the FIRST, so both are in
      // force. The column bound leaves columns 0-1 of the whole sheet; the row bound inside it finds
      // "Stop" in what is left and stops before it.
      var projection = UntilColumn(ColumnContaining("Total")).Of(Until(RowContaining("Stop")).Of(BlockExtent()).Select(value => value));

      var applied = projection.Apply(BothAxes());

      Assert.Equal("2x2", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);

      // The outermost bound is the one the projection describes itself by; the inner one is a child.
      Assert.Equal("UntilColumn", projection.Description);
    }

    // --- A landmark the anchor cannot be reached past -----------------------------------------------------------

    [Fact]
    public void ALandmarkBeforeTheAnchorMakesTheAnchorUnreachableAndFailsLoudly()
    {
      // The seek is outside the bound, so it searches only what the landmark left. A section whose
      // start lies beyond its own end is not there, and saying so beats reading half of it.
      var space = Mixed(new object?[,] { { "Total" }, { "Start" }, { "a" }, { "b" } });

      var failure = Assert.Throws<ProjectionException>(() =>
        On(RowContaining("Start")).Until(RowContaining("Total")).Of(Lines()).Map(space));

      Assert.Contains("no row containing 'Start' exists in the available space", failure.Message);
    }

    // --- The headline composition, on a grid ------------------------------------------------------------------------

    [Fact]
    public void OneRepeatPlacedTwice_TheFirstBoundedByTheSecondsCaption()
    {
      // The shape of examples/investor-irr.xlsx without the workbook: two series of blocks, the
      // first ending exactly where the second's caption begins. One Repeat, declared once and
      // placed twice — the first bounded by the caption the second anchors on, which is why the
      // second finds it at distance zero.
      var space = Mixed(new object?[,]
      {
        { "By transfer date" },
        { "A" },
        { "B" },
        { null },
        { "C" },
        { "By inception date" },
        { "D" },
        { null },
        { "E" },
        { "F" },
      });

      const string Inception = "By inception date";

      var series = VerticalRepeat(TextCell(), separatedBy: BlankRows());

      var report = VerticalFlow(v =>
      {
        var below = v.Next(Below(RowContaining("By transfer date")).Until(RowContaining(Inception)).Of(series));
        var below2 = v.Next(Below(RowContaining(Inception)).Of(series));

        return v.Build(read => new
        {
          ByTransferDate = read.Of(below),
          ByInception = read.Of(below2),
        });
      });

      var result = report.MapWithDiagnostics(space);

      Assert.Equal(new[] { "A", "B", "C" }, result.Value.ByTransferDate);
      Assert.Equal(new[] { "D", "E", "F" }, result.Value.ByInception);
      Assert.Empty(result.Diagnostics);
    }

    // --- Inspection and guards ---------------------------------------------------------------------------------

    [Fact]
    public void ABoundDescribesItselfAndIsAWrapper()
    {
      var bound = Until(RowContaining("Total")).Of(Lines());

      Assert.Equal("Until", bound.Description);
      Assert.True(bound.IsWrapper);
      Assert.True(bound.Named("section").IsWrapper);
      Assert.Equal("UntilColumn", UntilColumn(ColumnContaining("Total")).Of(Lines()).Description);
    }

    [Fact]
    public void ABoundExposesTheProjectionItWraps()
    {
      var inner = Lines();

      Assert.Same(inner, Assert.Single(Until(RowContaining("Total")).Of(inner).Children).Definition);
    }

    [Fact]
    public void ABoundRejectsANullLandmark()
    {
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Until((IRowLandmark)null!).Of(Lines())).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => UntilColumn((IColumnLandmark)null!).Of(Lines())).ParamName);
    }
  }
}
