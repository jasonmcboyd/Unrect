using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="IAreaStrategy"/> — how a shape's declared extent is found, once its origin is known. Each wraps the <see cref="SizeStrategies"/> twin of the same name.</summary>
  public static class AreaStrategies
  {
    /// <summary>The whole of whatever space is available. See <see cref="SizeStrategies.MaxSize"/>.</summary>
    public static IAreaStrategy MaxArea()
      => MaxSize().ToAreaStrategy();

    /// <summary>Zero by zero. See <see cref="SizeStrategies.MinSize"/>.</summary>
    public static IAreaStrategy MinArea()
      => MinSize().ToAreaStrategy();

    /// <summary>Exactly <paramref name="width"/> by <paramref name="height"/>; throws <see cref="OutOfBoundsException"/> when that does not fit. See <see cref="SizeStrategies.ExplicitSize"/>.</summary>
    public static IAreaStrategy ExplicitArea(int width, int height)
      => ExplicitSize(width, height).ToAreaStrategy();

    /// <summary>
    /// Rows first, then columns measured inside them — the order that matters when a table's width
    /// should be judged from the rows it actually occupies.
    /// </summary>
    public static IAreaStrategy RowsThenColumns(IRowStrategy rows, IColumnStrategy columns)
      => new RowAndColumnSizeStrategy(rows, columns).ToAreaStrategy();

    /// <summary>Columns first, then rows measured inside them; the transpose of <see cref="RowsThenColumns"/>.</summary>
    public static IAreaStrategy ColumnsThenRows(IColumnStrategy columns, IRowStrategy rows)
      => new RowAndColumnSizeStrategy(columns, rows).ToAreaStrategy();

    /// <summary>Whatever <paramref name="selector"/> computes from the available space. See <see cref="SizeStrategies.SelectSize"/>.</summary>
    public static IAreaStrategy SelectArea(Func<ISpace, Size> selector)
      => SelectSize(selector).ToAreaStrategy();
  }
}
