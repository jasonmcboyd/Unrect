namespace Unrect.Core
{
  /// <summary>
  /// A row strategy whose bound is genuinely a per-row rule, and so can be discovered one row at a
  /// time. <see cref="IRowStrategy.SelectRows"/> is <em>defined</em> as the fold of
  /// <see cref="BeginRows"/> to exhaustion — <c>Scans.Fold(BeginRows(), space)</c> — and an
  /// implementation is expected to spell it exactly that way, so that it states its rule once.
  /// <para>
  /// The definition is a convention an implementation follows, not a body it inherits: these
  /// libraries also target netstandard2.0, whose runtimes cannot dispatch a default interface member,
  /// so the fold lives on <see cref="Scans"/> and each implementation delegates to it in a line. What
  /// was once true by construction is therefore pinned by the fold-identity suite instead: for every
  /// factory in the vocabulary, <c>IncrementalStrategyTests</c> asserts that the eager answer equals a
  /// hand-written fold of the scan.
  /// </para>
  /// <para>
  /// A strategy that reads no cells — an explicit row count — must not implement this. Its
  /// <see cref="OutOfBoundsException"/> on overrun is a promise about the available height, and a
  /// scan is never told the available height.
  /// </para>
  /// </summary>
  public interface IIncrementalRowStrategy : IRowStrategy
  {
    /// <summary>
    /// A scan positioned before row 0. Stateful strategies must return a fresh scan per call;
    /// stateless ones may return themselves.
    /// </summary>
    IRowScan BeginRows();
  }
}
