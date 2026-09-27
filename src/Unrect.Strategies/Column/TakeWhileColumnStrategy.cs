using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileColumnStrategy<TSpace> : ILineStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Horizontal;

    public TakeWhileColumnStrategy(Func<Plane<TSpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<TSpace>, int, bool> Predicate { get; }

    public ILineScan<TSpace> Begin() => new Scanning.ColumnPredicate<TSpace>(Predicate);
  }
}
