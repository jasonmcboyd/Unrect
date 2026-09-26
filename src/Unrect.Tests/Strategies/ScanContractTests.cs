using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// What the size-scan contract owes and is owed: a scan that is owed an extent answers it and the
  /// caller compares; a scan that will not answer at all has broken its contract, which is a fault.
  /// </summary>
  public class ScanContractTests
  {
    private static ICellSpace TwoRows() => Grid(new[,] { { 1, 1 }, { 1, 1 } });

    /// <summary>A scan that takes every span and then, asked at the end, refuses to say how far across it reaches.</summary>
    private sealed class Mute : ISizeStrategy<ICellSpace>, ISizeScan<ICellSpace>
    {
      public ISizeScan<ICellSpace> Begin(Orientation along) => this;

      public bool Incremental => true;

      public bool Take(Plane<ICellSpace> region, int taken) => true;

      public int? Across(Plane<ICellSpace> region, int taken, bool final) => null;

      public int Along(Plane<ICellSpace> region, int taken) => taken;

      public Size? Required => null;
    }

    [Fact]
    public void AScanThatWillNotSayHowFarAcrossItReachesHasBrokenItsContract()
    {
      // Not a bounds condition: the region was there and the scan took all of it. The fold names
      // the breach, and names it as the scan's rather than the data's.
      var failure = Assert.Throws<ScanContractException>(() => new Mute().GetSize(Plane<ICellSpace>.Of(TwoRows())));

      Assert.Contains("how far across", failure.Message);
    }

    [Fact]
    public void ABrokenContractIsAFaultNoToleranceAbsorbs()
    {
      // A scan that THROWS over the data is the source's rule failing, and Optional may take it. A
      // scan that breaks its contract is the strategy's code, and Optional must not turn that into
      // an absent section.
      var tolerant = Sized(new Mute()).Of(Point()).Optional();

      var failure = Assert.Throws<ProjectionException>(() => tolerant.Map(TwoRows()));

      Assert.Contains("ScanContractException", failure.Message);
    }

    [Fact]
    public void AScanOwedRowsAnswersTheRowsItIsOwedAndTheFoldLeavesTheComparisonToTheCaller()
    {
      // The fold's contract is uniform across the family: what a scan is owed is what it answers,
      // shown enough spans or not, exactly as an explicit extent does. The caller compares with the
      // region — which is how a placement can say what did not fit rather than only that something did.
      var owed = SizeStrategies.RowsThenColumns(RowStrategies.TakeRows<ICellSpace>(5), ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ICellSpace>());

      var scan = owed.Begin(Orientation.Vertical);

      Assert.Equal(new Size(0, 5), scan.Required);
      Assert.Equal(5, owed.GetSize(Plane<ICellSpace>.Of(TwoRows())).Height);
    }

    [Fact]
    public void APlacementOwedMoreRowsThanItWasShownSaysHowManyItWasOwed()
    {
      // The message quotes what was required on the axis that was required, and nothing on the
      // axis that was discovered: 0x5, never 0x0. Pinned because the interleaved rows-and-columns
      // scan used to declare nothing while still requiring five.
      var owed = SizeStrategies.RowsThenColumns(RowStrategies.TakeRows<ICellSpace>(5), ColumnStrategies.TakeColumnsWhileAnyIsNotBlank<ICellSpace>());

      var failure = Assert.Throws<ProjectionException>(() => Sized(owed).Of(Point()).Map(TwoRows()));

      Assert.Contains("an extent of 0x5 does not fit here", failure.Message);
    }
  }
}
