namespace Unrect.Core
{
  public static class SpaceExtensions
  {
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space) => space.GetSubspace(new Offset(0, 0), space.Size);
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space, Offset offset) => space.GetSubspace(offset, new Size(space.Size.Width - offset.LeftOffset, space.Size.Height - offset.TopOffset));
    public static ISpace<TSpace> GetSubspace<TSpace>(this ISpace<TSpace> space, Size size) => space.GetSubspace(new Offset(0, 0), size);
  }
}
