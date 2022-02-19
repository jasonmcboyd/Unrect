using Unrect.Core;

namespace Unrect.Strategies
{
  internal class OffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
  {
    public OffsetStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy<TSpace> Strategy { get; }

    public Offset GetOffset(ISpace<TSpace> availableSpace) => new Offset(Strategy.GetSize(availableSpace));
  }
}
