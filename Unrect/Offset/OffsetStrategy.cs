using Unrect.Core;

namespace Unrect.Offset
{
  internal class OffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
  {
    public OffsetStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    private ISizeStrategy<TSpace> Strategy { get; }

    public Core.Offset GetOffset(ISpace<TSpace> availableSpace) => new Core.Offset(Strategy.GetSize(availableSpace));
  }
}
