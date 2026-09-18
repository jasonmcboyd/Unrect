using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>A size strategy under the name a placement declares it by: the same scan.</summary>
  internal sealed class AreaStrategy : IAreaStrategy
  {
    public AreaStrategy(ISizeStrategy strategy)
    {
      Strategy = strategy;
    }

    internal ISizeStrategy Strategy { get; }

    public ISizeScan Begin(Orientation along) => Strategy.Begin(along);
  }
}
