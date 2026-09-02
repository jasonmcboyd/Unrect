namespace Unrect.Core
{
  /// <summary>Where a subspace starts within its parent — a <see cref="Size"/> read as a displacement rather than an extent.</summary>
  public struct Offset
  {
    /// <summary>An offset of <paramref name="width"/> columns and <paramref name="height"/> rows.</summary>
    public Offset(int width, int height)
    {
      Size = new Size(width, height);
    }

    /// <summary>An offset the same magnitude as <paramref name="size"/>.</summary>
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

    /// <summary>Composes two displacements, e.g. how a <c>ShapeContext</c> accumulates <c>Advance</c> onto <c>Origin</c>.</summary>
    public static Offset operator +(Offset first, Offset second)
      => new Offset(first.Size + second.Size);
  }
}
