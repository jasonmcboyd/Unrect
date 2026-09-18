namespace Unrect.Core
{
  /// <summary>
  /// Where a region starts inside the space it is handed — declared as the machine that finds it.
  /// A strategy builds a fresh <see cref="IOffsetScan"/> per placement, along the axis the engine
  /// drives, and the scan is shown the spans one at a time. What a whole region answers is the
  /// fold of the scan: <see cref="Scans.GetOffset"/>.
  /// </summary>
  public interface IOffsetStrategy
  {
    /// <summary>A fresh scan, to be shown spans along <paramref name="along"/>.</summary>
    IOffsetScan Begin(Orientation along);
  }
}
