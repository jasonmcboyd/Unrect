using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// One rule along an axis, then the other across the region it took. Streams along the axis of
  /// its first rule — the rows taken one at a time, then the columns folded over them once the
  /// rows have settled — and answers over the whole region along the other.
  /// </summary>
  internal sealed class RowAndColumnSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    private RowAndColumnSizeStrategy(ILineStrategy<TSpace> rowSelectionStrategy, ILineStrategy<TSpace> columnSelectionStrategy, bool rowFirst)
    {
      RowSelectionStrategy = LineAxis.Require(rowSelectionStrategy, Orientation.Vertical, "rows");
      ColumnSelectionStrategy = LineAxis.Require(columnSelectionStrategy, Orientation.Horizontal, "columns");
      RowFirst = rowFirst;
    }

    internal ILineStrategy<TSpace> RowSelectionStrategy { get; }

    internal ILineStrategy<TSpace> ColumnSelectionStrategy { get; }

    internal bool RowFirst { get; }

    public static ISizeStrategy<TSpace> RowsThenColumns(ILineStrategy<TSpace> rows, ILineStrategy<TSpace> columns)
    {
      // Two rules that can both be told about a row as it arrives decide the width and the height
      // in one pass, which is what a discovered block on a streamed sheet wants.
      LineAxis.Require(rows, Orientation.Vertical, nameof(rows));
      LineAxis.Require(columns, Orientation.Horizontal, nameof(columns));

      if (columns is IRowMajorColumnStrategy<TSpace> rowMajorColumns)
        return new InterleavedRowAndColumnSizeStrategy<TSpace>(rows, rowMajorColumns);

      return new RowAndColumnSizeStrategy<TSpace>(rows, columns, rowFirst: true);
    }

    public static ISizeStrategy<TSpace> ColumnsThenRows(ILineStrategy<TSpace> columns, ILineStrategy<TSpace> rows)
      => new RowAndColumnSizeStrategy<TSpace>(rows, columns, rowFirst: false);

    public ISizeScan<TSpace> Begin(Orientation along)
    {
      if (RowFirst && along == Orientation.Vertical)
        return new RowsThenAcross(RowSelectionStrategy.Begin(), region => Scans.SelectLines(ColumnSelectionStrategy, region));

      if (!RowFirst && along == Orientation.Horizontal)
        return new ColumnsThenAcross(ColumnSelectionStrategy.Begin(), region => Scans.SelectLines(RowSelectionStrategy, region));

      return new Scanning.WholeSize<TSpace>(Whole, along);
    }

    private Size Whole(Plane<TSpace> region)
    {
      if (RowFirst)
      {
        var rowCount = Scans.SelectLines(RowSelectionStrategy, region);
        var columnCount = Scans.SelectLines(ColumnSelectionStrategy, region.Slice(new Size(region.Width, rowCount)));

        return new Size(columnCount, rowCount);
      }
      else
      {
        var columnCount = Scans.SelectLines(ColumnSelectionStrategy, region);
        var rowCount = Scans.SelectLines(RowSelectionStrategy, region.Slice(new Size(columnCount, region.Height)));

        return new Size(columnCount, rowCount);
      }
    }

    /// <summary>Rows one at a time; the columns folded over the rows taken once those have settled.</summary>
    private sealed class RowsThenAcross : ISizeScan<TSpace>
    {
      private readonly ILineScan<TSpace> _rows;
      private readonly System.Func<Plane<TSpace>, int> _columns;
      private bool _stopped;

      internal RowsThenAcross(ILineScan<TSpace> rows, System.Func<Plane<TSpace>, int> columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken)
      {
        if (_stopped)
          return false;

        var take = _rows.Includes(region, taken);

        if (!take)
          _stopped = true;

        return take;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final)
      {
        var settled = final || _stopped || (_rows.Required is int required && taken >= required);

        return settled ? _columns(region.Slice(new Size(region.Width, taken))) : (int?)null;
      }

      public int Along(Plane<TSpace> region, int taken)
        => _rows.Required ?? taken;

      public Size? Required => _rows.Required is int required ? new Size(0, required) : null;
    }

    /// <summary>The mirror: columns one at a time; the rows folded over the columns taken once those have settled.</summary>
    private sealed class ColumnsThenAcross : ISizeScan<TSpace>
    {
      private readonly ILineScan<TSpace> _columns;
      private readonly System.Func<Plane<TSpace>, int> _rows;
      private bool _stopped;

      internal ColumnsThenAcross(ILineScan<TSpace> columns, System.Func<Plane<TSpace>, int> rows)
      {
        _columns = columns;
        _rows = rows;
      }

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int taken)
      {
        if (_stopped)
          return false;

        var take = _columns.Includes(region, taken);

        if (!take)
          _stopped = true;

        return take;
      }

      public int? Across(Plane<TSpace> region, int taken, bool final)
      {
        var settled = final || _stopped || (_columns.Required is int required && taken >= required);

        return settled ? _rows(region.Slice(new Size(taken, region.Height))) : (int?)null;
      }

      public int Along(Plane<TSpace> region, int taken)
        => _columns.Required ?? taken;

      public Size? Required => _columns.Required is int required ? new Size(required, 0) : null;
    }
  }
}
