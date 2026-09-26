using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeWhileAllRowStrategy : ILineStrategy, ILineScan
  {
    public Orientation Along => Orientation.Vertical;

    public TakeWhileAllRowStrategy(Func<Point<ISpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Point<ISpace>, bool> Predicate { get; }

    // The rule carries nothing from row to row, so one instance is every scan of it.
    public ILineScan Begin() => this;

    public int? Required => null;

    public bool Includes(Plane<ISpace> space, int row)
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
