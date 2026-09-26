using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitRowCountStrategy<TSpace> : ILineStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Vertical;

    public ExplicitRowCountStrategy(int count)
    {
      if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

      Count = count;
    }

    internal int Count { get; }

    public ILineScan<TSpace> Begin() => new Scan(Count);

    /// <summary>Exactly this many rows, and owed all of them.</summary>
    private sealed class Scan : ILineScan<TSpace>
    {
      private readonly int _count;

      internal Scan(int count) => _count = count;

      public bool Includes(Plane<TSpace> space, int row) => row < _count;

      public int? Required => _count;
    }
  }
}
