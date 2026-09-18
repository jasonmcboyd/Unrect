namespace Unrect.Core
{
  /// <summary>
  /// The machine a row strategy builds: asked one row at a time whether the row belongs to the
  /// region, over the rows shown so far. A scan is asked each row once, in order, so it may carry
  /// state from one to the next.
  /// </summary>
  public interface IRowScan
  {
    /// <summary>Whether row <paramref name="row"/> of <paramref name="space"/> belongs to the region.</summary>
    bool IncludesRow(Plane<ISpace> space, int row);

    /// <summary>The number of rows the scan is owed, for one that counts them; null for one that discovers them.</summary>
    int? Required { get; }
  }
}
