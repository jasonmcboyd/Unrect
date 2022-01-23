using Unrect.Core;

namespace Unrect
{
  internal abstract class SizeStrategyBase<TSpace> : ISizeStrategy<TSpace>
  {
    public SizeStrategyBase(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy<TSpace> Strategy { get; }

    public Core.Size GetSize(ISpace<TSpace> availableSpace) => Strategy.GetSize(availableSpace);
  }
}
