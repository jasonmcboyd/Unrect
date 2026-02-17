using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeToRowStrategy<TSpace> : IRowStrategy<TSpace>
  {
    public TakeToRowStrategy(Func<ISpace<TSpace>, int, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<ISpace<TSpace>, int, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public int SelectRows(ISpace<TSpace> space)
    {
      int count = 0;

      while (count < space.Area.Size.Height && !Predicate(space, count))
        count++;

      return KeepMatchingRow && count < space.Area.Size.Height ? count + 1 : count;
    }
  }
}
