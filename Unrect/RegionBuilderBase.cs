using Unrect.Core;
using Unrect.Strategies;

using static Unrect.Strategies.SizeStrategies;

namespace Unrect
{
  public abstract class RegionBuilderBase<TSpace, T1> : IRegionBuilder<TSpace, T1>
    where T1 : IRegion<TSpace>
  {
    public IOffsetStrategy<TSpace> OffsetStrategy { get; init; } = MinSize<TSpace>().ToOffsetStrategy();
    public IAreaStrategy<TSpace> AreaStrategy { get; init; } = MaxSize<TSpace>().ToAreaStrategy();

    public abstract T1 Build(ISpace<TSpace> space);
  }
}
