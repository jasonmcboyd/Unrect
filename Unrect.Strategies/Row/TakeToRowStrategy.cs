using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeToRowStrategy<TSpace> : IRowStrategy<TSpace>
  {
    public TakeToRowStrategy(Func<ISpace<TSpace>, uint, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<ISpace<TSpace>, uint, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public uint SelectRows(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Size.Height && !Predicate(space, count))
        count++;

      return KeepMatchingRow && count < space.Area.Size.Height ? count + 1 : count;
    }
  }
}
