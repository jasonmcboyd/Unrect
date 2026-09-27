namespace Unrect.Core
{
  /// <summary>
  /// Where a region starts inside the space it is handed — declared as the machine that finds it.
  /// An offset strategy builds the same machine a size strategy does, a fresh
  /// <see cref="ISizeScan{TSpace}"/> per placement along the axis the engine drives; what its scan
  /// measures is the gap before the region — the spans skipped along the axis, and how far in
  /// across — and the region starts at that block's far corner. What a whole region answers is the
  /// fold of the scan: <see cref="Scans.GetOffset{TSpace}"/>. A rule is written over the space it
  /// may look at, so one that asks a cell's kind names a space that has one, and one that asks
  /// only the canonical questions is written over <see cref="ISpace"/> and used at that space.
  /// </summary>
  /// <typeparam name="TSpace">The space this rule reads.</typeparam>
  public interface IOffsetStrategy<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>A fresh scan of the gap before the region, to be shown spans along <paramref name="along"/>.</summary>
    ISizeScan<TSpace> Begin(Orientation along);
  }
}
