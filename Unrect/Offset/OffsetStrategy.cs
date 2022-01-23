using Unrect.Core;
using Unrect.Size;

namespace Unrect.Offset
{
  internal class OffsetStrategy<TSpace> : SizeStrategyBase<TSpace, Core.Offset>, IOffsetStrategy<TSpace>
  {
    public OffsetStrategy(ISizeStrategy<TSpace, Core.Size> strategy) : base(strategy, size => size.ToOffset())
    {
    }
  }
}
