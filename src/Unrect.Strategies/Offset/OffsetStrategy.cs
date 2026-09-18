using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A size read as an offset: skip while the size takes, start where it stops.</summary>
  internal sealed class OffsetStrategy : IOffsetStrategy
  {
    public OffsetStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    internal ISizeStrategy Strategy { get; }

    public IOffsetScan Begin(Orientation along) => new Scanning.SizeAsOffset(Strategy.Begin(along), along);
  }
}
