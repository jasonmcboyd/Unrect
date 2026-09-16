using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class RowsWhileAnySizeStrategy : IIncrementalSizeStrategy
  {
    public RowsWhileAnySizeStrategy(Func<Point<ISpace>, bool> predicate)
    {
      RowSelectionStrategy = new TakeWhileAnyRowStrategy(predicate);
    }

    private IIncrementalRowStrategy RowSelectionStrategy { get; }

    public IAreaScan BeginSize(Plane<ISpace> availableSpace)
      => new Scan(availableSpace.Width, RowSelectionStrategy.BeginRows());

    public Size GetSize(Plane<ISpace> availableSpace) => Scans.FoldSize(BeginSize(availableSpace), availableSpace);

    /// <summary>
    /// The width is the whole of what is available, so it is settled before a cell is read and the
    /// scan is nothing but its row strategy's, carrying that width.
    /// </summary>
    private sealed class Scan : IAreaScan
    {
      public Scan(int width, IRowScan rows)
      {
        Width = width;
        Rows = rows;
      }

      public int Width { get; }

      private IRowScan Rows { get; }

      public bool IncludesRow(Plane<ISpace> space, int row) => Rows.IncludesRow(space, row);
    }
  }
}
