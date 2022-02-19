using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeToAllRowStrategy<TSpace> : IRowStrategy<TSpace>
  {
    public TakeToAllRowStrategy(Func<TSpace, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<TSpace, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public uint SelectRows(ISpace<TSpace> space)
    {
      uint count = 0;

      while (count < space.Area.Size.Height)
      {
        for (int i = 0; i < space.Area.Size.Width; i++)
        {
          if (!Predicate(space[i, (int)count]))
            return count;
        }
        count++;
      }

      return KeepMatchingRow && count < space.Area.Size.Height ? count + 1 : count;
    }
  }
}
