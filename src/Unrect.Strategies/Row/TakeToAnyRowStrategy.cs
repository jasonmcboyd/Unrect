using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class TakeToAnyRowStrategy : IRowStrategy
  {
    public TakeToAnyRowStrategy(Func<CellValue, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<CellValue, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public int SelectRows(ISpace space)
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
          return KeepMatchingRow ? count + 1 : count;
        count++;
      }

      return count;
    }
  }
}
