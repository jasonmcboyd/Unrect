using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class MaxSizeStrategy : ISizeStrategy
  {
    public Size GetSize(ISpace availableSpace) => availableSpace.Area.Size;
  }
}
