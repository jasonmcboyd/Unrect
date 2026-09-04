namespace Unrect.Core
{
  /// <summary>
  /// The per-row half of a row strategy, exposed so a bound can be discovered as a projection
  /// consumes it instead of measured before the projection starts.
  /// <para>
  /// A scan is one-shot and may carry state: <see cref="IncludesRow"/> is called with
  /// <c>row = 0, 1, 2, …</c> in order, never repeated and never skipped, and the first <c>false</c>
  /// ends the extent — no call is made after it. A scan is never told how much space is available,
  /// which is why a strategy that guarantees something about the available height cannot be one.
  /// </para>
  /// </summary>
  public interface IRowScan
  {
    /// <summary>
    /// Whether <paramref name="row"/> of <paramref name="space"/> lies inside the extent.
    /// <para>
    /// Every call must pass the same space the scan was begun over: the argument to
    /// <see cref="IIncrementalSizeStrategy.BeginSize"/> or
    /// <see cref="IIncrementalAreaStrategy.BeginArea"/>, or — for a scan from
    /// <see cref="IIncrementalRowStrategy.BeginRows"/>, which is told no space at all — whichever
    /// space the first call hands it. A scan may answer from state it recorded while deciding its
    /// width rather than consulting the space, so folding one over any other space is undefined
    /// rather than merely slower. This is the invariant the interleaved strategy's replay rests on.
    /// </para>
    /// </summary>
    bool IncludesRow(ISpace space, int row);

    /// <summary>
    /// The number of leading rows of <paramref name="space"/> that <paramref name="scan"/> includes,
    /// read to exhaustion. This is the definition every incremental strategy's eager answer is
    /// expressed as, so the eager and incremental readings of one strategy cannot disagree.
    /// </summary>
    static int Fold(IRowScan scan, ISpace space)
    {
      int count = 0;

      while (count < space.Area.Height && scan.IncludesRow(space, count))
        count++;

      return count;
    }
  }
}
