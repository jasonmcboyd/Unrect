using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class ExplicitSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public ExplicitSizeStrategy(int width, int height)
    {
      if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

      Width = width;
      Height = height;
    }

    private int Width { get; }
    private int Height { get; }

    public Size GetSize(ISpace<TSpace> availableSpace) => new Size(Width, Height);
  }
}
