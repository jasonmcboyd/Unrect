using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class AreaStrategies
  {
    public static IAreaStrategy MaxArea()
      => MaxSize().ToAreaStrategy();

    public static IAreaStrategy MinArea()
      => MinSize().ToAreaStrategy();

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

    public static IAreaStrategy SelectArea(Func<ISpace, Size> selector)
      => SelectSize(selector).ToAreaStrategy();
  }
}
