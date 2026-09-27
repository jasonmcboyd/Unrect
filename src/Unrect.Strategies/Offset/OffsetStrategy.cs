using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A size read as an offset: the size's own scan, its block read as the gap before the region.
  /// The wrapper exists because an offset and a size are different words in the vocabulary, not
  /// different machines.
  /// </summary>
  internal sealed class OffsetStrategy<TSpace> : IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public OffsetStrategy(ISizeStrategy<TSpace> strategy)
    {
      Strategy = strategy;
    }

    internal ISizeStrategy<TSpace> Strategy { get; }

    public ISizeScan<TSpace> Begin(Orientation along) => Strategy.Begin(along);
  }
}
