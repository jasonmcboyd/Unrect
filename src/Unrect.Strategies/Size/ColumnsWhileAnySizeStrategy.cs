using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The leading columns in which some cell satisfies the predicate, the full height down.</summary>
  internal sealed class ColumnsWhileAnySizeStrategy : ISizeStrategy
  {
    public ColumnsWhileAnySizeStrategy(Func<Point<ISpace>, bool> predicate)
    {
      ColumnSelectionStrategy = ColumnStrategies.TakeColumnsWhileAny(predicate);
    }

    private IColumnStrategy ColumnSelectionStrategy { get; }

    public ISizeScan Begin(Orientation along)
      => along == Orientation.Horizontal
        ? new ColumnsSizeScan(ColumnSelectionStrategy.Begin(), fullHeight: true)
        : new Scanning.WholeSize(region => new Size(Scans.SelectColumns(ColumnSelectionStrategy, region), region.Area.Height), along);
  }
}
