using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A number of columns, as a size with no height: what a column rule lifts to when it is an offset.</summary>
  internal sealed class ColumnOffsetSizeStrategy : ISizeStrategy
  {
    public ColumnOffsetSizeStrategy(IColumnStrategy columnSelectionStrategy)
    {
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    internal IColumnStrategy ColumnSelectionStrategy { get; }

    public ISizeScan Begin(Orientation along)
      => along == Orientation.Horizontal
        ? new ColumnsSizeScan(ColumnSelectionStrategy.Begin(), fullHeight: false)
        : new Scanning.WholeSize(region => new Size(Scans.SelectColumns(ColumnSelectionStrategy, region), 0), along);
  }
}
