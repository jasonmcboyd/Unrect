namespace Unrect.Core
{
  /// <summary>
  /// How big a region is — declared as the machine that discovers it. A strategy builds a fresh
  /// <see cref="ISizeScan"/> per placement, along the axis the engine drives, and the scan is
  /// shown the spans one at a time. What a whole region answers is the fold of the scan:
  /// <see cref="Scans.GetSize"/>.
  /// </summary>
  public interface ISizeStrategy
  {
    /// <summary>A fresh scan, to be shown spans along <paramref name="along"/>.</summary>
    ISizeScan Begin(Orientation along);
  }
}
