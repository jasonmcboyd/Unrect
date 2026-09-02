using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The column transpose of <see cref="TakeToRowStrategy"/>: scans right to the first matching
  /// column and takes the columns before it, optionally including the match itself.
  /// </summary>
  internal sealed class TakeToColumnStrategy : IColumnStrategy
  {
    public TakeToColumnStrategy(Func<ISpace, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<ISpace, int, bool> Predicate { get; }

    public int SelectColumns(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Width && !Predicate(space, count))
        count++;

      // Inclusive: TakeColumnsTo means "up to and including the match".
      return count < space.Area.Width ? count + 1 : count;
    }
  }
}
