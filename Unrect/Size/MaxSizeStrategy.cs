using Unrect.Core;

namespace Unrect.Size
{
  public class MaxSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public Core.Size GetSize(ISpace<TSpace> availableSpace) => availableSpace.Area.Size;
  }
}
