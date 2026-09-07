using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
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
    private static ISpace Sections() => Mixed(new object?[,] { { "A" }, { "B" }, { "Total" }, { "C" }, { "End" } });

    private static IProjection<IReadOnlyList<string>> Lines() => VerticalRepeat(Cell(c => c.GetString()));

    // --- The bound ------------------------------------------------------------------------------

    [Fact]
    public void TheBoundedProjectionStopsBeforeTheLandmark()
    {
      // The landmark row is never inside what the projection may read.
      var applied = Lines().Until(RowContaining("Total")).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(2, applied.Consumed.Height);
      Assert.Equal(1, applied.Consumed.Width);
    }

    [Fact]
    public void TheFollowingSiblingStartsAtTheLandmark()
    {
      // Consumed is the bound, not what the inner projection read, so the next child's own seek
      // finds the caption at distance zero. This is what Until is for.
      var section = Lines().Until(RowContaining("Total"));
      var caption = Cell(c => c.GetString()).On(RowContaining("Total"));

      var read = VerticalFlow(v => $"[{string.Join(",", v.Next(section))}]+{v.Next(caption)}").Map(Sections());

      Assert.Equal("[A,B]+Total", read);
    }

    [Fact]
    public void ALandmarkOnTheVeryFirstRowLeavesNothingToRead()
    {
      // Not an error in itself: a repeat yields an empty list, and a Cell fails because a 1x1
      // extent does not fit in a zero-row one. Both are correct.
      var space = Mixed(new object?[,] { { "Total" }, { "A" } });

      var applied = Lines().Until(RowContaining("Total")).Apply(space);

      Assert.Empty(applied.Value);
      Assert.Equal(0, applied.Consumed.Height);

      Assert.Throws<ProjectionException>(() => Cell(c => c.GetString()).Until(RowContaining("Total")).Map(space));
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
        VerticalRepeat(Cell(c => c.GetString()), separatedBy: BlankRows()).Map(space));

      Assert.Equal(
        new[] { "A", "B" },
        VerticalRepeat(Cell(c => c.GetString()), separatedBy: BlankRows()).Until(RowContaining("Total")).Map(space));
    }

    // --- A missing landmark ----------------------------------------------------------------------------

    [Fact]
    public void AMissingLandmarkBlamesTheProjectionItWasBounding()
    {
      // "Until" is not what the user was looking for; the bound is part of the bounded projection's
      // declaration, so the bounded projection owns the failure.
      var failure = Assert.Throws<ProjectionException>(() =>
        Lines().Named("items").Until(RowContaining("Nope")).Map(Sections()));

      Assert.Equal("'items'", failure.Subject);
      Assert.Equal("'items' (VerticalRepeat)", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
      Assert.Contains("no row containing 'Nope' exists to end this projection", failure.Message);
    }

    [Fact]
    public void AnUnnamedBoundIsTransparentAndBlamesTheInnerProjection()
    {
      var failure = Assert.Throws<ProjectionException>(() => Lines().Until(RowContaining("Nope")).Map(Sections()));

      Assert.Equal("VerticalRepeat", failure.Subject);
      Assert.Equal("VerticalRepeat", failure.Path);
      Assert.DoesNotContain("Until", failure.Path);
    }

    [Fact]
    public void ANamedBoundSpeaksForItself()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Lines().Until(RowContaining("Nope")).Named("section").Map(Sections()));

      Assert.Equal("'section'", failure.Subject);
      Assert.Equal("'section' (Until)", failure.Path);
    }

    [Fact]
    public void AMissingLandmarkIsAbsorbable()
    {
      // A landmark that is not there is a disagreement about the shape of the data, which is
      // precisely what a tolerance boundary is for — not a bug in the reading code.
      Assert.Null(Lines().Until(RowContaining("Nope")).Optional().Map(Sections()));
    }

    [Fact]
    public void AMissingLandmarkInsideARepeatItemIsLoudRatherThanAStop()
    {
      // A missing start is exhaustion; a missing end is drift. The item was found, so the failure
      // is deeper than the item's own placement.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(Cell(c => c.GetString()).Until(RowContaining("Nope"))).Map(Sections()));

      Assert.Contains("VerticalRepeat[0]", failure.Path);
      Assert.Contains("no row containing 'Nope' exists to end this projection", failure.Message);
    }

    // --- orEnd ------------------------------------------------------------------------------------------

    [Fact]
    public void OrEnd_RunsToTheEndOfTheSpaceAndSaysSo()
    {
      var projection = Lines().Until(RowContaining("Nope"), orEnd: true);
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
      var result = Lines().Until(RowContaining("Total"), orEnd: true).MapWithDiagnostics(Sections());

      Assert.Equal(new[] { "A", "B" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("exists to end this projection"));
    }

    [Fact]
    public void OrEnd_IsDeclaredAlternationRatherThanTolerance()
    {
      // Info, not Warning: nothing failed and nothing was absorbed. The declaration said this was
      // allowed to happen.
      var result = Lines().Until(RowContaining("Nope"), orEnd: true).MapWithDiagnostics(Sections());

      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    // --- Composition -------------------------------------------------------------------------------------

    [Fact]
    public void ALaterBoundReplacesAnEarlierOneRatherThanNesting()
    {
      // A projection has one end, so Until follows the same "later replaces earlier" rule as Sized.
      var applied = Lines().Until(RowContaining("End")).Until(RowContaining("Total")).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void ReplacingABoundKeepsTheNameAlreadyOnIt()
    {
      var projection = Lines().Until(RowContaining("End")).Named("keep").Until(RowContaining("Total"));

      Assert.Equal("keep", projection.Name);
      Assert.Equal(new[] { "A", "B" }, projection.Map(Sections()));
    }

    [Fact]
    public void SizedAfterUntil_IsWhatTheParentSees()
    {
      // The modifier written last is what the parent consumes: the wrapper has a declared area, so
      // the engine consumes it in full and the landmark search happens inside it.
      var applied = Lines().Until(RowContaining("Total")).Sized(AreaStrategies.ExplicitArea(1, 4)).Apply(Sections());

      Assert.Equal(new[] { "A", "B" }, applied.Value);
      Assert.Equal(4, applied.Consumed.Height);
    }

    [Fact]
    public void UntilAfterSized_BoundsTheDeclaredExtent()
    {
      var applied = Lines().Sized(AreaStrategies.ExplicitArea(1, 2)).Until(RowContaining("Total")).Apply(Sections());

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
        Lines().Sized(AreaStrategies.ExplicitArea(1, 4)).Until(RowContaining("Total")).Map(Sections()));

      Assert.Contains("an extent of 1x4 does not fit here", failure.Message);
    }

    [Fact]
    public void AnOffsetThenUntil_AnchorsInsideWhatTheLandmarkLeft()
    {
      // The reading order — start here, stop there. Spelled with OffsetBy because what this pins is
      // that a bound composes with a placement of any kind; an anchor would read better in a
      // declaration, and AnchorModifierTests owns that half.
      var space = Mixed(new object?[,] { { "skip" }, { "A" }, { "B" }, { "Total" } });

      var section = Lines().OffsetBy(SkipRows(1)).Until(RowContaining("Total"));

      Assert.Equal(new[] { "A", "B" }, section.Map(space));
    }

    // --- The column twin ------------------------------------------------------------------------------------

    [Fact]
    public void UntilColumn_BoundsAcrossInsteadOfDown()
    {
      var space = Mixed(new object?[,] { { "a", "b", "Total", "d" } });

      var cells = HorizontalRepeat(Cell(c => c.GetString())).UntilColumn(ColumnContaining("Total"));
      var applied = HorizontalFlow(h => string.Join(",", h.Next(cells))).Apply(space);

      Assert.Equal("a,b", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
    }

    // --- Switching the axis ---------------------------------------------------------------------------------
    //
    // A projection has one end, so a second bound replaces the first even when it changes which axis the
    // bound is on. The wrapper carries the orientation, and everything that reads it follows.

    // 3 columns by 3 rows: a b Total / c d e / Stop f g.
    private static ISpace BothAxes() => Mixed(new object?[,]
    {
      { "a", "b", "Total" },
      { "c", "d", "e" },
      { "Stop", "f", "g" },
    });

    private static IProjection<string> BlockExtent() => Range(b => $"{b.Width}x{b.Height}");

    [Fact]
    public void AColumnBoundReplacesARowBound()
    {
      // The row bound would have left two rows; the column bound that replaced it leaves two
      // columns and all three rows.
      var projection = BlockExtent().Until(RowContaining("Stop")).UntilColumn(ColumnContaining("Total"));

      var applied = projection.Apply(BothAxes());

      Assert.Equal("2x3", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
      Assert.Equal("UntilColumn", projection.Description);
    }

    [Fact]
    public void ARowBoundReplacesAColumnBound()
    {
      var projection = BlockExtent().UntilColumn(ColumnContaining("Total")).Until(RowContaining("Stop"));

      var applied = projection.Apply(BothAxes());

      Assert.Equal("3x2", applied.Value);
      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
      Assert.Equal("Until", projection.Description);
    }

    [Fact]
    public void AMissAfterASwitchNamesTheLandmarkThatIsActuallyInForce()
    {
      // Blaming the discarded row landmark would send the reader looking down a column of rows for
      // something the declaration stopped asking about.
      var failure = Assert.Throws<ProjectionException>(() =>
        BlockExtent().Until(RowContaining("Stop")).UntilColumn(ColumnContaining("Nope")).Map(BothAxes()));

      Assert.Contains("no column containing 'Nope' exists to end this projection", failure.Message);
      Assert.DoesNotContain("Stop", failure.Message);
    }

    [Fact]
    public void ANameSurvivesASwitchOfAxis()
    {
      // The replacement clones the wrapper, so what the user called it outlives what it bounds by.
      var projection = BlockExtent().Until(RowContaining("Stop")).Named("band").UntilColumn(ColumnContaining("Nope"));

      Assert.Equal("band", projection.Name);
      Assert.Equal("UntilColumn", projection.Description);

      var failure = Assert.Throws<ProjectionException>(() => projection.Map(BothAxes()));

      Assert.Equal("'band'", failure.Subject);
      Assert.Equal("'band' (UntilColumn)", failure.Path);
    }

    // --- A landmark the anchor cannot be reached past -----------------------------------------------------------

    [Fact]
    public void ALandmarkBeforeTheAnchorMakesTheAnchorUnreachableAndFailsLoudly()
    {
      // The seek is outside the bound, so it searches only what the landmark left. A section whose
      // start lies beyond its own end is not there, and saying so beats reading half of it.
      var space = Mixed(new object?[,] { { "Total" }, { "Start" }, { "a" }, { "b" } });

      var failure = Assert.Throws<ProjectionException>(() =>
        Lines().On(RowContaining("Start")).Until(RowContaining("Total")).Map(space));

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

      var series = VerticalRepeat(Cell(c => c.GetString()), separatedBy: BlankRows());

      var report = VerticalFlow(v => new
      {
        ByTransferDate = v.Next(series
          .Below(RowContaining("By transfer date"))
          .Until(RowContaining(Inception))),
        ByInception = v.Next(series
          .Below(RowContaining(Inception))),
      });

      var result = report.MapWithDiagnostics(space);

      Assert.Equal(new[] { "A", "B", "C" }, result.Value.ByTransferDate);
      Assert.Equal(new[] { "D", "E", "F" }, result.Value.ByInception);
      Assert.Empty(result.Diagnostics);
    }

    // --- Inspection and guards ---------------------------------------------------------------------------------

    [Fact]
    public void ABoundDescribesItselfAndIsTransparentUntilNamed()
    {
      var bound = Lines().Until(RowContaining("Total"));

      Assert.Equal("Until", bound.Description);
      Assert.True(bound.IsTransparent);
      Assert.False(bound.Named("section").IsTransparent);
      Assert.Equal("UntilColumn", Lines().UntilColumn(ColumnContaining("Total")).Description);
    }

    [Fact]
    public void ABoundExposesTheProjectionItWraps()
    {
      var inner = Lines();

      Assert.Same(inner, Assert.Single(inner.Until(RowContaining("Total")).Children));
    }

    [Fact]
    public void ABoundRejectsANullLandmark()
    {
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Lines().Until(null!)).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Lines().UntilColumn(null!)).ParamName);
    }
  }
}
