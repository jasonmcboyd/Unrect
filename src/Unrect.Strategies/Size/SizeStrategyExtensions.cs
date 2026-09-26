using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The lift of a size to the offset a placement skips.</summary>
  public static class SizeStrategyExtensions
  {
    /// <summary><paramref name="sizeStrategy"/> as an offset: skip that many spans, start that far across.</summary>
    public static IOffsetStrategy<TSpace> ToOffsetStrategy<TSpace>(this ISizeStrategy<TSpace> sizeStrategy)
      where TSpace : class, ISpace
      => new OffsetStrategy<TSpace>(sizeStrategy);
  }
}
