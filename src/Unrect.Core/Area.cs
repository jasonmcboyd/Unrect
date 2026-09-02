namespace Unrect.Core
{
  /// <summary>A rectangular extent — how wide and how tall a region is, with no position of its own.</summary>
  public struct Area
  {
    /// <summary>An area <paramref name="width"/> by <paramref name="height"/>.</summary>
    public Area(int width, int height)
    {
      Size = new Size(width, height);
    }

    /// <summary>An area the same extent as <paramref name="size"/>.</summary>
    public Area(Size size)
    {
      Size = size;
    }

    /// <summary>The area's extent as a <see cref="Size"/>.</summary>
    public Size Size { get; }

    /// <summary>The region's width — <c>Size.Width</c>, for reading without the hop.</summary>
    public int Width => Size.Width;

    /// <summary>The region's height — <c>Size.Height</c>, for reading without the hop.</summary>
    public int Height => Size.Height;
  }
}
