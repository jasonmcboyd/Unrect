using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The column transpose of <see cref="TakeToRowStrategy"/>: scans right to the first matching
  /// column and takes the columns before it, optionally including the match itself.
  /// </summary>
  internal class TakeToColumnStrategy : IColumnStrategy
  {
    public TakeToColumnStrategy(Func<ISpace, int, bool> predicate, bool keepMatchingColumn)
    {
      Predicate = predicate;
      KeepMatchingColumn = keepMatchingColumn;
    }

    private Func<ISpace, int, bool> Predicate { get; }
    private bool KeepMatchingColumn { get; }

    public int SelectColumns(ISpace space)
    {
      int count = 0;

      while (count < space.Area.Size.Width && !Predicate(space, count))
        count++;

      return KeepMatchingColumn && count < space.Area.Size.Width ? count + 1 : count;
    }
  }
}
