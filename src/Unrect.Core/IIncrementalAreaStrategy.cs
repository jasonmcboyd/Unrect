namespace Unrect.Core
{
  /// <summary>
  /// The area layer's <see cref="IIncrementalRowStrategy"/>: a declared extent whose height a caller
  /// may discover as it consumes, rather than measure first. <see cref="IAreaStrategy.GetArea"/> is
  /// defined as <c>Scans.FoldArea(BeginArea(availableSpace), availableSpace)</c>, which is how an
  /// implementation is expected to spell it — see <see cref="IIncrementalRowStrategy"/> for why the
  /// definition is a convention rather than an inherited body, and what pins it.
  /// </summary>
  public interface IIncrementalAreaStrategy : IAreaStrategy
  {
    /// <summary>
    /// A scan of <paramref name="availableSpace"/> positioned before row 0, its width already
    /// decided. Stateful strategies must return a fresh scan per call; stateless ones may return
    /// themselves.
    /// </summary>
    IAreaScan BeginArea(ISpace availableSpace);
  }
}
