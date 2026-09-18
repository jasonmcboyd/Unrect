using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The two lifts of a size: to the area a placement declares, and to the offset a placement skips.</summary>
  public static class SizeStrategyExtensions
  {
    /// <summary><paramref name="sizeStrategy"/> as the area a placement declares.</summary>
    public static IAreaStrategy ToAreaStrategy(this ISizeStrategy sizeStrategy) => new AreaStrategy(sizeStrategy);

    /// <summary><paramref name="sizeStrategy"/> as an offset: skip that many spans, start that far across.</summary>
    public static IOffsetStrategy ToOffsetStrategy(this ISizeStrategy sizeStrategy) => new OffsetStrategy(sizeStrategy);
  }
}
