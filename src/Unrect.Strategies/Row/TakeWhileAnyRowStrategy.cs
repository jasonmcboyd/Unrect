using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAnyRowStrategy : IRowStrategy
  {
    public TakeWhileAnyRowStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public int SelectRows(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Height)
      {
        bool anyMatch = false;
        for (int i = 0; i < space.Area.Width; i++)
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

      return count;
    }
  }
}
