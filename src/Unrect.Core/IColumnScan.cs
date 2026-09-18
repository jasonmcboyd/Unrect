namespace Unrect.Core
{
  /// <summary>
  /// The machine a column strategy builds: asked one column at a time whether the column belongs
  /// to the region, over the columns shown so far. The mirror of <see cref="IRowScan"/>.
  /// </summary>
  public interface IColumnScan
  {
    /// <summary>Whether column <paramref name="column"/> of <paramref name="space"/> belongs to the region.</summary>
    bool IncludesColumn(Plane<ISpace> space, int column);

    /// <summary>The number of columns the scan is owed, for one that counts them; null for one that discovers them.</summary>
    int? Required { get; }
  }
}
