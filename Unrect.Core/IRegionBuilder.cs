namespace Unrect.Core
{
  public interface IRegionBuilder
  {
    IOffsetStrategy OffsetStrategy { get; }
    ISizeStrategy SizeStrategy { get; }
  }

  public interface IRegionBuilder<in TSpace, out TRegion> : IRegionBuilder
    where TRegion : IRegion<TSpace>
  {
    TRegion Build(ISpace<TSpace> space);
  }
}
