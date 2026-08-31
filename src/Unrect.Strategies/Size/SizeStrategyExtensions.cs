using Unrect.Core;

namespace Unrect.Strategies
{
  public static class SizeStrategyExtensions
  {
    public static IAreaStrategy ToAreaStrategy(this ISizeStrategy sizeStrategy)
      => new AreaStrategy(sizeStrategy);

    public static IOffsetStrategy ToOffsetStrategy(this ISizeStrategy sizeStrategy)
      => new OffsetStrategy(sizeStrategy);
  }
}
