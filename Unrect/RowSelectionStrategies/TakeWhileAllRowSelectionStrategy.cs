using System;
using Unrect.Core;

namespace Unrect.RowSelectionStrategies
{
  public class TakeWhileAllRowSelectionStrategy<TSpace> : IRowSelectionStrategy<TSpace>
  {
    public TakeWhileAllRowSelectionStrategy(Func<TSpace, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<TSpace, bool> Predicate { get; }

    public uint SelectRows(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Height)
      {
        for (int i = 0; i < space.Area.Width; i++)
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
