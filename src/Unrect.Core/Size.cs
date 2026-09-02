using System;

namespace Unrect.Core
{
  /// <summary>A width and a height, both non-negative. The building block <see cref="Area"/> and <see cref="Offset"/> both wrap.</summary>
  public struct Size
  {
    /// <summary>A size of <paramref name="width"/> by <paramref name="height"/>; either negative throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public Size(int width, int height)
    {
      if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
      if (height < 0) throw new ArgumentOutOfRangeException(nameof(height));

      Width = width;
      Height = height;
    }

    /// <summary>How wide.</summary>
    public int Width { get; }

    /// <summary>How tall.</summary>
    public int Height{ get; }

    /// <summary>Adds width to width and height to height.</summary>
    public static Size operator +(Size first, Size second)
      => new Size(first.Width + second.Width, first.Height + second.Height);
  }
}
