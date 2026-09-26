using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A size that is a function of the whole region: answered at the end, along either axis.</summary>
  internal sealed class SelectSizeStrategy<TSpace> : ISizeStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public SelectSizeStrategy(Func<Plane<TSpace>, Size> selector)
    {
      Selector = selector;
    }

    private Func<Plane<TSpace>, Size> Selector { get; }

    public ISizeScan<TSpace> Begin(Orientation along) => new Scanning.WholeSize<TSpace>(Selector, along);
  }
}
