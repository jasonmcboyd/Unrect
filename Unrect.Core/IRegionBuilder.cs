using System.Collections.Generic;

namespace Unrect.Core
{
  public interface IRegionBuilder<in TSpace>
  {
    IOffsetStrategy OffsetStrategy { get; }
    ISizeStrategy SizeStrategy { get; }
    IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders();
  }

  public interface IRegionBuilder<in TSpace, out TRegion> : IRegionBuilder<TSpace>
    where TRegion : IRegion<TSpace>
  {
    TRegion Build(ISpace<TSpace> space);
  }
}
