using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy : IColumnStrategy
  {
    public TakeWhileColumnStrategy(Func<Plane<ISpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public IColumnScan Begin() => new Scanning.ColumnPredicate(Predicate);
  }
}
