using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A size that is a function of the whole region: answered at the end, along either axis.</summary>
  internal sealed class SelectSizeStrategy : ISizeStrategy
  {
    public SelectSizeStrategy(Func<Plane<ISpace>, Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    private Func<Plane<ISpace>, Size> AreaSelector { get; }

    public ISizeScan Begin(Orientation along) => new Scanning.WholeSize(AreaSelector, along);
  }
}
