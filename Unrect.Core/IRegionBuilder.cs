using System.Collections.Generic;

namespace Unrect.Core
{
  public interface IRegionBuilder<in TSpace>
  {
    IOffsetStrategy<TSpace> OffsetStrategy { get; }
    IAreaStrategy<TSpace> SizeStrategy { get; }
    IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders();
  }

  public interface IRegionBuilder<in TSpace, out TRegion> : IRegionBuilder<TSpace>
    where TRegion : IRegion<TSpace>
  {
    TRegion Build(ISpace<TSpace> space);
  }
}
