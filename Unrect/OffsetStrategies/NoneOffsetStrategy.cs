using Unrect.Core;

namespace Unrect.OffsetStrategies
{
  public class NoneOffsetStrategy : IOffsetStrategy
  {
    public Offset GetOffset() => new Offset(0, 0);
  }
}
