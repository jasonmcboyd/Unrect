using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A number of rows, as a size with no width: what a row rule lifts to when it is an offset.</summary>
  internal sealed class RowOffsetSizeStrategy : ISizeStrategy
  {
    public RowOffsetSizeStrategy(ILineStrategy rowSelectionStrategy)
    {
      RowSelectionStrategy = LineAxis.Require(rowSelectionStrategy, Orientation.Vertical, nameof(rowSelectionStrategy));
    }

    internal ILineStrategy RowSelectionStrategy { get; }

    public ISizeScan Begin(Orientation along)
      => along == Orientation.Vertical
        ? new RowsSizeScan(RowSelectionStrategy.Begin(), fullWidth: false)
        : new Scanning.WholeSize(region => new Size(0, Scans.SelectLines(RowSelectionStrategy, region)), along);
  }
}
