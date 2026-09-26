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
      ISizeStrategy<ISpace> size = strategy switch
      {
        "rows while any value" => SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>(),
        "columns while any value" => SizeStrategies.ColumnsWhileAnyIsNotBlank<ISpace>(),
        "explicit size" => SizeStrategies.ExplicitSize<ISpace>(2, 3),
        "max size" => SizeStrategies.MaxSize<ISpace>(),
        "select size" => SizeStrategies.SelectSize<ISpace>(plane => new Size(1, 1)),
        "rows then columns" => SizeStrategies.RowsThenColumns(RowStrategies.TakeRows<ISpace>(2), ColumnStrategies.TakeColumnsTo<ISpace>((_, column) => column == 1)),
        "columns then rows" => SizeStrategies.ColumnsThenRows(ColumnStrategies.TakeColumns<ISpace>(2), RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>()),
        "discovered block" => SizeStrategies.RowsThenColumns(RowStrategies.TakeRowsWhileAnyIsNotBlank<ISpace>(), ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ISpace>()),
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
        "skip blank rows" => OffsetStrategies.SkipBlankRows<ISpace>(),
        "skip blank columns" => OffsetStrategies.SkipBlankColumns<ISpace>(),
        "explicit offset" => OffsetStrategies.ExplicitOffset<ISpace>(1, 1),
        "first non-blank cell" => OffsetStrategies.SkipToFirstNonBlankCell<ISpace>(),
        "select offset" => OffsetStrategies.SelectOffset<ISpace>(plane => new Size(0, 1)),
        "to a row landmark" => OffsetStrategies.To(RowLandmarks.RowSaying<ISpace>("x")),
        "to a column landmark" => OffsetStrategies.To(ColumnLandmarks.ColumnSaying<ISpace>("x")),
        "a chain of row skips" => OffsetStrategies.Then(OffsetStrategies.SkipBlankRows<ISpace>(), OffsetStrategies.ExplicitOffset<ISpace>(0, 1)),
        "a chain with a lambda in it" => OffsetStrategies.Then(OffsetStrategies.SkipBlankRows<ISpace>(), OffsetStrategies.SelectOffset<ISpace>(plane => new Size(0, 1))),
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
      var strategy = SizeStrategies.RowsWhileAnyIsNotBlank<ISpace>();

      var byRows = Scans.FoldSize<ISpace>(strategy.Begin(Orientation.Vertical), Plane<ISpace>.Of(space), Orientation.Vertical);
      var byColumns = Scans.FoldSize<ISpace>(strategy.Begin(Orientation.Horizontal), Plane<ISpace>.Of(space), Orientation.Horizontal);

      Assert.Equal(new Size(3, 2), byRows);
      Assert.Equal(byRows, byColumns);
    }
  }
}
