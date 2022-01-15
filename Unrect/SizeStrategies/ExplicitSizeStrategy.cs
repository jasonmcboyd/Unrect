using Unrect.Core;

namespace Unrect.SizeStrategies
{
  public class ExplicitSizeStrategy : ISizeStrategy
  {
    public ExplicitSizeStrategy(uint width, uint height)
    {
      Width = width;
      Height = height;
    }

    private uint Width { get; }
    private uint Height { get; }

    public Size GetSize(Size size) => new Size(Width, Height);
  }
}
