using Unrect.Core;

namespace Unrect.Strategies
{
  internal class ExplicitSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public ExplicitSizeStrategy(uint width, uint height)
    {
      Width = width;
      Height = height;
    }

    private uint Width { get; }
    private uint Height { get; }

    public Size GetSize(ISpace<TSpace> availableSpace) => new Size(Width, Height);
  }
}
