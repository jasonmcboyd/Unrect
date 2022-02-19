namespace Unrect.Core
{
  public struct Offset
  {
    public Offset(uint width, uint height)
    {
      Size = new Size(width, height);
    }

    public Offset(Size size)
    {
      Size = size;
    }

    public Size Size { get; }

    public static Offset operator +(Offset first, Offset second)
      => new Offset(first.Size + second.Size);
  }
}
