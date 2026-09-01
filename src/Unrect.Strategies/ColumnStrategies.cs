using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  public static class ColumnStrategies
  {
    public static IColumnStrategy TakeColumnsWhile(Func<ISpace, int, bool> predicate)
      => new TakeWhileColumnStrategy(predicate);

    /// <summary>
    /// Columns while <paramref name="predicate"/> holds of the cell in <paramref name="row"/> — the
    /// transpose of <see cref="RowStrategies.TakeRowsWhile(int, Func{CellValue, int, bool})"/>, for
    /// reading a band off one caption row.
    /// </summary>
    public static IColumnStrategy TakeColumnsWhile(int row, Func<CellValue, int, bool> predicate)
      => TakeColumnsWhile((space, column) => predicate(space[column, row], column));

    public static IColumnStrategy TakeColumns(int count)
      => new ExplicitColumnCountStrategy(count);

    /// <summary>
    /// Columns up to and including the first satisfying <paramref name="predicate"/> — the transpose
    /// of <see cref="RowStrategies.TakeRowsTo"/>. The match is kept, where a while-strategy stops
    /// before it.
    /// </summary>
    public static IColumnStrategy TakeColumnsTo(Func<ISpace, int, bool> predicate)
      => new TakeToColumnStrategy(predicate, true);

    /// <summary>
    /// Columns up to and including the first whose cell in <paramref name="row"/> equals
    /// <paramref name="value"/> — the transpose of <see cref="RowStrategies.TakeRowsToValue"/>.
    /// </summary>
    public static IColumnStrategy TakeColumnsToValue(int row, CellValue value)
      => TakeColumnsTo((space, column) => space[column, row].Equals(value));

    /// <summary>
    /// Every column of the available space. The declared spelling of "the full width", which
    /// otherwise has to be written as the opaque constant predicate <c>(s, c) =&gt; true</c>.
    /// </summary>
    public static IColumnStrategy AllColumns() => TakeColumnsWhile((_, _) => true);

    public static IColumnStrategy TakeColumnsWhileAll(Func<CellValue, bool> predicate)
      => new TakeWhileAllColumnStrategy(predicate);

    public static IColumnStrategy TakeColumnsWhileAny(Func<CellValue, bool> predicate)
      => new TakeWhileAnyColumnStrategy(predicate);

    public static IColumnStrategy TakeColumnsWhileAnyValue()
      => TakeColumnsWhileAny(v => v.HasValue);

    public static IAreaStrategy TakeColumnsWhile(
      this IRowStrategy strategy,
      Func<ISpace, int, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhile(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeColumnsWhileAll(
      this IRowStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhileAll(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeColumnsWhileAny(
      this IRowStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhileAny(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeColumnsWhileAnyValue(this IRowStrategy strategy)
      => strategy.TakeColumnsWhileAny(v => v.HasValue);

    /// <summary>Those rows, at the full available width.</summary>
    public static IAreaStrategy AllColumns(this IRowStrategy strategy)
      => strategy.TakeColumnsWhile((_, _) => true);
  }
}
