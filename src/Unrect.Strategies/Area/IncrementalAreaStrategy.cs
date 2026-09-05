using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// <see cref="AreaStrategy"/> for a size strategy that is incremental — the same lift, carrying
  /// the scan across so the area is discoverable one row at a time exactly when the size was.
  /// <see cref="IAreaStrategy.GetArea"/> is the same fold the wrapped strategy's own
  /// <see cref="ISizeStrategy.GetSize"/> is defined as, so the lift cannot change the answer.
  /// </summary>
  internal sealed class IncrementalAreaStrategy : IIncrementalAreaStrategy
  {
    public IncrementalAreaStrategy(IIncrementalSizeStrategy strategy)
    {
      Strategy = strategy;
    }

    private IIncrementalSizeStrategy Strategy { get; }

    public IAreaScan BeginArea(ISpace availableSpace) => Strategy.BeginSize(availableSpace);

    public Area GetArea(ISpace availableSpace) => Scans.FoldArea(BeginArea(availableSpace), availableSpace);
  }
}
