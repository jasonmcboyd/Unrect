namespace Unrect.Core
{
  /// <summary>
  /// A row strategy whose bound is genuinely a per-row rule, and so can be discovered one row at a
  /// time. <see cref="IRowStrategy.SelectRows"/> is defined here as the fold of
  /// <see cref="BeginRows"/> to exhaustion: an implementation states its rule once, and eager
  /// callers get the same answer by construction rather than by agreement.
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

    /// <inheritdoc />
    int IRowStrategy.SelectRows(ISpace space) => IRowScan.Fold(BeginRows(), space);
  }
}
