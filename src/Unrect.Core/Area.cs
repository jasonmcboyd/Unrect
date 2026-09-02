namespace Unrect.Core
{
  public struct Area
  {
    public Area(int width, int height)
    {
      Size = new Size(width, height);
    }

    public Area(Size size)
    {
      Size = size;
    }

    public Size Size { get; }

    /// <summary>The region's width — <c>Size.Width</c>, for reading without the hop.</summary>
    public int Width => Size.Width;

    /// <summary>The region's height — <c>Size.Height</c>, for reading without the hop.</summary>
    public int Height => Size.Height;
  }
}
