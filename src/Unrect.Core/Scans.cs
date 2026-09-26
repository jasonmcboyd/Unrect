namespace Unrect.Core
{
  /// <summary>
  /// What a whole region answers, defined as the fold of a strategy's scan over it: the region's
  /// spans shown one at a time, in order, and the scan's settlement read at the end. The one
  /// definition of a strategy's whole-region answer, so a scan that decides as it goes and one
  /// that decides at the end cannot disagree about what a region means.
  /// </summary>
  public static class Scans
  {
    /// <summary>Where <paramref name="strategy"/> starts a region inside <paramref name="region"/>, its scan shown the rows in order.</summary>
    /// <exception cref="OutOfBoundsException">The space has no such place.</exception>
    public static Offset GetOffset<TSpace>(this IOffsetStrategy<TSpace> strategy, Plane<TSpace> region)
      where TSpace : class, ISpace
      => FoldOffset(strategy.Begin(Orientation.Vertical), region, Orientation.Vertical);

    /// <summary>How big a region <paramref name="strategy"/> finds inside <paramref name="region"/>, its scan shown the rows in order.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more than the space holds.</exception>
    public static Size GetSize<TSpace>(this ISizeStrategy<TSpace> strategy, Plane<TSpace> region)
      where TSpace : class, ISpace
      => FoldSize(strategy.Begin(Orientation.Vertical), region, Orientation.Vertical);

    /// <summary>How many leading lines of <paramref name="region"/> <paramref name="strategy"/> takes, along the axis it names.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more lines than the region holds.</exception>
    public static int SelectLines<TSpace>(this ILineStrategy<TSpace> strategy, Plane<TSpace> region)
      where TSpace : class, ISpace => FoldLines(strategy.Begin(), region, strategy.Along);

    /// <summary>
    /// The lines <paramref name="scan"/> includes along <paramref name="along"/>, asked one at a
    /// time from the first until it says no or the lines run out.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more lines than there are.</exception>
    public static int FoldLines<TSpace>(ILineScan<TSpace> scan, Plane<TSpace> region, Orientation along)
      where TSpace : class, ISpace
    {
      var count = 0;
      var lines = Spans.Along(region, along);

      while (count < lines && scan.Includes(region, count))
        count++;

      return scan.Required is int required && count < required ? throw new OutOfBoundsException() : count;
    }

    /// <summary>
    /// The offset <paramref name="scan"/> settles on over <paramref name="region"/>: the spans
    /// along <paramref name="along"/> shown one at a time until it starts, and its
    /// <see cref="IOffsetScan{TSpace}.Settle"/> when it never does.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The region has no such place.</exception>
    public static Offset FoldOffset<TSpace>(IOffsetScan<TSpace> scan, Plane<TSpace> region, Orientation along)
      where TSpace : class, ISpace
    {
      var count = Spans.Along(region, along);

      for (var index = 0; index < count; index++)
      {
        var step = scan.Next(Spans.Prefix(region, index + 1, along), index, out var across);

        if (step == OffsetStep.StartHere)
          return Spans.ToOffset(index, across, along);

        if (step == OffsetStep.StartNext)
          return Spans.ToOffset(index + 1, across, along);
      }

      return scan.Settle(region);
    }

    /// <summary>
    /// The size <paramref name="scan"/> settles on over <paramref name="region"/>: the spans along
    /// <paramref name="along"/> shown one at a time until it refuses one or they run out, then what
    /// it keeps and how far across it reaches. The answer may exceed the region — a scan that is
    /// owed an extent (<see cref="ISizeScan{TSpace}.Required"/>) answers what it is owed, shown enough or
    /// not — and it is the caller's to compare with the region.
    /// </summary>
    /// <exception cref="ScanContractException">The scan broke its contract: asked at the end, it did not say how far across it reaches.</exception>
    public static Size FoldSize<TSpace>(ISizeScan<TSpace> scan, Plane<TSpace> region, Orientation along)
      where TSpace : class, ISpace
    {
      var count = Spans.Along(region, along);
      var taken = 0;

      while (taken < count && scan.Take(Spans.Prefix(region, taken + 1, along), taken))
        taken++;

      var kept = Spans.Prefix(region, taken, along);
      var length = scan.Along(kept, taken);
      var across = scan.Across(kept, taken, final: true) ?? throw NoWidthAtTheEnd();

      return Spans.ToSize(length, across, along);
    }

    /// <summary>The one breach a size scan can commit: asked at the end, it did not say how far across it reaches.</summary>
    internal static ScanContractException NoWidthAtTheEnd()
      => new ScanContractException("a size scan must say how far across it reaches once the spans have run out");
  }
}
