using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Rows and columns discovered in one pass: each row the row rule accepts is told to the column
  /// accumulator as it is taken, so the width settles as soon as no further row could change it
  /// and the height as soon as a row is refused. A discovered block, streamed.
  /// </summary>
  internal sealed class InterleavedRowAndColumnSizeStrategy : ISizeStrategy
  {
    public InterleavedRowAndColumnSizeStrategy(IRowStrategy rowSelectionStrategy, IRowMajorColumnStrategy columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    internal IRowStrategy RowSelectionStrategy { get; }

    internal IRowMajorColumnStrategy ColumnSelectionStrategy { get; }

    public ISizeScan Begin(Orientation along)
      => along == Orientation.Vertical
        ? new Scan(RowSelectionStrategy.Begin(), ColumnSelectionStrategy)
        : new Scanning.WholeSize(Whole, along);

    private Size Whole(Plane<ISpace> region)
    {
      var rows = Scans.SelectRows(RowSelectionStrategy, region);
      var columns = ColumnAccumulators.Fold(ColumnSelectionStrategy.BeginColumns(region.Width), region.Slice(new Area(region.Width, rows)));

      return new Size(columns, rows);
    }

    private sealed class Scan : ISizeScan
    {
      private readonly IRowScan _rows;
      private readonly IRowMajorColumnStrategy _columns;
      private IColumnAccumulator? _accumulator;
      private bool _stopped;

      internal Scan(IRowScan rows, IRowMajorColumnStrategy columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public bool Incremental => true;

      public bool Take(Plane<ISpace> region, int taken)
      {
        if (_stopped)
          return false;

        _accumulator ??= _columns.BeginColumns(region.Width);

        if (!_rows.IncludesRow(region, taken))
        {
          _stopped = true;
          return false;
        }

        if (!_accumulator.IsSettled)
          _accumulator.Include(region, taken);

        return true;
      }

      public int? Across(Plane<ISpace> region, int taken, bool final)
      {
        _accumulator ??= _columns.BeginColumns(region.Width);

        return _accumulator.IsSettled || _stopped || final ? _accumulator.Count : (int?)null;
      }

      public int Along(Plane<ISpace> region, int taken)
        => _rows.Required is int required && taken < required ? throw new OutOfBoundsException() : taken;

      public bool Complete(int taken) => _rows.Required is not int required || taken == required;

      public Size Declared => default;
    }
  }
}
