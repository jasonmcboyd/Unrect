namespace Unrect.Core
{
  /// <summary>
  /// Where a region starts inside the space it is handed — declared as the machine that finds it.
  /// A strategy builds a fresh <see cref="IOffsetScan{TSpace}"/> per placement, along the axis the
  /// engine drives, and the scan is shown the spans one at a time. What a whole region answers is
  /// the fold of the scan: <see cref="Scans.GetOffset{TSpace}"/>. A rule is written over the space
  /// it may look at, so one that asks a cell's kind names a space that has one, and one that asks
  /// only the canonical questions is written over <see cref="ISpace"/> and used at that space.
  /// </summary>
  /// <typeparam name="TSpace">The space this rule reads.</typeparam>
  public interface IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>A fresh scan, to be shown spans along <paramref name="along"/>.</summary>
    IOffsetScan<TSpace> Begin(Orientation along);
  }
}
