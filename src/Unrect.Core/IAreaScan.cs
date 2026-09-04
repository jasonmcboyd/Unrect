namespace Unrect.Core
{
  /// <summary>
  /// A row scan that also carries the width of the extent it is scanning — everything an incremental
  /// size or area strategy needs to hand out a rectangle whose height is not yet known.
  /// <para>
  /// One type serves both layers: an <see cref="Area"/> is a <see cref="Size"/> under another name,
  /// so a scan that describes one describes the other.
  /// </para>
  /// </summary>
  public interface IAreaScan : IRowScan
  {
    /// <summary>
    /// The extent's width, fixed when the scan begins and never revised. Deciding it may consume
    /// leading rows through this same scan, but never rows the height alone would not have consumed.
    /// </summary>
    int Width { get; }
  }
}
