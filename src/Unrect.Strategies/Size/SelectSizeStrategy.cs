using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class SelectSizeStrategy : ISizeStrategy
  {
    public SelectSizeStrategy(Func<ISpace, Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    private Func<ISpace, Size> AreaSelector { get; }

    public Size GetSize(ISpace availableSpace) => AreaSelector(availableSpace);
  }
}
