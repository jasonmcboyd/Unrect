using System;

namespace Unrect.Core
{
  /// <summary>A width and a height, both non-negative: the extent of a space or a region, and what a size strategy answers.</summary>
  public readonly struct Size : IEquatable<Size>
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
    public int Height { get; }

    /// <summary>Adds width to width and height to height.</summary>
    public static Size operator +(Size first, Size second)
      => new Size(first.Width + second.Width, first.Height + second.Height);

    /// <summary>Whether <paramref name="other"/> is the same width and height.</summary>
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;

    /// <summary>Equality against any object — see <see cref="Equals(Size)"/> when the other value is a size.</summary>
    public override bool Equals(object? obj) => obj is Size other && Equals(other);

    /// <summary>Consistent with <see cref="Equals(Size)"/>.</summary>
    public override int GetHashCode() => Hashes.Combine(Width, Height);

    /// <summary>Same as <see cref="Equals(Size)"/>.</summary>
    public static bool operator ==(Size first, Size second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Size first, Size second) => !(first == second);

    /// <summary>
    /// The extent as <c>WxH</c>, width first — the same rendering <see cref="Plane{TSpace}"/> gives
    /// the region it names, so an extent reads the same wherever it is printed.
    /// </summary>
    public override string ToString() => $"{Width}x{Height}";
  }
}
