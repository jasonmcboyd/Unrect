using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ExplicitSizeStrategy : ISizeStrategy
  {
    public ExplicitSizeStrategy(int width, int height)
    {
      if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

      Width = width;
      Height = height;
    }

    internal int Width { get; }
    internal int Height { get; }

    public Size GetSize(Plane<ISpace> availableSpace) => new Size(Width, Height);
  }
}
