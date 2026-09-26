namespace Unrect.Core
{
  /// <summary>
  /// The machine a size strategy builds: shown the spans of a region one at a time along an axis,
  /// it says which to take and, once it can, how wide the region is across the axis. A scan that
  /// is <see cref="Incremental"/> answers as the spans arrive; one that is not takes every span and
  /// answers over the whole region at the end, which is what the engine holds a child for.
  /// </summary>
  public interface ISizeScan
  {
    /// <summary>Whether this scan can answer span by span. False means it needs the whole region, and the engine holds the child until it has it.</summary>
    bool Incremental { get; }

    /// <summary>Whether the newest span of <paramref name="region"/>, the one after <paramref name="taken"/> already taken, belongs to the region.</summary>
    bool Take(Plane<ISpace> region, int taken);

    /// <summary>
    /// How far the region reaches across the axis, or null while that cannot be known.
    /// <paramref name="region"/> is the spans taken so far; <paramref name="final"/> says no more
    /// are coming, at which point a scan must answer.
    /// </summary>
    int? Across(Plane<ISpace> region, int taken, bool final);

    /// <summary>
    /// How many of the <paramref name="taken"/> spans the region keeps, asked once no more are
    /// coming. Every span for a scan that decided as it went; a whole-region scan decides here; a
    /// scan that is owed a count answers that count, shown enough or not, and the caller compares.
    /// </summary>
    int Along(Plane<ISpace> region, int taken);

    /// <summary>
    /// What the scan is owed, or null for one that discovers its extent: the spans it must be shown
    /// along its axis, and how far it reaches across, with zero on an axis it discovers. Fewer spans
    /// than that is the bounds condition a placement reports as an extent that does not fit.
    /// </summary>
    Size? Required { get; }
  }
}
