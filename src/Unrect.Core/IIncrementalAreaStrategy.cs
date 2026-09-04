namespace Unrect.Core
{
  /// <summary>
  /// The area layer's <see cref="IIncrementalRowStrategy"/>: a declared extent whose height a caller
  /// may discover as it consumes, rather than measure first. <see cref="IAreaStrategy.GetArea"/> is
  /// defined as the fold of <see cref="BeginArea"/>, so eager and incremental cannot disagree.
  /// </summary>
  public interface IIncrementalAreaStrategy : IAreaStrategy
  {
    /// <summary>
    /// A scan of <paramref name="availableSpace"/> positioned before row 0, its width already
    /// decided. Stateful strategies must return a fresh scan per call; stateless ones may return
    /// themselves.
    /// </summary>
    IAreaScan BeginArea(ISpace availableSpace);

    /// <inheritdoc />
    Area IAreaStrategy.GetArea(ISpace availableSpace)
    {
      var scan = BeginArea(availableSpace);

      return new Area(scan.Width, IRowScan.Fold(scan, availableSpace));
    }
  }
}
