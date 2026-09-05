using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeToRowStrategy : IIncrementalRowStrategy
  {
    public TakeToRowStrategy(Func<ISpace, int, bool> predicate, bool keepMatchingRow)
    {
      Predicate = predicate;
      KeepMatchingRow = keepMatchingRow;
    }

    private Func<ISpace, int, bool> Predicate { get; }
    private bool KeepMatchingRow { get; }

    public IRowScan BeginRows() => new Scan(this);

    public int SelectRows(ISpace space) => Scans.Fold(BeginRows(), space);

    /// <summary>
    /// The one stateful scan in the family, and the reason <see cref="IRowScan"/> is an object
    /// rather than a function: when the matching row is kept it is included and the extent ends, so
    /// the scan has to remember that the match is behind it.
    /// </summary>
    private sealed class Scan : IRowScan
    {
      public Scan(TakeToRowStrategy strategy)
      {
        Strategy = strategy;
      }

      private TakeToRowStrategy Strategy { get; }
      private bool Matched { get; set; }

      public bool IncludesRow(ISpace space, int row)
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
