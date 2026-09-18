using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The leading rows in which some cell satisfies the predicate, the full width across.</summary>
  internal sealed class RowsWhileAnySizeStrategy : ISizeStrategy
  {
    public RowsWhileAnySizeStrategy(Func<Point<ISpace>, bool> predicate)
    {
      RowSelectionStrategy = new TakeWhileAnyRowStrategy(predicate);
    }

    internal IRowStrategy RowSelectionStrategy { get; }

    public ISizeScan Begin(Orientation along)
      => along == Orientation.Vertical
        ? new RowsSizeScan(RowSelectionStrategy.Begin(), fullWidth: true)
        : new Scanning.WholeSize(region => new Size(region.Width, Scans.SelectRows(RowSelectionStrategy, region)), along);
  }

  /// <summary>
  /// Rows decided by a row scan, one per span down a region: as wide as the region, or nothing
  /// wide at all for a size that is only a number of rows to skip.
  /// </summary>
  internal sealed class RowsSizeScan : ISizeScan
  {
    private readonly IRowScan _rows;
    private readonly bool _fullWidth;

    internal RowsSizeScan(IRowScan rows, bool fullWidth)
    {
      _rows = rows;
      _fullWidth = fullWidth;
    }

    public bool Incremental => true;

    public bool Take(Plane<ISpace> region, int taken) => _rows.IncludesRow(region, taken);

    public int? Across(Plane<ISpace> region, int taken, bool final) => _fullWidth ? region.Width : 0;

    public int Along(Plane<ISpace> region, int taken)
      => _rows.Required is int required && taken < required ? throw new OutOfBoundsException() : taken;

    public bool Complete(int taken) => _rows.Required is not int required || taken == required;

    public Size Declared => _rows.Required is int required ? new Size(0, required) : default;
  }

  /// <summary>The mirror: columns decided by a column scan, one per span across a region.</summary>
  internal sealed class ColumnsSizeScan : ISizeScan
  {
    private readonly IColumnScan _columns;
    private readonly bool _fullHeight;

    internal ColumnsSizeScan(IColumnScan columns, bool fullHeight)
    {
      _columns = columns;
      _fullHeight = fullHeight;
    }

    public bool Incremental => true;

    public bool Take(Plane<ISpace> region, int taken) => _columns.IncludesColumn(region, taken);

    public int? Across(Plane<ISpace> region, int taken, bool final) => _fullHeight ? region.Area.Height : 0;

    public int Along(Plane<ISpace> region, int taken)
      => _columns.Required is int required && taken < required ? throw new OutOfBoundsException() : taken;

    public bool Complete(int taken) => _columns.Required is not int required || taken == required;

    public Size Declared => _columns.Required is int required ? new Size(required, 0) : default;
  }
}
