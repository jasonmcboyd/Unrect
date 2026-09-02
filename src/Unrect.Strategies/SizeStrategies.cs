using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="ISizeStrategy"/> — how a shape's extent is discovered or declared.</summary>
  public static class SizeStrategies
  {
    /// <summary>The whole of whatever space is available.</summary>
    public static ISizeStrategy MaxSize()
      => new MaxSizeStrategy();

    /// <summary>Zero by zero — no extent at all.</summary>
    public static ISizeStrategy MinSize()
      => new ExplicitSizeStrategy(0, 0);

    /// <summary>Exactly <paramref name="width"/> by <paramref name="height"/>; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static ISizeStrategy ExplicitSize(int width, int height)
      => new ExplicitSizeStrategy(width, height);

    /// <summary>Full available width, and as many leading rows as have at least one cell satisfying <paramref name="predicate"/>.</summary>
    public static ISizeStrategy RowsWhileAny(Func<CellValue, bool> predicate)
      => new RowsWhileAnySizeStrategy(predicate);

    /// <summary>Full available width, and the leading rows that carry values — <see cref="RowsWhileAny"/> with <c>HasValue</c> as the predicate.</summary>
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

    /// <summary>Whatever <paramref name="selector"/> computes from the available space — the escape hatch when no other strategy fits.</summary>
    public static ISizeStrategy SelectSize(Func<ISpace, Size> selector)
      => new SelectSizeStrategy(selector);
  }
}
