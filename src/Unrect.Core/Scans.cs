namespace Unrect.Core
{
  /// <summary>
  /// The definitional folds: what an incremental strategy's eager reading <em>means</em>, written
  /// once so every implementation can say it in a line.
  /// <para>
  /// Each incremental interface pairs a scan with the eager method it refines, and the eager method
  /// is defined as the fold of the scan to exhaustion. An implementation is expected to spell that
  /// definition by delegating here rather than by writing a loop of its own — the eager and
  /// incremental readings of one strategy must not disagree, and one shared fold is what keeps them
  /// from being able to. See <see cref="IIncrementalRowStrategy"/> for why that is a convention an
  /// implementation follows rather than a body it inherits, and for what pins it.
  /// </para>
  /// </summary>
  public static class Scans
  {
    /// <summary>
    /// The number of leading rows of <paramref name="space"/> that <paramref name="scan"/> includes,
    /// read to exhaustion.
    /// </summary>
    public static int Fold(IRowScan scan, ISpace space)
    {
      int count = 0;

      while (count < space.Area.Height && scan.IncludesRow(space, count))
        count++;

      return count;
    }

    /// <summary>
    /// The size <paramref name="scan"/> denotes over <paramref name="space"/>: its settled width, and
    /// its rows folded to exhaustion.
    /// </summary>
    public static Size FoldSize(IAreaScan scan, ISpace space)
      => new Size(scan.Width, Fold(scan, space));

    /// <summary>
    /// The area <paramref name="scan"/> denotes over <paramref name="space"/> — <see cref="FoldSize"/>
    /// at the area layer, which is the same rectangle under the other name.
    /// </summary>
    public static Area FoldArea(IAreaScan scan, ISpace space)
      => new Area(FoldSize(scan, space));
  }
}
