namespace Unrect.Core
{
  /// <summary>
  /// The size layer's <see cref="IIncrementalRowStrategy"/>: a size whose width is settled up front
  /// and whose height is a per-row rule. <see cref="ISizeStrategy.GetSize"/> is defined as
  /// <c>Scans.FoldSize(BeginSize(availableSpace), availableSpace)</c>, which is how an implementation
  /// is expected to spell it — see <see cref="IIncrementalRowStrategy"/> for why the definition is a
  /// convention rather than an inherited body, and what pins it.
  /// </summary>
  public interface IIncrementalSizeStrategy : ISizeStrategy
  {
    /// <summary>
    /// A scan of <paramref name="availableSpace"/> positioned before row 0, its width already
    /// decided. Stateful strategies must return a fresh scan per call; stateless ones may return
    /// themselves.
    /// </summary>
    IAreaScan BeginSize(ISpace availableSpace);
  }
}
