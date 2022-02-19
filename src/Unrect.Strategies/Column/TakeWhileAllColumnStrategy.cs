using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeWhileAllColumnStrategy<TSpace> : IColumnStrategy<TSpace>
  {
    public TakeWhileAllColumnStrategy(Func<TSpace, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<TSpace, bool> Predicate { get; }

    public uint SelectColumns(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Size.Width)
      {
        for (int i = 0; i < space.Area.Size.Height; i++)
        {
          if (!Predicate(space[i, (int)count]))
            return count;
        }
        count++;
      }

      return count;
    }
  }
}
