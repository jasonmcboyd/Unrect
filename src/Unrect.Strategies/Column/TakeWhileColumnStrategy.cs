using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy : ILineStrategy
  {
    public Orientation Along => Orientation.Horizontal;

    public TakeWhileColumnStrategy(Func<Plane<ISpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public ILineScan Begin() => new Scanning.ColumnPredicate(Predicate);
  }
}
