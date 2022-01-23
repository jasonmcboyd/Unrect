using System;
using Unrect.Core;

namespace Unrect
{
  internal abstract class SizeStrategyBase<TSpace, TSize> : ISizeStrategy<TSpace, TSize>
    where TSize : Core.Size
  {
    protected SizeStrategyBase(
      ISizeStrategy<TSpace, Core.Size> strategy,
      Func<Core.Size, TSize> selector)
    {
      Strategy = strategy;
      Selector = selector;
    }

    private ISizeStrategy<TSpace, Core.Size> Strategy { get; }
    private Func<Core.Size, TSize> Selector { get; }

    public TSize GetSize(ISpace<TSpace> availableSpace) => Selector(Strategy.GetSize(availableSpace));
  }
}
