using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class OffsetStrategy : IOffsetStrategy
  {
    public OffsetStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy Strategy { get; }

    public Offset GetOffset(ICellValues availableSpace) => new Offset(Strategy.GetSize(availableSpace));
  }
}
