using Unrect.Core;

namespace Unrect.Size
{
  public class ExplicitSizeStrategy<TSpace> : ISizeStrategy<TSpace, Core.Size>
  {
    public ExplicitSizeStrategy(uint width, uint height)
    {
      Width = width;
      Height = height;
    }

    private uint Width { get; }
    private uint Height { get; }

    public Core.Size GetSize(ISpace<TSpace> availableSpace) => new Core.Area(Width, Height);
  }
}
