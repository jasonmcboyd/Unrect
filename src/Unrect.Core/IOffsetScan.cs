namespace Unrect.Core
{
  /// <summary>
  /// The machine an offset strategy builds: shown the spans of a region one at a time along an
  /// axis, it says where the region a placement opens begins. A scan that is
  /// <see cref="Incremental"/> answers as the spans arrive; one that is not skips every span and
  /// answers over the whole region at <see cref="Settle"/>, which is what the engine holds a child
  /// for. Either way <see cref="Settle"/> is the answer when the spans ran out first.
  /// </summary>
  public interface IOffsetScan
  {
    /// <summary>Whether this scan can answer span by span. False means it needs the whole region, and the engine holds the child until it has it.</summary>
    bool Incremental { get; }

    /// <summary>
    /// The scan's answer for the newest span. <paramref name="region"/> is every span shown so
    /// far, the newest at <paramref name="index"/>, and <paramref name="across"/> is how far into
    /// that span the region starts when it starts.
    /// </summary>
    OffsetStep Next(Plane<ISpace> region, int index, out int across);

    /// <summary>
    /// The offset over the whole of <paramref name="region"/>, asked when every span was shown and
    /// none started — or, for a scan that is not incremental, the only question asked. Throws
    /// <see cref="OutOfBoundsException"/> when the region has no such place.
    /// </summary>
    Offset Settle(Plane<ISpace> region);
  }
}
