namespace Unrect.Core
{
  public static class SpaceExtensions
  {
    public static ISpace GetSubspace(this ISpace space) => space.GetSubspace(new Offset(0, 0), space.Area);
    public static ISpace GetSubspace(this ISpace space, Offset offset) => space.GetSubspace(offset, new Area(space.Area.Size.Width - offset.Size.Width, space.Area.Size.Height - offset.Size.Height));
    public static ISpace GetSubspace(this ISpace space, Area area) => space.GetSubspace(new Offset(0, 0), area);
  }
}
