using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// Every strategy builds its own scan, along either axis, and says whether that scan answers as
  /// the spans arrive or only over the whole region. Which is which is a promise the engine's holds
  /// rest on, so it is pinned: a strategy that streams along the axis it was written for and answers
  /// whole along the other, one that streams both ways, and one that streams neither.
  /// <para>
  /// The other half of the old identity suite — that a whole region's answer is the fold of the
  /// scan — is definitional now (<see cref="Scans"/>), so the strategy suites pin the answers and
  /// nothing pins the tautology.
  /// </para>
  /// </summary>
  public class ScanCensusTests
  {
    [Theory]
    [InlineData("rows while any value", true, false)]
    [InlineData("columns while any value", false, true)]
    [InlineData("explicit size", true, true)]
    [InlineData("max size", true, true)]
    [InlineData("select size", false, false)]
    [InlineData("rows then columns", true, false)]
    [InlineData("columns then rows", false, true)]
    [InlineData("discovered block", true, false)]
    public void ASizeStrategyStreamsAlongTheAxisItWasWrittenFor(string strategy, bool alongRows, bool alongColumns)
    {
      IAreaStrategy size = strategy switch
      {
        "rows while any value" => SizeStrategies.RowsWhileAnyValue().ToAreaStrategy(),
        "columns while any value" => SizeStrategies.ColumnsWhileAnyValue().ToAreaStrategy(),
        "explicit size" => AreaStrategies.ExplicitArea(2, 3),
        "max size" => AreaStrategies.MaxArea(),
        "select size" => AreaStrategies.SelectArea(plane => new Size(1, 1)),
        "rows then columns" => AreaStrategies.RowsThenColumns(RowStrategies.TakeRows(2), ColumnStrategies.TakeColumnsTo((_, column) => column == 1)),
        "columns then rows" => AreaStrategies.ColumnsThenRows(ColumnStrategies.TakeColumns(2), RowStrategies.TakeRowsWhileAnyValue()),
        "discovered block" => AreaStrategies.RowsThenColumns(RowStrategies.TakeRowsWhileAnyValue(), ColumnStrategies.TakeColumnsWhileAnyValue()),
        _ => throw new System.ArgumentOutOfRangeException(nameof(strategy)),
      };

      Assert.Equal(alongRows, size.Begin(Orientation.Vertical).Incremental);
      Assert.Equal(alongColumns, size.Begin(Orientation.Horizontal).Incremental);
    }

    [Theory]
    [InlineData("skip blank rows", true, false)]
    [InlineData("skip blank columns", false, true)]
    [InlineData("explicit offset", true, true)]
    [InlineData("first non-blank cell", true, true)]
    [InlineData("select offset", false, false)]
    [InlineData("to a row landmark", true, false)]
    [InlineData("to a column landmark", false, true)]
    [InlineData("a chain of row skips", true, false)]
    [InlineData("a chain with a lambda in it", false, false)]
    public void AnOffsetStrategyStreamsAlongTheAxisItWasWrittenFor(string strategy, bool alongRows, bool alongColumns)
    {
      var offset = strategy switch
      {
        "skip blank rows" => OffsetStrategies.SkipBlankRows(),
        "skip blank columns" => OffsetStrategies.SkipBlankColumns(),
        "explicit offset" => OffsetStrategies.ExplicitOffset(1, 1),
        "first non-blank cell" => OffsetStrategies.SkipToFirstNonBlankCell(),
        "select offset" => OffsetStrategies.SelectOffset(plane => new Size(0, 1)),
        "to a row landmark" => OffsetStrategies.To(RowLandmarks.RowContaining("x")),
        "to a column landmark" => OffsetStrategies.To(ColumnLandmarks.ColumnContaining("x")),
        "a chain of row skips" => OffsetStrategies.Then(OffsetStrategies.SkipBlankRows(), OffsetStrategies.ExplicitOffset(0, 1)),
        "a chain with a lambda in it" => OffsetStrategies.Then(OffsetStrategies.SkipBlankRows(), OffsetStrategies.SelectOffset(plane => new Size(0, 1))),
        _ => throw new System.ArgumentOutOfRangeException(nameof(strategy)),
      };

      Assert.Equal(alongRows, offset.Begin(Orientation.Vertical).Incremental);
      Assert.Equal(alongColumns, offset.Begin(Orientation.Horizontal).Incremental);
    }

    [Fact]
    public void AWholeRegionScanAnswersTheSameAsTheStreamingOneOverTheSameRegion()
    {
      // The two scans a strategy builds mean the same region: along the axis it streams the answer
      // is folded span by span, along the other it is settled at the end, and the size is the size.
      var space = Grid(new[,]
      {
        { 1, 2, 0 },
        { 3, 4, 0 },
        { 0, 0, 0 },
      });
      var strategy = SizeStrategies.RowsWhileAnyValue();

      var byRows = Scans.FoldSize(strategy.Begin(Orientation.Vertical), Plane<ISpace>.Of(space), Orientation.Vertical);
      var byColumns = Scans.FoldSize(strategy.Begin(Orientation.Horizontal), Plane<ISpace>.Of(space), Orientation.Horizontal);

      Assert.Equal(new Size(3, 2), byRows);
      Assert.Equal(byRows, byColumns);
    }
  }
}
