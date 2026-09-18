namespace Unrect.Core
{
  /// <summary>
  /// Which leading columns of a region belong to it — declared as the machine that decides, one
  /// column at a time. What a whole region answers is the fold of the scan:
  /// <see cref="Scans.SelectColumns"/>.
  /// </summary>
  public interface IColumnStrategy
  {
    /// <summary>A fresh scan, to be asked column by column.</summary>
    IColumnScan Begin();
  }
}
