using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The leading rows in which some cell satisfies the predicate, the full width across.</summary>
  internal sealed class RowsWhileAnySizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public RowsWhileAnySizeStrategy(Func<Point<TSpace>, bool> predicate)
    {
      RowSelectionStrategy = new TakeWhileAnyRowStrategy<TSpace>(predicate);
    }

    internal ILineStrategy<TSpace> RowSelectionStrategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along)
      => along == Orientation.Vertical
        ? new RowsSizeScan<TSpace>(RowSelectionStrategy.Begin(), fullWidth: true)
        : new Scanning.WholeSize<TSpace>(region => new Size(region.Width, Scans.SelectLines(RowSelectionStrategy, region)), along);
  }

  /// <summary>
  /// Rows decided by a row scan, one per span down a region: as wide as the region, or nothing
  /// wide at all for a size that is only a number of rows to skip.
  /// </summary>
  internal sealed class RowsSizeScan<TSpace> : ISizeScan<TSpace>
    where TSpace : class, ISpace
  {
    private readonly ILineScan<TSpace> _rows;
    private readonly bool _fullWidth;

    internal RowsSizeScan(ILineScan<TSpace> rows, bool fullWidth)
    {
      _rows = rows;
      _fullWidth = fullWidth;
    }

    public bool Incremental => true;

    public bool Take(Plane<TSpace> region, int taken) => _rows.Includes(region, taken);

    public int? Across(Plane<TSpace> region, int taken, bool final) => _fullWidth ? region.Width : 0;

    public int Along(Plane<TSpace> region, int taken)
      => _rows.Required ?? taken;

    public Size? Required => _rows.Required is int required ? new Size(0, required) : null;
  }

  /// <summary>The mirror: columns decided by a column scan, one per span across a region.</summary>
  internal sealed class ColumnsSizeScan<TSpace> : ISizeScan<TSpace>
    where TSpace : class, ISpace
  {
    private readonly ILineScan<TSpace> _columns;
    private readonly bool _fullHeight;

    internal ColumnsSizeScan(ILineScan<TSpace> columns, bool fullHeight)
    {
      _columns = columns;
      _fullHeight = fullHeight;
    }

    public bool Incremental => true;

    public bool Take(Plane<TSpace> region, int taken) => _columns.Includes(region, taken);

    public int? Across(Plane<TSpace> region, int taken, bool final) => _fullHeight ? region.Height : 0;

    public int Along(Plane<TSpace> region, int taken)
      => _columns.Required ?? taken;

    public Size? Required => _columns.Required is int required ? new Size(required, 0) : null;
  }
}
