using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// One rule along an axis, then the other across the region it took. Streams along the axis of
  /// its first rule — the rows taken one at a time, then the columns folded over them once the
  /// rows have settled — and answers over the whole region along the other.
  /// </summary>
  internal sealed class RowAndColumnSizeStrategy : ISizeStrategy
  {
    private RowAndColumnSizeStrategy(IRowStrategy rowSelectionStrategy, IColumnStrategy columnSelectionStrategy, bool rowFirst)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = rowFirst;
    }

    internal IRowStrategy RowSelectionStrategy { get; }

    internal IColumnStrategy ColumnSelectionStrategy { get; }

    internal bool RowFirst { get; }

    public static ISizeStrategy RowsThenColumns(IRowStrategy rows, IColumnStrategy columns)
    {
      // Two rules that can both be told about a row as it arrives decide the width and the height
      // in one pass, which is what a discovered block on a streamed sheet wants.
      if (columns is IRowMajorColumnStrategy rowMajorColumns)
        return new InterleavedRowAndColumnSizeStrategy(rows, rowMajorColumns);

      return new RowAndColumnSizeStrategy(rows, columns, rowFirst: true);
    }

    public static ISizeStrategy ColumnsThenRows(IColumnStrategy columns, IRowStrategy rows)
      => new RowAndColumnSizeStrategy(rows, columns, rowFirst: false);

    public ISizeScan Begin(Orientation along)
    {
      if (RowFirst && along == Orientation.Vertical)
        return new RowsThenAcross(RowSelectionStrategy.Begin(), region => Scans.SelectColumns(ColumnSelectionStrategy, region));

      if (!RowFirst && along == Orientation.Horizontal)
        return new ColumnsThenAcross(ColumnSelectionStrategy.Begin(), region => Scans.SelectRows(RowSelectionStrategy, region));

      return new Scanning.WholeSize(Whole, along);
    }

    private Size Whole(Plane<ISpace> region)
    {
      if (RowFirst)
      {
        var rowCount = Scans.SelectRows(RowSelectionStrategy, region);
        var columnCount = Scans.SelectColumns(ColumnSelectionStrategy, region.Slice(new Area(region.Width, rowCount)));

        return new Size(columnCount, rowCount);
      }
      else
      {
        var columnCount = Scans.SelectColumns(ColumnSelectionStrategy, region);
        var rowCount = Scans.SelectRows(RowSelectionStrategy, region.Slice(new Area(columnCount, region.Area.Height)));

        return new Size(columnCount, rowCount);
      }
    }

    /// <summary>Rows one at a time; the columns folded over the rows taken once those have settled.</summary>
    private sealed class RowsThenAcross : ISizeScan
    {
      private readonly IRowScan _rows;
      private readonly System.Func<Plane<ISpace>, int> _columns;
      private bool _stopped;

      internal RowsThenAcross(IRowScan rows, System.Func<Plane<ISpace>, int> columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public bool Incremental => true;

      public bool Take(Plane<ISpace> region, int taken)
      {
        if (_stopped)
          return false;

        var take = _rows.IncludesRow(region, taken);

        if (!take)
          _stopped = true;

        return take;
      }

      public int? Across(Plane<ISpace> region, int taken, bool final)
      {
        var settled = final || _stopped || (_rows.Required is int required && taken >= required);

        return settled ? _columns(region.Slice(new Area(region.Width, taken))) : (int?)null;
      }

      public int Along(Plane<ISpace> region, int taken)
        => _rows.Required is int required && taken < required ? throw new OutOfBoundsException() : taken;

      public bool Complete(int taken) => _rows.Required is not int required || taken == required;

      public Size Declared => new Size(0, _rows.Required ?? 0);
    }

    /// <summary>The mirror: columns one at a time; the rows folded over the columns taken once those have settled.</summary>
    private sealed class ColumnsThenAcross : ISizeScan
    {
      private readonly IColumnScan _columns;
      private readonly System.Func<Plane<ISpace>, int> _rows;
      private bool _stopped;

      internal ColumnsThenAcross(IColumnScan columns, System.Func<Plane<ISpace>, int> rows)
      {
        _columns = columns;
        _rows = rows;
      }

      public bool Incremental => true;

      public bool Take(Plane<ISpace> region, int taken)
      {
        if (_stopped)
          return false;

        var take = _columns.IncludesColumn(region, taken);

        if (!take)
          _stopped = true;

        return take;
      }

      public int? Across(Plane<ISpace> region, int taken, bool final)
      {
        var settled = final || _stopped || (_columns.Required is int required && taken >= required);

        return settled ? _rows(region.Slice(new Area(taken, region.Area.Height))) : (int?)null;
      }

      public int Along(Plane<ISpace> region, int taken)
        => _columns.Required is int required && taken < required ? throw new OutOfBoundsException() : taken;

      public bool Complete(int taken) => _columns.Required is not int required || taken == required;

      public Size Declared => new Size(_columns.Required ?? 0, 0);
    }
  }
}
