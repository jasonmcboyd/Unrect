using Unrect.Core;
using Unrect.SizeStrategies;

namespace Unrect
{
  public static class SizeStrategy
  {
    public static ISizeStrategy Max() => new MaxSizeStrategy();
  }
}
