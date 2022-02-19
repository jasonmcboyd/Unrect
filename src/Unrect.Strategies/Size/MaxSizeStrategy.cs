using Unrect.Core;

namespace Unrect.Strategies
{
  internal class MaxSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public Size GetSize(ISpace<TSpace> availableSpace) => availableSpace.Area.Size;
  }
}
