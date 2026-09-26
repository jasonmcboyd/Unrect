using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The leading columns in which some cell satisfies the predicate, the full height down.</summary>
  internal sealed class ColumnsWhileAnySizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public ColumnsWhileAnySizeStrategy(Func<Point<TSpace>, bool> predicate)
    {
      ColumnSelectionStrategy = ColumnStrategies.TakeColumnsWhileAny(predicate);
    }

    private ILineStrategy<TSpace> ColumnSelectionStrategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Orientation.Horizontal
        ? new ColumnsSizeScan<TSpace>(ColumnSelectionStrategy.Begin(), fullHeight: true)
        : new Scanning.WholeSize<TSpace>(region => new Size(Scans.SelectLines(ColumnSelectionStrategy, region), region.Height), along);
  }
}
