using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class AreaStrategy : IAreaStrategy
  {
    public AreaStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    internal ISizeStrategy Strategy { get; }

    public Area GetArea(Plane<ISpace> availableSpace) => new Area(Strategy.GetSize(availableSpace));
  }
}
