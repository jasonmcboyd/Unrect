using Unrect.Core;
using Unrect.Size;

namespace Unrect.Area
{
  internal class AreaStrategy<TSpace> : SizeStrategyBase<TSpace, Core.Area>, IAreaStrategy<TSpace>
  {
    public AreaStrategy(ISizeStrategy<TSpace, Core.Size> strategy) : base(strategy, size => size.ToArea())
    {
    }
  }
}
