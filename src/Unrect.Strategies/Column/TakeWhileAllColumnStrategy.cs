using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllColumnStrategy : IColumnStrategy
  {
    public TakeWhileAllColumnStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public int SelectColumns(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Width)
      {
        for (int i = 0; i < space.Area.Height; i++)
        {
          if (!Predicate(space[count, i]))
            return count;
        }
        count++;
      }

      return count;
    }
  }
}
