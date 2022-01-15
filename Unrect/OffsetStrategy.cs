using Unrect.Core;
using Unrect.OffsetStrategies;

namespace Unrect
{
  public static class OffsetStrategy
  {
    public static IOffsetStrategy None() => new NoneOffsetStrategy();
  }
}
