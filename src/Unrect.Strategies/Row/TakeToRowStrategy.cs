using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeToRowStrategy<TSpace> : ILineStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Vertical;

    public TakeToRowStrategy(Func<Plane<TSpace>, int, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<Plane<TSpace>, int, bool> Predicate { get; }

    private bool KeepMatchingRow { get; }

    public ILineScan<TSpace> Begin() => new Scan(this);

    private sealed class Scan : ILineScan<TSpace>
    {
      public Scan(TakeToRowStrategy<TSpace> strategy)
      {
        Strategy = strategy;
      }

      private TakeToRowStrategy<TSpace> Strategy { get; }

      private bool Matched { get; set; }

      public int? Required => null;

      public bool Includes(Plane<TSpace> space, int row)
      {
        // Only reachable when the match was kept — an unkept match ends the extent by returning
        // false, and nothing is asked after that.
        if (Matched)
          return false;

        if (!Strategy.Predicate(space, row))
          return true;

        Matched = true;
        return Strategy.KeepMatchingRow;
      }
    }
  }
}
