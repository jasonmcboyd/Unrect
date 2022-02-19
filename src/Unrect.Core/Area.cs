namespace Unrect.Core
{
  public struct Area
  {
    public Area(uint width, uint height)
    {
      Size = new Size(width, height);
    }

    public Area(Size size)
    {
      Size = size;
    }

    public Size Size { get; }
  }
}
