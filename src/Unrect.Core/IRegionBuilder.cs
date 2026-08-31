namespace Unrect.Core
{
  public interface IRegionBuilder
  {
    IOffsetStrategy OffsetStrategy { get; }
    IAreaStrategy AreaStrategy { get; }
  }

  public interface IRegionBuilder<out TRegion> : IRegionBuilder
    where TRegion : IRegion
  {
    TRegion Build(ISpace space);
  }
}
