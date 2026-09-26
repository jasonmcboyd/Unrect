using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeToColumnStrategy : ILineStrategy
  {
    public Orientation Along => Orientation.Horizontal;

    public TakeToColumnStrategy(Func<Plane<ISpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<ISpace>, int, bool> Predicate { get; }

    public ILineScan Begin() => new Scan(Predicate);

    /// <summary>Inclusive: TakeColumnsTo means "up to and including the match".</summary>
    private sealed class Scan : ILineScan
    {
      private readonly Func<Plane<ISpace>, int, bool> _predicate;
      private bool _matched;

      internal Scan(Func<Plane<ISpace>, int, bool> predicate) => _predicate = predicate;

      public int? Required => null;

      public bool Includes(Plane<ISpace> space, int column)
      {
        if (_matched)
          return false;

        _matched = _predicate(space, column);
        return true;
      }
    }
  }
}
