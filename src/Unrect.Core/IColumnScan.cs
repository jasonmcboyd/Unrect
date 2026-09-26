namespace Unrect.Core
{
  /// <summary>
  /// The machine a column strategy builds: asked one column at a time whether the column belongs
  /// to the region, over the columns shown so far. A scan is asked each column once, in order, so
  /// it may carry state from one to the next. The mirror of <see cref="IRowScan"/>.
  /// </summary>
  public interface IColumnScan
  {
    /// <summary>Whether column <paramref name="column"/> of <paramref name="region"/> belongs to it.</summary>
    bool IncludesColumn(Plane<ISpace> region, int column);

    /// <summary>The number of columns the scan is owed, for one that counts them; null for one that discovers them.</summary>
    int? Required { get; }
  }
}
