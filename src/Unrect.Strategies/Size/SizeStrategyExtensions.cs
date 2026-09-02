using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Lifts a <see cref="ISizeStrategy"/> into the other two strategy interfaces — an area and an
  /// offset are both "a size", read as an extent or as a displacement.
  /// </summary>
  public static class SizeStrategyExtensions
  {
    /// <summary>Reads the size as a declared extent.</summary>
    public static IAreaStrategy ToAreaStrategy(this ISizeStrategy sizeStrategy)
      => new AreaStrategy(sizeStrategy);

    /// <summary>Reads the size as a displacement — how far to move, not how much to claim.</summary>
    public static IOffsetStrategy ToOffsetStrategy(this ISizeStrategy sizeStrategy)
      => new OffsetStrategy(sizeStrategy);
  }
}
