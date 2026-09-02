namespace Unrect.Core
{
  public struct Offset
  {
    public Offset(int width, int height)
    {
      Size = new Size(width, height);
    }

    public Offset(Size size)
    {
      Size = size;
    }

    public Size Size { get; }

    /// <summary>The offset's width — <c>Size.Width</c>, for reading without the hop.</summary>
    public int Width => Size.Width;

    /// <summary>The offset's height — <c>Size.Height</c>, for reading without the hop.</summary>
    public int Height => Size.Height;

    public static Offset operator +(Offset first, Offset second)
      => new Offset(first.Size + second.Size);
  }
}
