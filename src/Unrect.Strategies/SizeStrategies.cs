using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  public static class SizeStrategies
  {
    public static ISizeStrategy MaxSize()
      => new MaxSizeStrategy();

    public static ISizeStrategy MinSize()
      => new ExplicitSizeStrategy(0, 0);

    public static ISizeStrategy ExplicitSize(int width, int height)
      => new ExplicitSizeStrategy(width, height);

    public static ISizeStrategy RowsWhileAny(Func<CellValue, bool> predicate)
      => new RowsWhileAnySizeStrategy(predicate);

    public static ISizeStrategy RowsWhileAnyValue()
      => RowsWhileAny(v => v.HasValue);

    public static ISizeStrategy SelectSize(Func<ISpace, Size> selector)
      => new SelectSizeStrategy(selector);
  }
}
