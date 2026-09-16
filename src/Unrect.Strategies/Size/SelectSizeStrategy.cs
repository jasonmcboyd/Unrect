using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class SelectSizeStrategy : ISizeStrategy
  {
    public SelectSizeStrategy(Func<Plane<ISpace>, Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    private Func<Plane<ISpace>, Size> AreaSelector { get; }

    public Size GetSize(Plane<ISpace> availableSpace) => AreaSelector(availableSpace);
  }
}
