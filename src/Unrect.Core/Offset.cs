using System;

namespace Unrect.Core
{
  /// <summary>Where a region starts within the one it was cut from — a <see cref="Size"/> read as a displacement rather than an extent.</summary>
  public readonly struct Offset : IEquatable<Offset>
  {
    /// <summary>An offset of <paramref name="width"/> columns and <paramref name="height"/> rows.</summary>
    public Offset(int width, int height)
    {
      Size = new Size(width, height);
    }

    /// <summary>An offset of <paramref name="size"/>'s width and height.</summary>
    public Offset(Size size)
    {
      Size = size;
    }

    /// <summary>The offset, as a <see cref="Size"/>.</summary>
    public Size Size { get; }

    /// <summary>The offset's width — <c>Size.Width</c>, for reading without the hop.</summary>
    public int Width => Size.Width;

    /// <summary>The offset's height — <c>Size.Height</c>, for reading without the hop.</summary>
    public int Height => Size.Height;

    /// <summary>Composes two displacements — a slice's origin onto its parent's.</summary>
    public static Offset operator +(Offset first, Offset second)
      => new Offset(first.Size + second.Size);

    /// <summary>Whether <paramref name="other"/> is the same displacement.</summary>
    public bool Equals(Offset other) => Size == other.Size;

    /// <summary>Equality against any object — see <see cref="Equals(Offset)"/> when the other value is an offset.</summary>
    public override bool Equals(object? obj) => obj is Offset other && Equals(other);

    /// <summary>Consistent with <see cref="Equals(Offset)"/>.</summary>
    public override int GetHashCode() => Size.GetHashCode();

    /// <summary>Same as <see cref="Equals(Offset)"/>.</summary>
    public static bool operator ==(Offset first, Offset second) => first.Equals(second);

    /// <summary>The negation of the equality operator above.</summary>
    public static bool operator !=(Offset first, Offset second) => !(first == second);

    /// <summary>The displacement as <c>(column,row)</c> — the same rendering <see cref="Plane{TSpace}"/> gives its origin.</summary>
    public override string ToString() => $"({Width},{Height})";
  }
}
