using Unrect.Core;

namespace Unrect.Size
{
  public class MaxSizeStrategy<TSpace> : ISizeStrategy<TSpace, Core.Size>
  {
    public Core.Size GetSize(ISpace<TSpace> availableSpace) => availableSpace.Area;
  }
}
