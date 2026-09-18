using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>Arithmetic over spans: the region a run of consecutive spans covers, and the spans a region cuts into.</summary>
  internal static class Spans
  {
    /// <summary>The region <paramref name="count"/> consecutive spans cover, starting at <paramref name="first"/>, along <paramref name="along"/>.</summary>
    internal static Plane<TSpace> Region<TSpace>(Plane<TSpace> first, int count, Orientation along)
      where TSpace : class, ISpace
      => along == Orientation.Vertical
        ? new Plane<TSpace>(first.Space, first.Origin, new Area(first.Width, count))
        : new Plane<TSpace>(first.Space, first.Origin, new Area(count, first.Declared.Height));

    /// <summary>A region of no spans at <paramref name="anchor"/>'s origin and width.</summary>
    internal static Plane<TSpace> Empty<TSpace>(Plane<TSpace> anchor, Orientation along)
      where TSpace : class, ISpace
      => along == Orientation.Vertical
        ? new Plane<TSpace>(anchor.Space, anchor.Origin, new Area(anchor.Width, 0))
        : new Plane<TSpace>(anchor.Space, anchor.Origin, new Area(0, anchor.Declared.Height));

    /// <summary>
    /// A region of no spans <paramref name="distance"/> spans along from <paramref name="first"/>'s
    /// origin — where a child that is never fed would have begun. At the very end of the space the
    /// constructor still admits a zero-height plane; past it, the first span's own origin stands in.
    /// </summary>
    internal static Plane<TSpace> EmptyAt<TSpace>(Plane<TSpace> first, int distance, Orientation along)
      where TSpace : class, ISpace
    {
      try
      {
        return along == Orientation.Vertical
          ? new Plane<TSpace>(first.Space, first.Origin + new Offset(0, distance), new Area(first.Width, 0))
          : new Plane<TSpace>(first.Space, first.Origin + new Offset(distance, 0), new Area(0, first.Declared.Height));
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

      var area = region.Area;

      if (along == Orientation.Vertical)
      {
        for (var row = 0; row < area.Height; row++)
          yield return region.Slice(new Offset(0, row), new Area(area.Width, 1));
      }
      else
      {
        for (var column = 0; column < area.Width; column++)
          yield return region.Slice(new Offset(column, 0), new Area(1, area.Height));
      }
    }

    /// <summary>The span's length along <paramref name="along"/>.</summary>
    internal static int Along(Size size, Orientation along) => along == Orientation.Vertical ? size.Height : size.Width;

    internal static int Across(Size size, Orientation along) => along == Orientation.Vertical ? size.Width : size.Height;

    internal static Size ToSize(int along, int across, Orientation orientation)
      => orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);

    internal static Offset Step(int along, Orientation orientation)
      => orientation == Orientation.Vertical ? new Offset(0, along) : new Offset(along, 0);
  }
}
