using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy : IColumnStrategy
  {
    public TakeWhileColumnStrategy(Func<ICellValues, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ICellValues, int, bool> Predicate { get; }

    public int SelectColumns(ICellValues space)
    {
      int count = 0;

      while (count < space.Area.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
