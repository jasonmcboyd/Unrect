namespace Unrect.Core
{
  /// <summary>
  /// Which leading rows of a region belong to it — declared as the machine that decides, one row
  /// at a time. What a whole region answers is the fold of the scan: <see cref="Scans.SelectRows"/>.
  /// </summary>
  public interface IRowStrategy
  {
    /// <summary>A fresh scan, to be asked row by row.</summary>
    IRowScan Begin();
  }
}
