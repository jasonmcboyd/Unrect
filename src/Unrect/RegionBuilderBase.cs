using Unrect.Core;
using Unrect.Strategies;

using static Unrect.Strategies.SizeStrategies;

namespace Unrect
{
  public abstract class RegionBuilderBase<T1> : IRegionBuilder<T1>
    where T1 : IRegion
  {
    public IOffsetStrategy OffsetStrategy { get; init; } = MinSize().ToOffsetStrategy();
    public IAreaStrategy AreaStrategy { get; init; } = MaxSize().ToAreaStrategy();

    public abstract T1 Build(ISpace space);
  }
}
