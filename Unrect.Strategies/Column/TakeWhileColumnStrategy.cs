using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeWhileColumnStrategy<TSpace> : IColumnStrategy<TSpace>
  {
    public TakeWhileColumnStrategy(Func<ISpace<TSpace>, uint, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace<TSpace>, uint, bool> Predicate { get; }

    public uint SelectColumns(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Size.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
