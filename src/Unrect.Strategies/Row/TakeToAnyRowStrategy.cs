using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeToAnyRowStrategy<TSpace> : IRowStrategy<TSpace>
  {
    public TakeToAnyRowStrategy(Func<TSpace, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<TSpace, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public int SelectRows(ISpace<TSpace> space)
    {
      int count = 0;

      while (count < space.Area.Size.Height)
      {
        bool anyMatch = false;
        for (int i = 0; i < space.Area.Size.Width; i++)
        {
          if (Predicate(space[i, count]))
          {
            anyMatch = true;
            break;
          }
        }
        if (!anyMatch)
          return count;
        count++;
      }

      return KeepMatchingRow && count < space.Area.Size.Height ? count + 1 : count;
    }
  }
}
