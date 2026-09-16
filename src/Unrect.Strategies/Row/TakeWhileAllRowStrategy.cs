using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllRowStrategy : IIncrementalRowStrategy, IRowScan
  {
    public TakeWhileAllRowStrategy(Func<Point<ISpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Point<ISpace>, bool> Predicate { get; }

    // The rule carries nothing from row to row, so one instance is every scan of it.
    public IRowScan BeginRows() => this;

    public int SelectRows(Plane<ISpace> space) => Scans.Fold(BeginRows(), space);

    public bool IncludesRow(Plane<ISpace> space, int row)
    {
      for (int i = 0; i < space.Width; i++)
      {
        if (!Predicate(space[i, row]))
          return false;
      }

      return true;
    }
  }
}
