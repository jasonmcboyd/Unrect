using Unrect.Core;

namespace Unrect.OffsetStrategies
{
  public class ExplicitOffsetStrategy : IOffsetStrategy
  {
    public ExplicitOffsetStrategy(uint leftOffset, uint topOffset)
    {
      LeftOffset = leftOffset;
      TopOffset = topOffset;
    }

    public uint LeftOffset { get; }
    public uint TopOffset { get; }

    public Offset GetOffset() => new Offset(LeftOffset, TopOffset);
  }
}
