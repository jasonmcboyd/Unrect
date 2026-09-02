namespace Unrect.Core
{
  /// <summary>Convenience overloads of <see cref="ISpace.GetSubspace(Offset, Area)"/> for the common partial cases.</summary>
  public static class SpaceExtensions
  {
    /// <summary>Everything from <paramref name="offset"/> to the far edge of <paramref name="space"/> — no area to declare, just a starting point.</summary>
    public static ISpace GetSubspace(this ISpace space, Offset offset) => space.GetSubspace(offset, new Area(space.Area.Width - offset.Width, space.Area.Height - offset.Height));

    /// <summary><paramref name="area"/>, from <paramref name="space"/>'s own top-left corner.</summary>
    public static ISpace GetSubspace(this ISpace space, Area area) => space.GetSubspace(new Offset(0, 0), area);
  }
}
