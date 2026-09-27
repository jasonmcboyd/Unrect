using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Rows and columns discovered in one pass: each row the row rule accepts is told to the column
  /// accumulator as it is taken, so the width settles as soon as no further row could change it
  /// and the height as soon as a row is refused. A discovered block, streamed.
  /// </summary>
  internal sealed class InterleavedRowAndColumnSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public InterleavedRowAndColumnSizeStrategy(ILineStrategy<TSpace> rowSelectionStrategy, IRowMajorColumnStrategy<TSpace> columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    internal ILineStrategy<TSpace> RowSelectionStrategy { get; }

    internal IRowMajorColumnStrategy<TSpace> ColumnSelectionStrategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Orientation.Vertical
        ? new Scan(RowSelectionStrategy.Begin(), ColumnSelectionStrategy)
        : new Scanning.WholeSize<TSpace>(Whole, along);

    private Size Whole(Plane<TSpace> region)
    {
      var rows = Scans.SelectLines(RowSelectionStrategy, region);
      var columns = ColumnAccumulators.Fold(ColumnSelectionStrategy.BeginColumns(region.Width), region.Slice(new Size(region.Width, rows)));

      return new Size(columns, rows);
    }

    private sealed class Scan : ISizeScan<TSpace>
    {
      private readonly ILineScan<TSpace> _rows;
      private readonly IRowMajorColumnStrategy<TSpace> _columns;
      private IColumnAccumulator<TSpace>? _accumulator;
      private bool _stopped;

      internal Scan(ILineScan<TSpace> rows, IRowMajorColumnStrategy<TSpace> columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken)
      {
        if (_stopped)
          return false;

        _accumulator ??= _columns.BeginColumns(region.Width);

        if (!_rows.Includes(region, taken))
        {
          _stopped = true;
          return false;
        }

        if (!_accumulator.IsSettled)
          _accumulator.Include(region, taken);

        return true;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final)
      {
        _accumulator ??= _columns.BeginColumns(region.Width);

        return _accumulator.IsSettled || _stopped || final ? _accumulator.Count : (int?)null;
      }

      public int Along(Plane<TSpace> region, int taken)
        => _rows.Required ?? taken;

      public Size? Required => _rows.Required is int required ? new Size(0, required) : null;
    }
  }
}
