using Unrect.Area;
using Unrect.Offset;

namespace Unrect.Size
{
  internal static class SizeStrategyExtensions
  {
    internal static Core.IAreaStrategy<TSpace> ToAreaStrategy<TSpace>(this Core.ISizeStrategy<TSpace, Core.Size> sizeStrategy)
      => new AreaStrategy<TSpace>(sizeStrategy);

    internal static Core.IOffsetStrategy<TSpace> ToOffsetStrategy<TSpace>(this Core.ISizeStrategy<TSpace, Core.Size> sizeStrategy)
      => new OffsetStrategy<TSpace>(sizeStrategy);
  }
}
