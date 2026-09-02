using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy : IColumnStrategy
  {
    public TakeWhileColumnStrategy(Func<ISpace, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace, int, bool> Predicate { get; }

    public int SelectColumns(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
