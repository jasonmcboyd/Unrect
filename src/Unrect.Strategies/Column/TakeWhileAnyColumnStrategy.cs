using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAnyColumnStrategy : IColumnStrategy
  {
    public TakeWhileAnyColumnStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public int SelectColumns(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Width)
      {
        bool anyMatch = false;
        for (int i = 0; i < space.Area.Height; i++)
        {
          if (Predicate(space[count, i]))
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
