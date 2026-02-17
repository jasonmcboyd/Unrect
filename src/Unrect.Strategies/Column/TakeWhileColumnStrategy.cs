using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeWhileColumnStrategy<TSpace> : IColumnStrategy<TSpace>
  {
    public TakeWhileColumnStrategy(Func<ISpace<TSpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace<TSpace>, int, bool> Predicate { get; }

    public int SelectColumns(ISpace<TSpace> space)
    {
      int count = 0;

      while (count < space.Area.Size.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
