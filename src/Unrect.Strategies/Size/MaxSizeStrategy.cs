using Unrect.Core;

namespace Unrect.Strategies
{
  internal class MaxSizeStrategy : ISizeStrategy
  {
    public Size GetSize(ISpace availableSpace) => availableSpace.Area.Size;
  }
}
