using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A number of rows, as a size with no width: what a row rule lifts to when it is an offset.</summary>
  internal sealed class RowOffsetSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public RowOffsetSizeStrategy(ILineStrategy<TSpace> rowSelectionStrategy)
    {
      RowSelectionStrategy = LineAxis.Require(rowSelectionStrategy, Orientation.Vertical, nameof(rowSelectionStrategy));
    }

    internal ILineStrategy<TSpace> RowSelectionStrategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Orientation.Vertical
        ? new RowsSizeScan<TSpace>(RowSelectionStrategy.Begin(), fullWidth: false)
        : new Scanning.WholeSize<TSpace>(region => new Size(0, Scans.SelectLines(RowSelectionStrategy, region)), along);
  }
}
