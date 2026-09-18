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
    /// <summary>Where <paramref name="strategy"/> starts a region inside <paramref name="space"/>, its scan shown the rows in order.</summary>
    /// <exception cref="OutOfBoundsException">The space has no such place.</exception>
    public static Offset GetOffset(this IOffsetStrategy strategy, Plane<ISpace> space)
      => FoldOffset(strategy.Begin(Orientation.Vertical), space, Orientation.Vertical);

    /// <summary>How big a region <paramref name="strategy"/> finds inside <paramref name="space"/>, its scan shown the rows in order.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more than the space holds.</exception>
    public static Size GetSize(this ISizeStrategy strategy, Plane<ISpace> space)
      => FoldSize(strategy.Begin(Orientation.Vertical), space, Orientation.Vertical);

    /// <summary>The area <paramref name="strategy"/> finds inside <paramref name="space"/>, its scan shown the rows in order.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more than the space holds.</exception>
    public static Area GetArea(this IAreaStrategy strategy, Plane<ISpace> space)
      => new Area(FoldSize(strategy.Begin(Orientation.Vertical), space, Orientation.Vertical));

    /// <summary>How many leading rows of <paramref name="space"/> <paramref name="strategy"/> takes.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more rows than the space holds.</exception>
    public static int SelectRows(this IRowStrategy strategy, Plane<ISpace> space) => Fold(strategy.Begin(), space);

    /// <summary>How many leading columns of <paramref name="space"/> <paramref name="strategy"/> takes.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more columns than the space holds.</exception>
    public static int SelectColumns(this IColumnStrategy strategy, Plane<ISpace> space) => FoldColumns(strategy.Begin(), space);

    /// <summary>The rows <paramref name="scan"/> includes, asked one at a time from the top until it says no or the rows run out.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more rows than there are.</exception>
    public static int Fold(IRowScan scan, Plane<ISpace> space)
    {
      var count = 0;

      while (space.HasRow(count) && scan.IncludesRow(space, count))
        count++;

      return scan.Required is int required && count < required ? throw new OutOfBoundsException() : count;
    }

    /// <summary>The columns <paramref name="scan"/> includes, asked one at a time from the left until it says no or the columns run out.</summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more columns than there are.</exception>
    public static int FoldColumns(IColumnScan scan, Plane<ISpace> space)
    {
      var count = 0;

      while (count < space.Width && scan.IncludesColumn(space, count))
        count++;

      return scan.Required is int required && count < required ? throw new OutOfBoundsException() : count;
    }

    /// <summary>
    /// The offset <paramref name="scan"/> settles on over <paramref name="region"/>: the spans
    /// along <paramref name="along"/> shown one at a time until it starts, and its
    /// <see cref="IOffsetScan.Settle"/> when it never does.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The region has no such place.</exception>
    public static Offset FoldOffset(IOffsetScan scan, Plane<ISpace> region, Orientation along)
    {
      var count = Along(region, along);

      for (var index = 0; index < count; index++)
      {
        var step = scan.Next(Prefix(region, index + 1, along), index, out var across);

        if (step == OffsetStep.StartHere)
          return At(index, across, along);

        if (step == OffsetStep.StartNext)
          return At(index + 1, across, along);
      }

      return scan.Settle(region);
    }

    /// <summary>
    /// The size <paramref name="scan"/> settles on over <paramref name="region"/>: the spans along
    /// <paramref name="along"/> shown one at a time until it refuses one or they run out, then what
    /// it keeps and how far across it reaches. The answer may exceed the region — an explicit
    /// extent says what it wants — and it is the caller's to compare.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The scan was owed more than the region holds.</exception>
    public static Size FoldSize(ISizeScan scan, Plane<ISpace> region, Orientation along)
    {
      var count = Along(region, along);
      var taken = 0;

      while (taken < count && scan.Take(Prefix(region, taken + 1, along), taken))
        taken++;

      var kept = Prefix(region, taken, along);
      var length = scan.Along(kept, taken);
      var across = scan.Across(kept, taken, final: true) ?? throw new OutOfBoundsException();

      return along == Orientation.Vertical ? new Size(across, length) : new Size(length, across);
    }

    private static int Along(Plane<ISpace> region, Orientation along)
      => along == Orientation.Vertical ? region.Area.Height : region.Width;

    private static Plane<ISpace> Prefix(Plane<ISpace> region, int spans, Orientation along)
      => along == Orientation.Vertical
        ? region.Slice(new Area(region.Width, spans))
        : region.Slice(new Area(spans, region.Area.Height));

    private static Offset At(int index, int across, Orientation along)
      => along == Orientation.Vertical ? new Offset(across, index) : new Offset(index, across);
  }
}
