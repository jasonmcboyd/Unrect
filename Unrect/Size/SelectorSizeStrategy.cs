using System;
using Unrect.Core;

namespace Unrect.Size
{
  public class SelectorSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public SelectorSizeStrategy(Func<ISpace<TSpace>, Core.Size> areaSelector)
    {
      AreaSelector = areaSelector;
    }

    public Func<ISpace<TSpace>, Core.Size> AreaSelector { get; set; }

    public Core.Size GetSize(ISpace<TSpace> availableSpace) => AreaSelector(availableSpace);
  }
}
