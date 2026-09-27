using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="ISizeStrategy{TSpace}"/> — how a projection's extent is discovered or declared.</summary>
  public static class SizeStrategies
  {
    /// <summary>The whole of whatever space is available.</summary>
    public static ISizeStrategy<TSpace> MaxSize<TSpace>()
      where TSpace : class, ISpace
      => new MaxSizeStrategy<TSpace>();

    /// <summary>Zero by zero — no extent at all.</summary>
    public static ISizeStrategy<TSpace> MinSize<TSpace>()
      where TSpace : class, ISpace
      => new ExplicitSizeStrategy<TSpace>(0, 0);

    /// <summary>Exactly <paramref name="width"/> by <paramref name="height"/>; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static ISizeStrategy<TSpace> ExplicitSize<TSpace>(int width, int height)
      where TSpace : class, ISpace
      => new ExplicitSizeStrategy<TSpace>(width, height);

    /// <summary>Full available width, and as many leading rows as have at least one cell satisfying <paramref name="predicate"/>.</summary>
    public static ISizeStrategy<TSpace> RowsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new RowsWhileAnySizeStrategy<TSpace>(predicate);

    /// <summary>Full available width, and the leading rows that carry values — <see cref="RowsWhileAny"/> with <c>HasValue</c> as the predicate.</summary>
    public static ISizeStrategy<TSpace> RowsWhileAnyIsNotBlank<TSpace>()
      where TSpace : class, ISpace
      => RowsWhileAny<TSpace>(v => !v.IsBlank());

    /// <summary>
    /// Full available height, and as many leading columns as have at least one cell satisfying
    /// <paramref name="predicate"/> — the transpose of <see cref="RowsWhileAny"/>.
    /// </summary>
    public static ISizeStrategy<TSpace> ColumnsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new ColumnsWhileAnySizeStrategy<TSpace>(predicate);

    /// <summary>Full available height, and the leading columns that carry values.</summary>
    public static ISizeStrategy<TSpace> ColumnsWhileAnyIsNotBlank<TSpace>()
      where TSpace : class, ISpace
      => ColumnsWhileAny<TSpace>(v => !v.IsBlank());

    /// <summary>Whatever <paramref name="selector"/> computes from the available space — the escape hatch when no other strategy fits.</summary>
    public static ISizeStrategy<TSpace> SelectSize<TSpace>(Func<Plane<TSpace>, Size> selector)
      where TSpace : class, ISpace
      => new SelectSizeStrategy<TSpace>(selector);

    /// <summary>
    /// Rows first, then columns measured inside them — the order that matters when a table's width
    /// should be judged from the rows it actually occupies. Where both halves are per-row rules the
    /// two are read as one forward walk, which leaves the height discoverable as a projection
    /// consumes it; otherwise the extent is measured up front.
    /// </summary>
    public static ISizeStrategy<TSpace> RowsThenColumns<TSpace>(ILineStrategy<TSpace> rows, ILineStrategy<TSpace> columns)
      where TSpace : class, ISpace
      => RowAndColumnSizeStrategy<TSpace>.RowsThenColumns(rows, columns);

    /// <summary>Columns first, then rows measured inside them; the transpose of <see cref="RowsThenColumns"/>, and always measured up front.</summary>
    public static ISizeStrategy<TSpace> ColumnsThenRows<TSpace>(ILineStrategy<TSpace> columns, ILineStrategy<TSpace> rows)
      where TSpace : class, ISpace
      => RowAndColumnSizeStrategy<TSpace>.ColumnsThenRows(columns, rows);
  }
}
