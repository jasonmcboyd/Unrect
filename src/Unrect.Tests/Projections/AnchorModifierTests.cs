using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Fluent;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The three anchors — <c>.On</c>, <c>.Below</c>, <c>.RightOf</c> — which say where a projection
  /// starts by naming something in the grid rather than a distance to it. They are one-liners over
  /// <c>OffsetStrategies.To</c>/<c>Past</c>, so what is worth pinning is not new machinery but the
  /// four claims the words make: which cell you land on, that the step past a match is exactly one,
  /// that an anchor <em>replaces a default</em> offset while a movement composes onto it and a
  /// second declared position is refused outright, and that a landmark that matches nothing is loud
  /// in the same words whichever spelling reached it.
  /// <para>
  /// The axis is carried by the argument's type and enforced by overload resolution:
  /// <c>.Below</c> takes an <c>ILineLandmark</c> and <c>.RightOf</c> an <c>ILineLandmark</c>, so
  /// <c>.Below(Columns.Containing("x"))</c> does not compile. There is no runtime behaviour to pin
  /// there — a test could only assert that something the compiler already refused stays refused.
  /// </para>
  /// </summary>
  public class AnchorModifierTests
  {
    // A junk row, the landmark row, two rows under it.
    private static ICellSpace RowSheet() => Mixed(new object?[,] { { "junk" }, { "Detail" }, { "a" }, { "b" } });

    // The same four cells turned on their side, so the column twins read identically.
    private static ICellSpace ColumnSheet() => Mixed(new object?[,] { { "junk", "Detail", "a", "b" } });

    private static ILineLandmark<ICellSpace> Detail() => Rows.Containing("Detail");

    private static ILineLandmark<ICellSpace> DetailColumn() => Columns.Containing("Detail");

    // --- On: the projection lands on the match and owns it -------------------------------------------------

    [Fact]
    public void On_LandsOnTheMatchedRowAndOwnsIt()
    {
      // Owning it is the whole difference from Below: the matched row is inside the extent, which
      // is how a caption becomes content the projection reads rather than a gap it steps over.
      var applied = On(Detail()).Of(Point().Text()).Apply(RowSheet());

      Assert.Equal("Detail", applied.Value);
      Assert.Equal(1, applied.Offset.Row);
      Assert.Equal(0, applied.Offset.Column);

      Assert.Equal(3, On(Detail()).Of(Range(b => b.Height)).Map(RowSheet()));
    }

    [Fact]
    public void On_LandsOnTheMatchedColumnAndOwnsIt()
    {
      // One word, both axes: occupancy has no direction, so the argument's type is what says which.
      var applied = On(DetailColumn()).Of(Point().Text()).Apply(ColumnSheet());

      Assert.Equal("Detail", applied.Value);
      Assert.Equal(1, applied.Offset.Column);
      Assert.Equal(0, applied.Offset.Row);

      Assert.Equal(3, On(DetailColumn()).Of(Range(b => b.Width)).Map(ColumnSheet()));
    }

    // --- Below and RightOf: exactly one beyond ---------------------------------------------------------

    [Fact]
    public void Below_StartsExactlyOneRowBelowTheMatch()
    {
      // The doc pins what the English leaves loose. "Below" could mean anywhere under the match; it
      // means the very next row — the matched row's own height, never a step the declaration chose.
      var applied = Below(Detail()).Of(Point().Text()).Apply(RowSheet());

      Assert.Equal("a", applied.Value);
      Assert.Equal(2, applied.Offset.Row);
      Assert.Equal(0, applied.Offset.Column);

      // Said the other way, so a regression in either operator shows up here: one more than On.
      Assert.Equal(
        On(Detail()).Of(Point().Text()).Apply(RowSheet()).Offset.Row + 1,
        applied.Offset.Row);

      // ...and the matched row is outside the extent, where On had it inside.
      Assert.Equal(2, Below(Detail()).Of(Range(b => b.Height)).Map(RowSheet()));
    }

    [Fact]
    public void RightOf_StartsExactlyOneColumnRightOfTheMatch()
    {
      var applied = RightOf(DetailColumn()).Of(Point().Text()).Apply(ColumnSheet());

      Assert.Equal("a", applied.Value);
      Assert.Equal(2, applied.Offset.Column);
      Assert.Equal(0, applied.Offset.Row);

      Assert.Equal(
        On(DetailColumn()).Of(Point().Text()).Apply(ColumnSheet()).Offset.Column + 1,
        applied.Offset.Column);

      Assert.Equal(2, RightOf(DetailColumn()).Of(Range(b => b.Width)).Map(ColumnSheet()));
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

      var below = Below(Detail()).Of(Point().Text());

      Assert.Equal("a|c", HorizontalFlow(h => $"{h.Text()}|{h.Next(below)}").Map(space));
    }

    [Fact]
    public void RightOf_MeansAcrossTheSheetEvenInsideAVerticalFlow()
    {
      var space = Mixed(new object?[,]
      {
        { "a", "x", "y" },
        { "Detail", "b", "c" },
      });

      var right = RightOf(DetailColumn()).Of(Point().Text());

      Assert.Equal("a|b", VerticalFlow(v => $"{v.Text()}|{v.Next(right)}").Map(space));
    }

    // --- Replace a default, refuse a declaration ---------------------------------------------------------

    // The double-anchor refusal — Down(2).Below(m), Down(2).On(m) — was a runtime ArgumentException
    // while placement was carried by postfix modifiers. With geometry expressed only through the
    // pipeline it is a COMPILE-time refusal (an anchor is an entry, not a stage), so its pins moved to
    // spike/PlacementGauntlet/MustNotCompilePipeline.cs (Z, AY); there is no runtime behaviour left to
    // assert here. What survives is the composing half, below.

    [Fact]
    public void AMovementWrittenAfterAnAnchorCarriesOnFromIt()
    {
      // The half of the old replacement pin that survives the refusal, and the vocabulary's answer
      // to "start there, then go on a bit": movements compose, so the anchor remains the
      // projection's one statement of where it starts and the step is measured from it.
      Assert.Equal("b", Below(Detail()).Down(1).Of(Point().Text()).Map(RowSheet()));
      Assert.Equal("a", On(Detail()).Down(1).Of(Point().Text()).Map(RowSheet()));
    }

    // A second placement over an anchor — Below(m).OffsetBy(…), On(m).OffsetBy(…) — was likewise a
    // runtime refusal and is now a compile-time one (OffsetBy is an entry, refused after an anchor);
    // pinned as MustNotCompilePipeline.cs (AA). No runtime behaviour remains to assert.

    [Fact]
    public void AnAnchorDiscardsAProjectionsDefaultOffset()
    {
      // Replacing is also how a default is thrown away: a Table skips the blank rows in front of it
      // unless something says otherwise, and an anchor is one of the things that can say otherwise.
      // This is the offset family's default-path guard — the discriminator the refusals above turn
      // on is whether a MODIFIER put the offset there, so a shape's own definition is replaceable
      // and silent, exactly as it always was.
      var space = Mixed(new object?[,]
      {
        { null, null },
        { "Investor", "Amount" },
        { "Acme", "10" },
      });

      Assert.Equal(new[] { "Investor", "Amount" }, Table(t => t.ColumnNames).Map(space));
      Assert.Equal(new[] { "Acme", "10" }, On(Rows.Containing("Acme")).Of(Table(t => t.ColumnNames)).Map(space));
    }

    [Fact]
    public void ADeclaredExtentSurvivesAnAnchor()
    {
      // An anchor replaces the OFFSET and nothing else. Placement is two independent halves, and
      // .Sized is the other half's own replace — so an extent the projection declared is still its
      // extent wherever the landmark puts it.
      var tall = Sized(Extent.Of(1, 2)).Of(Range(b => (b.Width, b.Height)));

      Assert.Equal((1, 2), On(Detail()).Of(tall).Map(RowSheet()));
      Assert.Equal((1, 2), Below(Detail()).Of(tall).Map(RowSheet()));

      var wide = Sized(Extent.Of(2, 1)).Of(Range(b => (b.Width, b.Height)));

      Assert.Equal((2, 1), On(DetailColumn()).Of(wide).Map(ColumnSheet()));
      Assert.Equal((2, 1), RightOf(DetailColumn()).Of(wide).Map(ColumnSheet()));
    }

    // --- A landmark that matches nothing ------------------------------------------------------------------

    [Fact]
    public void AMissingLandmarkIsLoudThroughEveryAnchor()
    {
      // Loud because it means the section the declaration describes is not the section in the file.
      Assert.Contains(
        "no row containing 'Nope' exists in the available space",
        Miss(On(Rows.Containing("Nope")).Of(Point().Text()), RowSheet()));

      Assert.Contains(
        "no row containing 'Nope' exists in the available space",
        Miss(Below(Rows.Containing("Nope")).Of(Point().Text()), RowSheet()));

      Assert.Contains(
        "no column containing 'Nope' exists in the available space",
        Miss(On(Columns.Containing("Nope")).Of(Point().Text()), ColumnSheet()));

      Assert.Contains(
        "no column containing 'Nope' exists in the available space",
        Miss(RightOf(Columns.Containing("Nope")).Of(Point().Text()), ColumnSheet()));
    }

    [Fact]
    public void AMissingLandmarkSaysWhatTheStrategySpellingSaid()
    {
      // Message identity, not merely a shared phrase: the renovation moved the spelling and must
      // not have moved the diagnostic. A matcher only locates and reports absence, and it reports
      // the same absence whether a modifier or a raw lift asked it.
      var row = Rows.Containing("Nope");
      var column = Columns.Containing("Nope");

      Assert.Equal(
        Miss(OffsetBy(OffsetStrategies.To(row)).Of(Point().Text()), RowSheet()),
        Miss(On(row).Of(Point().Text()), RowSheet()));

      Assert.Equal(
        Miss(OffsetBy(OffsetStrategies.Past(row)).Of(Point().Text()), RowSheet()),
        Miss(Below(row).Of(Point().Text()), RowSheet()));

      Assert.Equal(
        Miss(OffsetBy(OffsetStrategies.To(column)).Of(Point().Text()), ColumnSheet()),
        Miss(On(column).Of(Point().Text()), ColumnSheet()));

      Assert.Equal(
        Miss(OffsetBy(OffsetStrategies.Past(column)).Of(Point().Text()), ColumnSheet()),
        Miss(RightOf(column).Of(Point().Text()), ColumnSheet()));
    }

    [Fact]
    public void AMissingLandmarkIsAbsorbable()
    {
      // A disagreement about the data rather than a broken projection, so a tolerance boundary is
      // allowed to take it — as a Warning that still carries the matcher's own words.
      var result = Below(Rows.Containing("Nope")).Of(Point().Text()).ElseDefault().MapWithDiagnostics(RowSheet());

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

      AssertSameOffset(OffsetBy(OffsetStrategies.To(row)).Of(Point().Text()), On(row).Of(Point().Text()), RowSheet());
      AssertSameOffset(OffsetBy(OffsetStrategies.Past(row)).Of(Point().Text()), Below(row).Of(Point().Text()), RowSheet());
      AssertSameOffset(OffsetBy(OffsetStrategies.To(column)).Of(Point().Text()), On(column).Of(Point().Text()), ColumnSheet());
      AssertSameOffset(OffsetBy(OffsetStrategies.Past(column)).Of(Point().Text()), RightOf(column).Of(Point().Text()), ColumnSheet());
    }

    [Fact]
    public void OnAndBelowLandADifferentRowApart()
    {
      // The guard on the test above: if both modifiers were wired to the same lift it would still
      // pass, so pin that the two genuinely disagree on this grid.
      Assert.Equal("Detail", On(Detail()).Of(Point().Text()).Map(RowSheet()));
      Assert.Equal("a", Below(Detail()).Of(Point().Text()).Map(RowSheet()));
      Assert.Equal("Detail", On(DetailColumn()).Of(Point().Text()).Map(ColumnSheet()));
      Assert.Equal("a", RightOf(DetailColumn()).Of(Point().Text()).Map(ColumnSheet()));
    }

    // --- Guards ---------------------------------------------------------------------------------------------

    [Fact]
    public void TheAnchorModifiersRejectANullLandmark()
    {
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => On((ILineLandmark<ICellSpace>)null!).Of(Point().Text())).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => On((ILineLandmark<ICellSpace>)null!).Of(Point().Text())).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => Below((ILineLandmark<ICellSpace>)null!).Of(Point().Text())).ParamName);
      Assert.Equal("landmark", Assert.Throws<ArgumentNullException>(() => RightOf((ILineLandmark<ICellSpace>)null!).Of(Point().Text())).ParamName);
    }

    // The anchors' null-projection guard now lives on the pipeline terminal: .Of blames "projection"
    // at construction, whichever anchor opened the pipeline and whether or not it carries a demand.
    // (This replaces the retired postfix modifiers' guard, which threw on a null receiver.)

    [Fact]
    public void TheAnchorModifiersRejectANullProjection()
    {
      // Every anchor entry reaches the same .Of terminal, so pinning one plus a demanding scope pins
      // the guard for all of them; the ParamName is asserted so a weakened guard (or none) is caught.
      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentNullException>(() => On(Detail()).Of<string>(null!)).ParamName);
      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentNullException>(() => Below(Detail()).Of<string>(null!)).ParamName);
      Assert.Equal(
        "projection",
        Assert.Throws<ArgumentNullException>(() => RightOf(DetailColumn()).Of<string>(null!)).ParamName);

      // (A fourth pin stood here, on the SCOPED pipeline's own .Of overload. There is one stage
      // hierarchy now and it is the one above, so the pin said the same thing twice.)
    }

    private static string Miss(IProjectionDefinition<ICellSpace, string> projection, ICellSpace space)
      => Assert.Throws<ProjectionException>(() => projection.Map(space)).Message;

    private static void AssertSameOffset(IProjectionDefinition<ICellSpace, string> lifted, IProjectionDefinition<ICellSpace, string> anchored, ICellSpace space)
    {
      var expected = lifted.Apply(space);
      var actual = anchored.Apply(space);

      Assert.Equal(expected.Value, actual.Value);
      Assert.Equal(expected.Offset.Column, actual.Offset.Column);
      Assert.Equal(expected.Offset.Row, actual.Offset.Row);
    }
  }
}
