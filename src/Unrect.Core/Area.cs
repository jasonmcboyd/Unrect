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
  }
}
