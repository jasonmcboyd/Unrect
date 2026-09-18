using System.Collections.Generic;

namespace Unrect.Core
{
  /// <summary>
  /// Arithmetic over spans, written once for every scan and machine that reads a region one span
  /// at a time: how far a region runs along an axis and reaches across it, the region a run of
  /// spans covers, the spans a region cuts into, and the size or offset a pair of along-and-across
  /// numbers means under an orientation. Internal, and exempt from Core's charter the way
  /// <see cref="Hashes"/> is: nothing here is a contract, only what the contracts' implementers
  /// would each write for themselves.
  /// </summary>
  internal static class Spans
  {
    /// <summary>How many spans <paramref name="size"/> has along <paramref name="along"/>.</summary>
    internal static int Along(Size size, Orientation along) => along == Orientation.Vertical ? size.Height : size.Width;

    /// <summary>How far <paramref name="size"/> reaches across <paramref name="along"/>.</summary>
    internal static int Across(Size size, Orientation along) => along == Orientation.Vertical ? size.Width : size.Height;

    /// <summary>How many spans <paramref name="region"/> has along <paramref name="along"/>.</summary>
    internal static int Along<TSpace>(Plane<TSpace> region, Orientation along)
      where TSpace : class, ISpace
      => Along(region.Area.Size, along);

    /// <summary>How far <paramref name="region"/> reaches across <paramref name="along"/>.</summary>
    internal static int Across<TSpace>(Plane<TSpace> region, Orientation along)
      where TSpace : class, ISpace
      => Across(region.Area.Size, along);

    /// <summary>The size <paramref name="along"/> spans long and <paramref name="across"/> wide means under <paramref name="orientation"/>.</summary>
    internal static Size ToSize(int along, int across, Orientation orientation)
      => orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);

    /// <summary>The offset <paramref name="along"/> spans in and <paramref name="across"/> cells over means under <paramref name="orientation"/>.</summary>
    internal static Offset ToOffset(int along, int across, Orientation orientation)
      => orientation == Orientation.Vertical ? new Offset(across, along) : new Offset(along, across);

    /// <summary>An offset of <paramref name="along"/> spans and nothing across.</summary>
    internal static Offset Step(int along, Orientation orientation) => ToOffset(along, 0, orientation);

    /// <summary>The cell of span <paramref name="span"/> at <paramref name="across"/> into it.</summary>
    internal static Point<TSpace> Cell<TSpace>(Plane<TSpace> region, int span, int across, Orientation along)
      where TSpace : class, ISpace
      => along == Orientation.Vertical ? region[across, span] : region[span, across];

    /// <summary>The leading <paramref name="spans"/> spans of <paramref name="region"/> along <paramref name="along"/>.</summary>
    /// <exception cref="OutOfBoundsException">The region has fewer spans than that.</exception>
    internal static Plane<TSpace> Prefix<TSpace>(Plane<TSpace> region, int spans, Orientation along)
      where TSpace : class, ISpace
      => region.Slice(new Area(ToSize(spans, Across(region, along), along)));

    /// <summary>The region <paramref name="count"/> consecutive spans cover, starting at <paramref name="first"/>, along <paramref name="along"/>.</summary>
    internal static Plane<TSpace> Region<TSpace>(Plane<TSpace> first, int count, Orientation along)
      where TSpace : class, ISpace
      => new Plane<TSpace>(first.Space, first.Origin, new Area(ToSize(count, Across(first, along), along)));

    /// <summary>A region of no spans at <paramref name="anchor"/>'s origin, as wide across as it is.</summary>
    internal static Plane<TSpace> Empty<TSpace>(Plane<TSpace> anchor, Orientation along)
      where TSpace : class, ISpace
      => new Plane<TSpace>(anchor.Space, anchor.Origin, new Area(ToSize(0, Across(anchor, along), along)));

    /// <summary>
    /// A region of no spans <paramref name="distance"/> spans along from <paramref name="first"/>'s
    /// origin — where a child that is never fed would have begun. At the very end of the space the
    /// constructor still admits an empty plane; past it, the first span's own origin stands in.
    /// </summary>
    internal static Plane<TSpace> EmptyAt<TSpace>(Plane<TSpace> first, int distance, Orientation along)
      where TSpace : class, ISpace
    {
      try
      {
        return new Plane<TSpace>(first.Space, first.Origin + Step(distance, along), new Area(ToSize(0, Across(first, along), along)));
      }
      catch (OutOfBoundsException)
      {
        return Empty(first, along);
      }
    }

    /// <summary>The spans <paramref name="region"/> cuts into along <paramref name="along"/>; the whole region as one span when null.</summary>
    internal static IEnumerable<Plane<TSpace>> Of<TSpace>(Plane<TSpace> region, Orientation? along)
      where TSpace : class, ISpace
    {
      if (along is null)
      {
        yield return region;
        yield break;
      }

      var count = Along(region, along.Value);

      for (var span = 0; span < count; span++)
        yield return region.Slice(Step(span, along.Value), new Area(ToSize(1, Across(region, along.Value), along.Value)));
    }
  }
}
