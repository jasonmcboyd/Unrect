using System;

namespace Unrect.Core
{
  public struct Size
  {
    public Size(int width, int height)
    {
      if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

      Width = width;
      Height = height;
    }

    public int Width { get; }
    public int Height{ get; }

    public static Size operator +(Size first, Size second)
      => new Size(first.Width + second.Width, first.Height + second.Height);
  }
}
