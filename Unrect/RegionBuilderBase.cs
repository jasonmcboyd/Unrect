using System.Collections.Generic;
using Unrect.Core;
using static Unrect.OffsetStrategy;
using static Unrect.SizeStrategy;

namespace Unrect
{
  public abstract class RegionBuilderBase<TSpace, T1> : IRegionBuilder<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public IOffsetStrategy OffsetStrategy { get; init; } = None();
    public ISizeStrategy SizeStrategy { get; init; } = Max();

    public abstract T1 Build(ISpace<TSpace> space);

    public abstract IEnumerable<IRegionBuilder<TSpace>> GetSubregionBuilders();
  }
}
