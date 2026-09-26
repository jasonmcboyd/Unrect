using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class TakeToColumnStrategy<TSpace> : ILineStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Horizontal;

    public TakeToColumnStrategy(Func<Plane<TSpace>, int, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<Plane<TSpace>, int, bool> Predicate { get; }

    public ILineScan<TSpace> Begin() => new Scan(Predicate);

    /// <summary>Inclusive: TakeColumnsTo means "up to and including the match".</summary>
    private sealed class Scan : ILineScan<TSpace>
    {
      private readonly Func<Plane<TSpace>, int, bool> _predicate;
      private bool _matched;

      internal Scan(Func<Plane<TSpace>, int, bool> predicate) => _predicate = predicate;

      public int? Required => null;

      public bool Includes(Plane<TSpace> space, int column)
      {
        if (_matched)
          return false;

        _matched = _predicate(space, column);
        return true;
      }
    }
  }
}
