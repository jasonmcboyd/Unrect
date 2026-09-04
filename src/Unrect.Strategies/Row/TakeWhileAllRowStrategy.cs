using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllRowStrategy : IIncrementalRowStrategy, IRowScan
  {
    public TakeWhileAllRowStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    // The rule carries nothing from row to row, so one instance is every scan of it.
    public IRowScan BeginRows() => this;

    public bool IncludesRow(ISpace space, int row)
    {
      for (int i = 0; i < space.Area.Width; i++)
      {
        if (!Predicate(space[i, row]))
          return false;
      }

      return true;
    }
  }
}
