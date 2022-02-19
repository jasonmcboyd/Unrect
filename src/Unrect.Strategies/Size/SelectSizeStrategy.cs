using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class SelectSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public SelectSizeStrategy(Func<ISpace<TSpace>, Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    public Func<ISpace<TSpace>, Size> AreaSelector { get; set; }

    public Size GetSize(ISpace<TSpace> availableSpace) => AreaSelector(availableSpace);
  }
}
