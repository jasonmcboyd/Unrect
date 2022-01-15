namespace Unrect.Core
{
  public struct Offset
  {
    public Offset(uint leftOffset, uint topOffset)
    {
      LeftOffset = leftOffset;
      TopOffset = topOffset;
    }

    public uint LeftOffset { get; }
    public uint TopOffset { get; }

    public static Offset operator +(Offset left, Offset right) => new Offset(left.LeftOffset + right.LeftOffset, left.TopOffset + right.TopOffset);
  }
}
