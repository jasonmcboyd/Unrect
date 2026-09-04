namespace Unrect.Core
{
  /// <summary>
  /// The size layer's <see cref="IIncrementalRowStrategy"/>: a size whose width is settled up front
  /// and whose height is a per-row rule. <see cref="ISizeStrategy.GetSize"/> is defined as the fold
  /// of <see cref="BeginSize"/>, so eager and incremental cannot disagree.
  /// </summary>
  public interface IIncrementalSizeStrategy : ISizeStrategy
  {
    /// <summary>
    /// A scan of <paramref name="availableSpace"/> positioned before row 0, its width already
    /// decided. Stateful strategies must return a fresh scan per call; stateless ones may return
    /// themselves.
    /// </summary>
    IAreaScan BeginSize(ISpace availableSpace);

    /// <inheritdoc />
    Size ISizeStrategy.GetSize(ISpace availableSpace)
    {
      var scan = BeginSize(availableSpace);

      return new Size(scan.Width, IRowScan.Fold(scan, availableSpace));
    }
  }
}
