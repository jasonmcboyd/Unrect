using System;
using Unrect.Core;

namespace Unrect.RowSelectionStrategies
{
  public class TakeWhileRowSelectionStrategy<TSpace> : IRowSelectionStrategy<TSpace>
  {
    public TakeWhileRowSelectionStrategy(Func<ISpace<TSpace>, uint, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace<TSpace>, uint, bool> Predicate { get; }

    public uint SelectRows(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Size.Height && Predicate(space, count))
        count++;

      return count;
    }
  }
}
