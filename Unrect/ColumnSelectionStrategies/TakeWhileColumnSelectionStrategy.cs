using System;
using Unrect.Core;

namespace Unrect.ColumnSelectionStrategies
{
  public class TakeWhileColumnSelectionStrategy<TSpace> : IColumnSelectionStrategy<TSpace>
  {
    public TakeWhileColumnSelectionStrategy(Func<ISpace<TSpace>, uint, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace<TSpace>, uint, bool> Predicate { get; }

    public uint SelectColumns(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Width && Predicate(space, count))
        count++;

      return count;
    }
  }
}
