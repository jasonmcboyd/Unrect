using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class MaxSizeStrategy : ISizeStrategy
  {
    public Size GetSize(ICellValues availableSpace) => availableSpace.Area.Size;
  }
}
