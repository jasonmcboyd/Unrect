namespace Unrect.Core
{
  /// <summary>
  /// The machine a line strategy builds: asked one line at a time — a row or a column, along the
  /// axis its strategy names — whether the line belongs to the region, over the lines shown so
  /// far. A scan is asked each line once, in order, so it may carry state from one to the next.
  /// </summary>
  /// <typeparam name="TSpace">The space this rule reads.</typeparam>
  public interface ILineScan<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>Whether line <paramref name="index"/> of <paramref name="region"/> — a row or a column, along the strategy's axis — belongs to it.</summary>
    bool Includes(Plane<TSpace> region, int index);

    /// <summary>The number of lines the scan is owed, for one that counts them; null for one that discovers them.</summary>
    int? Required { get; }
  }
}
