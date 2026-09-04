using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Lifts a <see cref="ISizeStrategy"/> into the other two strategy interfaces — an area and an
  /// offset are both "a size", read as an extent or as a displacement.
  /// </summary>
  public static class SizeStrategyExtensions
  {
    /// <summary>
    /// Reads the size as a declared extent. Incrementality carries across: the area can be
    /// discovered a row at a time exactly when the size could, and is measured up front otherwise.
    /// </summary>
    public static IAreaStrategy ToAreaStrategy(this ISizeStrategy sizeStrategy)
      => sizeStrategy is IIncrementalSizeStrategy incremental
        ? new IncrementalAreaStrategy(incremental)
        : new AreaStrategy(sizeStrategy);

    /// <summary>Reads the size as a displacement — how far to move, not how much to claim.</summary>
    public static IOffsetStrategy ToOffsetStrategy(this ISizeStrategy sizeStrategy)
      => new OffsetStrategy(sizeStrategy);
  }
}
