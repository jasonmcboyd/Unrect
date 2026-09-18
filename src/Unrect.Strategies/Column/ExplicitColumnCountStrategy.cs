using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitColumnCountStrategy : IColumnStrategy
  {
    public ExplicitColumnCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    internal int Count { get; }

    public IColumnScan Begin() => new Scan(Count);

    /// <summary>Exactly this many columns, and owed all of them.</summary>
    private sealed class Scan : IColumnScan
    {
      private readonly int _count;

      internal Scan(int count) => _count = count;

      public bool IncludesColumn(Plane<ISpace> space, int column) => column < _count;

      public int? Required => _count;
    }
  }
}
