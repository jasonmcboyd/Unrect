using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class SelectSizeStrategy : ISizeStrategy
  {
    public SelectSizeStrategy(Func<ICellValues, Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    private Func<ICellValues, Size> AreaSelector { get; }

    public Size GetSize(ICellValues availableSpace) => AreaSelector(availableSpace);
  }
}
