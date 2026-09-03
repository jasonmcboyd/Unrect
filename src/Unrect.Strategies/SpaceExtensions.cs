using Unrect.Core;

namespace Unrect
{
  /// <summary>
  /// Convenience overloads of <see cref="ISpace.GetSubspace(Offset, Area)"/> for the common partial
  /// cases: a starting point with no area to declare, or an area from the space's own corner.
  /// <para>
  /// In the <c>Unrect</c> namespace beside <c>GridSpace</c>, because that is where it
  /// belongs to a reader, but compiled into <c>Unrect.Strategies</c>, because that is the lowest
  /// assembly every caller can see: the strategies use it and so does the shape layer above them.
  /// Both ship in the one <c>Unrect</c> package, so the split is invisible to anyone installing it.
  /// </para>
  /// </summary>
  public static class SpaceExtensions
  {
    /// <summary>
    /// Everything from <paramref name="offset"/> to the far edge of <paramref name="space"/> — no
    /// area to declare, just a starting point.
    /// </summary>
    /// <exception cref="OutOfBoundsException">
    /// <paramref name="offset"/> lies outside <paramref name="space"/>.
    /// </exception>
    public static ISpace GetSubspace(this ISpace space, Offset offset)
    {
      // Checked here rather than left to the subtraction below. Without it an oversized offset
      // produces a negative extent, and Area's own validation reports that as an
      // ArgumentOutOfRangeException — a different exception from the one the two-argument form
      // throws for the same mistake, and the wrong kind besides: running off the edge of a space is
      // a bounds condition a declaration may recover from, which is what OutOfBoundsException means.
      if (offset.Width > space.Area.Width || offset.Height > space.Area.Height)
        throw new OutOfBoundsException();

      return space.GetSubspace(offset, new Area(space.Area.Width - offset.Width, space.Area.Height - offset.Height));
    }

    /// <summary><paramref name="area"/>, from <paramref name="space"/>'s own top-left corner.</summary>
    /// <exception cref="OutOfBoundsException"><paramref name="area"/> does not fit <paramref name="space"/>.</exception>
    public static ISpace GetSubspace(this ISpace space, Area area) => space.GetSubspace(new Offset(0, 0), area);
  }
}
