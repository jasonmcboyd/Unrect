namespace Unrect.Core
{
  public static class SpaceExtensions
  {
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space) => space.GetSubspace(new Offset(0, 0), space.Area);
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space, Offset offset) => space.GetSubspace(offset, new Area(space.Area.Size.Width - offset.Size.Width, space.Area.Size.Height - offset.Size.Height));
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space, Area area) => space.GetSubspace(new Offset(0, 0), area);
  }
}
