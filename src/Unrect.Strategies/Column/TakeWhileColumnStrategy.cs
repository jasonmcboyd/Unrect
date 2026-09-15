using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy : IColumnStrategy
  {
    public TakeWhileColumnStrategy(Func<Plane<ISpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public int SelectColumns(Plane<ISpace> space)
    {
      int count = 0;

      while (count < space.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
