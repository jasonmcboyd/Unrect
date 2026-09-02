using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class AreaStrategy : IAreaStrategy
  {
    public AreaStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy Strategy { get; }

    public Area GetArea(ISpace availableSpace) => new Area(Strategy.GetSize(availableSpace));
  }
}
