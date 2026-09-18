using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeToRowStrategy : IRowStrategy
  {
    public TakeToRowStrategy(Func<Plane<ISpace>, int, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    private bool KeepMatchingRow { get; }

    public IRowScan Begin() => new Scan(this);

    private sealed class Scan : IRowScan
    {
      public Scan(TakeToRowStrategy strategy)
      {
        Strategy = strategy;
      }

      private TakeToRowStrategy Strategy { get; }

      private bool Matched { get; set; }

      public int? Required => null;

      public bool IncludesRow(Plane<ISpace> space, int row)
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
