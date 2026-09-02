namespace Unrect.Core
{
  public static class SpaceExtensions
  {
    public static ISpace GetSubspace(this ISpace space, Offset offset) => space.GetSubspace(offset, new Area(space.Area.Width - offset.Width, space.Area.Height - offset.Height));
    public static ISpace GetSubspace(this ISpace space, Area area) => space.GetSubspace(new Offset(0, 0), area);
  }
}
