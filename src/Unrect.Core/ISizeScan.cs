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
    /// coming. Every span for a scan that decided as it went; a whole-region scan decides here.
    /// A scan that was owed more than it was shown throws <see cref="OutOfBoundsException"/>.
    /// </summary>
    int Along(Plane<ISpace> region, int taken);

    /// <summary>Whether <paramref name="taken"/> spans satisfy the scan — false for an explicit extent that was owed more.</summary>
    bool Complete(int taken);

    /// <summary>What the strategy declared outright, for a message about an extent that does not fit; the default when the extent is discovered.</summary>
    Size Declared { get; }
  }
}
