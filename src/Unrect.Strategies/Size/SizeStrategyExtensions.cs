using Unrect.Core;

namespace Unrect.Strategies
{
  public static class SizeStrategyExtensions
  {
    public static IAreaStrategy<TSpace> ToAreaStrategy<TSpace>(this ISizeStrategy<TSpace> sizeStrategy)
      => new AreaStrategy<TSpace>(sizeStrategy);

    public static IOffsetStrategy<TSpace> ToOffsetStrategy<TSpace>(this ISizeStrategy<TSpace> sizeStrategy)
      => new OffsetStrategy<TSpace>(sizeStrategy);
  }
}
