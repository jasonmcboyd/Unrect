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
    public TakeToColumnStrategy(Func<Plane<ISpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public int SelectColumns(Plane<ISpace> space)
    {
      int count = 0;

      while (count < space.Width && !Predicate(space, count))
        count++;

      // Inclusive: TakeColumnsTo means "up to and including the match".
      return count < space.Width ? count + 1 : count;
    }
  }
}
