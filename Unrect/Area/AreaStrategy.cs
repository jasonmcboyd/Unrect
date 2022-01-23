using Unrect.Core;

namespace Unrect.Area
{
  internal class AreaStrategy<TSpace> : IAreaStrategy<TSpace>
  {
    public AreaStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy<TSpace> Strategy { get; }

    public Core.Area GetArea(ISpace<TSpace> availableSpace) => new Core.Area(Strategy.GetSize(availableSpace));
  }
}
