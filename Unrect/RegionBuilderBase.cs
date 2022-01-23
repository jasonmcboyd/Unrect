using System.Collections.Generic;
using Unrect.Core;
using Unrect.Size;
using static Unrect.Size.SizeStrategies;

namespace Unrect
{
  public abstract class RegionBuilderBase<TSpace, T1> : IRegionBuilder<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public IOffsetStrategy<TSpace> OffsetStrategy { get; init; } = MinSize<TSpace>().ToOffsetStrategy();
    public IAreaStrategy<TSpace> SizeStrategy { get; init; } = MaxSize<TSpace>().ToAreaStrategy();

    public abstract T1 Build(ISpace<TSpace> space);

    public abstract IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders();
  }
}
