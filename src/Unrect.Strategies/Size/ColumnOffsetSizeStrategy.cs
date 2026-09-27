using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A number of columns, as a size with no height: what a column rule lifts to when it is an offset.</summary>
  internal sealed class ColumnOffsetSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public ColumnOffsetSizeStrategy(ILineStrategy<TSpace> columnSelectionStrategy)
    {
      ColumnSelectionStrategy = LineAxis.Require(columnSelectionStrategy, Orientation.Horizontal, nameof(columnSelectionStrategy));
    }

    internal ILineStrategy<TSpace> ColumnSelectionStrategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Orientation.Horizontal
        ? new ColumnsSizeScan<TSpace>(ColumnSelectionStrategy.Begin(), fullHeight: false)
        : new Scanning.WholeSize<TSpace>(region => new Size(Scans.SelectLines(ColumnSelectionStrategy, region), 0), along);
  }
}
