using Unrect.Core;

namespace Unrect.Strategies
{
  internal class OffsetStrategy : IOffsetStrategy
  {
    public OffsetStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy Strategy { get; }

    public Offset GetOffset(ISpace availableSpace) => new Offset(Strategy.GetSize(availableSpace));
  }
}
