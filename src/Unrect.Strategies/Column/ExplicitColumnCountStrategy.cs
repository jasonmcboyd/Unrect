using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitColumnCountStrategy : ILineStrategy
  {
    public Orientation Along => Orientation.Horizontal;

    public ExplicitColumnCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    internal int Count { get; }

    public ILineScan Begin() => new Scan(Count);

    /// <summary>Exactly this many columns, and owed all of them.</summary>
    private sealed class Scan : ILineScan
    {
      private readonly int _count;

      internal Scan(int count) => _count = count;

      public bool Includes(Plane<ISpace> space, int column) => column < _count;

      public int? Required => _count;
    }
  }
}
