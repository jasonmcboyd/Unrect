using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class MaxSizeStrategy : ISizeStrategy
  {
    public Size GetSize(Plane<ISpace> availableSpace) => availableSpace.Area.Size;
  }
}
