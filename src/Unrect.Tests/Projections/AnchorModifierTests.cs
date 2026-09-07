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
  /// The three anchors — <c>.On</c>, <c>.Below</c>, <c>.RightOf</c> — which say where a projection
  /// starts by naming something in the grid rather than a distance to it. They are one-liners over
  /// <c>OffsetStrategies.To</c>/<c>Past</c>, so what is worth pinning is not new machinery but the
  /// four claims the words make: which cell you land on, that the step past a match is exactly one,
  /// that an anchor <em>replaces</em> the offset while a movement composes onto it, and that a
  /// landmark that matches nothing is loud in the same words whichever spelling reached it.
  /// <para>
  /// The axis is carried by the argument's type and enforced by overload resolution:
  /// <c>.Below</c> takes an <c>IRowLandmark</c> and <c>.RightOf</c> an <c>IColumnLandmark</c>, so
  /// <c>.Below(ColumnContaining("x"))</c> does not compile. There is no runtime behaviour to pin
  /// there — a test could only assert that something the compiler already refused stays refused.
  /// </para>
  /// </summary>
  public class AnchorModifierTests
  {
    // A junk row, the landmark row, two rows under it.
    private static ISpace Rows() => Mixed(new object?[,] { { "junk" }, { "Detail" }, { "a" }, { "b" } });

    // The same four cells turned on their side, so the column twins read identically.
    private static ISpace Columns() => Mixed(new object?[,] { { "junk", "Detail", "a", "b" } });

    private static IRowLandmark Detail() => RowContaining("Detail");

    private static IColumnLandmark DetailColumn() => ColumnContaining("Detail");

    // --- On: the projection lands on the match and owns it -------------------------------------------------

    [Fact]
    public void On_LandsOnTheMatchedRowAndOwnsIt()
    {
      // Owning it is the whole difference from Below: the matched row is inside the extent, which
      // is how a caption becomes content the projection reads rather than a gap it steps over.
      var applied = Text().On(Detail()).Apply(Rows());

      Assert.Equal("Detail", applied.Value);
      Assert.Equal(1, applied.Offset.Size.Height);
      Assert.Equal(0, applied.Offset.Size.Width);

      Assert.Equal(3, Range(b => b.Height).On(Detail()).Map(Rows()));
    }

    [Fact]
    public void On_LandsOnTheMatchedColumnAndOwnsIt()
    {
      // One word, both axes: occupancy has no direction, so the argument's type is what says which.
      var applied = Text().On(DetailColumn()).Apply(Columns());

      Assert.Equal("Detail", applied.Value);
      Assert.Equal(1, applied.Offset.Size.Width);
      Assert.Equal(0, applied.Offset.Size.Height);

      Assert.Equal(3, Range(b => b.Width).On(DetailColumn()).Map(Columns()));
    }

    // --- Below and RightOf: exactly one beyond ---------------------------------------------------------

    [Fact]
    public void Below_StartsExactlyOneRowBelowTheMatch()
    {
      // The doc pins what the English leaves loose. "Below" could mean anywhere under the match; it
      // means the very next row — the matched row's own height, never a step the declaration chose.
      var applied = Text().Below(Detail()).Apply(Rows());

      Assert.Equal("a", applied.Value);
      Assert.Equal(2, applied.Offset.Size.Height);
      Assert.Equal(0, applied.Offset.Size.Width);

      // Said the other way, so a regression in either operator shows up here: one more than On.
      Assert.Equal(
        Text().On(Detail()).Apply(Rows()).Offset.Size.Height + 1,
        applied.Offset.Size.Height);

      // ...and the matched row is outside the extent, where On had it inside.
      Assert.Equal(2, Range(b => b.Height).Below(Detail()).Map(Rows()));
    }

    [Fact]
    public void RightOf_StartsExactlyOneColumnRightOfTheMatch()
    {
      var applied = Text().RightOf(DetailColumn()).Apply(Columns());

      Assert.Equal("a", applied.Value);
      Assert.Equal(2, applied.Offset.Size.Width);
      Assert.Equal(0, applied.Offset.Size.Height);

      Assert.Equal(
        Text().On(DetailColumn()).Apply(Columns()).Offset.Size.Width + 1,
        applied.Offset.Size.Width);

      Assert.Equal(2, Range(b => b.Width).RightOf(DetailColumn()).Map(Columns()));
    }

    // --- The direction is the sheet's, not the flow's ---------------------------------------------------
    //
    // Law 2: grid-absolute over flow-relative. "Below" means down the sheet wherever it is written,
    // so a declaration means the same thing when the flow around it is rewritten.

    [Fact]
    public void Below_MeansDownTheSheetEvenInsideAHorizontalFlow()
    {
      // The second child's band is the single column to the right, one cell of which is the
      // landmark. Read as "the next band along this flow", there would be nowhere to go and this
      // would fail.
      var space = Mixed(new object?[,]
      {
        { "a", "Detail" },
        { "b", "c" },
      });

      var below = Text().Below(Detail());

      Assert.Equal("a|c", HorizontalFlow(h => $"{h.Next(Text())}|{h.Next(below)}").Map(space));
    }

    [Fact]
    public void RightOf_MeansAcrossTheSheetEvenInsideAVerticalFlow()
    {
      var space = Mixed(new object?[,]
      {
        { "a", "x", "y" },
        { "Detail", "b", "c" },
      });

      var right = Text().RightOf(DetailColumn());

      Assert.Equal("a|b", VerticalFlow(v => $"{v.Next(Text())}|{v.Next(right)}").Map(space));
    }

    // --- Replace, like all placement --------------------------------------------------------------------

    [Fact]
    public void AnAnchorReplacesWhateverOffsetTheProjectionAlreadyHad()
    {
      // A position stated as a relation to a thing owes nothing to wherever the cursor had got to.
      // Were Down(2) composed with the anchor, the search would start below the landmark and fail.
      Assert.Equal("a", Text().Down(2).Below(Detail()).Map(Rows()));
      Assert.Equal("Detail", Text().Down(2).On(Detail()).Map(Rows()));

      // ...and a movement written after one carries on from the anchor, as movements always do.
      Assert.Equal("b", Text().Below(Detail()).Down(1).Map(Rows()));
      Assert.Equal("a", Text().On(Detail()).Down(1).Map(Rows()));
    }

    [Fact]
    public void ALaterPlacementReplacesAnAnchor()
    {
      // The other direction of the same rule, and the one that says an anchor is not privileged:
      // .OffsetBy replaces it exactly as it replaces anything else, so the last one written wins.
      Assert.Equal("junk", Text().Below(Detail()).OffsetBy(SkipRows(0)).Map(Rows()));
      Assert.Equal("b", Text().On(Detail()).OffsetBy(SkipRows(3)).Map(Rows()));
    }

    [Fact]
    public void AnAnchorDiscardsAProjectionsDefaultOffset()
    {
      // Replacing is also how a default is thrown away: a Table skips the blank rows in front of it
      // unless something says otherwise, and an anchor is one of the things that can say otherwise.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { "Investor", "Amount" },
        { "Acme", "10" },
      });

      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).Map(space));
      Assert.Equal(new[] { "Acme", "10" }, Table(t => t.ColumnNames).On(RowContaining("Acme")).Map(space));
    }

    [Fact]
    public void ADeclaredAreaSurvivesAnAnchor()
    {
      // An anchor replaces the OFFSET and nothing else. Placement is two independent halves, and
      // .Sized is the other half's own replace — so an extent the projection declared is still its
      // extent wherever the landmark puts it.
      var tall = Range(b => (b.Width, b.Height)).Sized(Extent(1, 2));

      Assert.Equal((1, 2), tall.On(Detail()).Map(Rows()));
      Assert.Equal((1, 2), tall.Below(Detail()).Map(Rows()));

      var wide = Range(b => (b.Width, b.Height)).Sized(Extent(2, 1));

      Assert.Equal((2, 1), wide.On(DetailColumn()).Map(Columns()));
      Assert.Equal((2, 1), wide.RightOf(DetailColumn()).Map(Columns()));
    }

    // --- A landmark that matches nothing ------------------------------------------------------------------

    [Fact]
    public void AMissingLandmarkIsLoudThroughEveryAnchor()
    {
      // Loud because it means the section the declaration describes is not the section in the file.
      Assert.Contains(
        "no row containing 'Nope' exists in the available space",
        Miss(Text().On(RowContaining("Nope")), Rows()));

      Assert.Contains(
        "no row containing 'Nope' exists in the available space",
        Miss(Text().Below(RowContaining("Nope")), Rows()));

      Assert.Contains(
        "no column containing 'Nope' exists in the available space",
        Miss(Text().On(ColumnContaining("Nope")), Columns()));

      Assert.Contains(
        "no column containing 'Nope' exists in the available space",
        Miss(Text().RightOf(ColumnContaining("Nope")), Columns()));
    }

    [Fact]
    public void AMissingLandmarkSaysWhatTheStrategySpellingSaid()
    {
      // Message identity, not merely a shared phrase: the renovation moved the spelling and must
      // not have moved the diagnostic. A matcher only locates and reports absence, and it reports
      // the same absence whether a modifier or a raw lift asked it.
      var row = RowContaining("Nope");
      var column = ColumnContaining("Nope");

      Assert.Equal(
        Miss(Text().OffsetBy(OffsetStrategies.To(row)), Rows()),
        Miss(Text().On(row), Rows()));

      Assert.Equal(
        Miss(Text().OffsetBy(OffsetStrategies.Past(row)), Rows()),
        Miss(Text().Below(row), Rows()));

      Assert.Equal(
        Miss(Text().OffsetBy(OffsetStrategies.To(column)), Columns()),
        Miss(Text().On(column), Columns()));

      Assert.Equal(
        Miss(Text().OffsetBy(OffsetStrategies.Past(column)), Columns()),
        Miss(Text().RightOf(column), Columns()));
    }

    [Fact]
    public void AMissingLandmarkIsAbsorbable()
    {
      // A disagreement about the data rather than a broken projection, so a tolerance boundary is
      // allowed to take it — as a Warning that still carries the matcher's own words.
      var result = Text().Below(RowContaining("Nope")).Optional().MapWithDiagnostics(Rows());

      Assert.Null(result.Value);
      Assert.Contains(
        result.Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning && d.Message.Contains("no row containing 'Nope'"));
    }

    // --- Forwarding -----------------------------------------------------------------------------------------

    [Fact]
    public void TheAnchorModifiersForwardToTheirStrategies()
    {
      // Proved by behaviour rather than by reference: an anchor wired to the wrong lift would still
      // compile, and On/Below differ by exactly the one row this would swap.
      var row = Detail();
      var column = DetailColumn();

      AssertSameOffset(Text().OffsetBy(OffsetStrategies.To(row)), Text().On(row), Rows());
      AssertSameOffset(Text().OffsetBy(OffsetStrategies.Past(row)), Text().Below(row), Rows());
      AssertSameOffset(Text().OffsetBy(OffsetStrategies.To(column)), Text().On(column), Columns());
      AssertSameOffset(Text().OffsetBy(OffsetStrategies.Past(column)), Text().RightOf(column), Columns());
    }

    [Fact]
    public void OnAndBelowLandADifferentRowApart()
    {
      // The guard on the test above: if both modifiers were wired to the same lift it would still
      // pass, so pin that the two genuinely disagree on this grid.
      Assert.Equal("Detail", Text().On(Detail()).Map(Rows()));
      Assert.Equal("a", Text().Below(Detail()).Map(Rows()));
      Assert.Equal("Detail", Text().On(DetailColumn()).Map(Columns()));
      Assert.Equal("a", Text().RightOf(DetailColumn()).Map(Columns()));
    }

    // --- Guards ---------------------------------------------------------------------------------------------

    [Fact]
    public void TheAnchorModifiersRejectANullLandmark()
    {
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Text().On((IRowLandmark)null!)).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Text().On((IColumnLandmark)null!)).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Text().Below(null!)).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Text().RightOf(null!)).ParamName);
    }

    [Fact]
    public void TheAnchorModifiersRejectANullProjection()
    {
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).On(Detail())).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).On(DetailColumn())).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).Below(Detail())).ParamName);
      Assert.Equal("projection", Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).RightOf(DetailColumn())).ParamName);
    }

    private static string Miss(IProjection<string> projection, ISpace space)
      => Assert.Throws<ProjectionException>(() => projection.Map(space)).Message;

    private static void AssertSameOffset(IProjection<string> lifted, IProjection<string> anchored, ISpace space)
    {
      var expected = lifted.Apply(space);
      var actual = anchored.Apply(space);

      Assert.Equal(expected.Value, actual.Value);
      Assert.Equal(expected.Offset.Size.Width, actual.Offset.Size.Width);
      Assert.Equal(expected.Offset.Size.Height, actual.Offset.Size.Height);
    }
  }
}
