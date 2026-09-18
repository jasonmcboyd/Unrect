using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitRowCountStrategy : IRowStrategy
  {
    public ExplicitRowCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    internal int Count { get; }

    public IRowScan Begin() => new Scan(Count);

    /// <summary>Exactly this many rows, and owed all of them.</summary>
    private sealed class Scan : IRowScan
    {
      private readonly int _count;

      internal Scan(int count) => _count = count;

      public bool IncludesRow(Plane<ISpace> space, int row) => row < _count;

      public int? Required => _count;
    }
  }
}
