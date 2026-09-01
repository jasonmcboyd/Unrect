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

    /// <summary>
    /// Full available height, and as many leading columns as have at least one cell satisfying
    /// <paramref name="predicate"/> — the transpose of <see cref="RowsWhileAny"/>.
    /// </summary>
    public static ISizeStrategy ColumnsWhileAny(Func<CellValue, bool> predicate)
      => new ColumnsWhileAnySizeStrategy(predicate);

    /// <summary>Full available height, and the leading columns that carry values.</summary>
    public static ISizeStrategy ColumnsWhileAnyValue()
      => ColumnsWhileAny(v => v.HasValue);

    public static ISizeStrategy SelectSize(Func<ISpace, Size> selector)
      => new SelectSizeStrategy(selector);
  }
}
