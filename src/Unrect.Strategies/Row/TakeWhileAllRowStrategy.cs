using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllRowStrategy : IRowStrategy
  {
    public TakeWhileAllRowStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public int SelectRows(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Height)
      {
        for (int i = 0; i < space.Area.Width; i++)
        {
          if (!Predicate(space[i, count]))
            return count;
        }
        count++;
      }

      return count;
    }
  }
}
