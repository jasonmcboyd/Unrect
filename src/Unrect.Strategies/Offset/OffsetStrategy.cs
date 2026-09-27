using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A size read as an offset: skip while the size takes, start where it stops.</summary>
  internal sealed class OffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public OffsetStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    internal ISizeStrategy<TSpace> Strategy { get; }

    public IOffsetScan<TSpace> Begin(Orientation along) => new Scanning.SizeAsOffset<TSpace>(Strategy.Begin(along), along);
  }
}
