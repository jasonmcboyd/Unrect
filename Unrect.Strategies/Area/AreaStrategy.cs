using Unrect.Core;

namespace Unrect.Strategies
{
  internal class AreaStrategy<TSpace> : IAreaStrategy<TSpace>
  {
    public AreaStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy<TSpace> Strategy { get; }

    public Area GetArea(ISpace<TSpace> availableSpace) => new Area(Strategy.GetSize(availableSpace));
  }
}
