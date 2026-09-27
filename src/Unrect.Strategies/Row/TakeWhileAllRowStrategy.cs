using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllRowStrategy<TSpace> : ILineStrategy<TSpace>, ILineScan<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Vertical;

    public TakeWhileAllRowStrategy(Func<Point<TSpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Point<TSpace>, bool> Predicate { get; }

    // The rule carries nothing from row to row, so one instance is every scan of it.
    public ILineScan<TSpace> Begin() => this;

    public int? Required => null;

    public bool Includes(Plane<TSpace> space, int row)
    {
      for (int i = 0; i < space.Width; i++)
      {
        if (!Predicate(space[i, row]))
          return false;
      }

      return true;
    }
  }
}
