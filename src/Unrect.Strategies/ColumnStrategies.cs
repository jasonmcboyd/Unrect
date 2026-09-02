using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="IColumnStrategy"/> — the column twin of <see cref="RowStrategies"/>.</summary>
  public static class ColumnStrategies
  {
    /// <summary>Leading columns for which <paramref name="predicate"/> holds; stops at the first column it does not, keeping the match out.</summary>
    public static IColumnStrategy TakeColumnsWhile(Func<ISpace, int, bool> predicate)
      => new TakeWhileColumnStrategy(predicate);

    /// <summary>
    /// Columns while <paramref name="predicate"/> holds of the cell in <paramref name="row"/> — the
    /// transpose of <see cref="RowStrategies.TakeRowsWhile(int, Func{CellValue, int, bool})"/>, for
    /// reading a band off one caption row.
    /// </summary>
    public static IColumnStrategy TakeColumnsWhile(int row, Func<CellValue, int, bool> predicate)
      => TakeColumnsWhile((space, column) => predicate(space[column, row], column));

    /// <summary>Exactly <paramref name="count"/> columns; throws <see cref="OutOfBoundsException"/> when that does not fit.</summary>
    public static IColumnStrategy TakeColumns(int count)
      => new ExplicitColumnCountStrategy(count);

    /// <summary>
    /// Columns up to and including the first satisfying <paramref name="predicate"/> — the transpose
    /// of <see cref="RowStrategies.TakeRowsTo"/>. The match is kept, where a while-strategy stops
    /// before it.
    /// </summary>
    public static IColumnStrategy TakeColumnsTo(Func<ISpace, int, bool> predicate)
      => new TakeToColumnStrategy(predicate);

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

    /// <summary>Leading columns in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IColumnStrategy TakeColumnsWhileAll(Func<CellValue, bool> predicate)
      => new TakeWhileAllColumnStrategy(predicate);

    /// <summary>Leading columns in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IColumnStrategy TakeColumnsWhileAny(Func<CellValue, bool> predicate)
      => new TakeWhileAnyColumnStrategy(predicate);

    /// <summary>Leading columns that carry a value — <see cref="TakeColumnsWhileAny(Func{CellValue, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static IColumnStrategy TakeColumnsWhileAnyValue()
      => TakeColumnsWhileAny(v => v.HasValue);

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhile(Func{ISpace, int, bool})"/>, rows measured first.</summary>
    public static IAreaStrategy TakeColumnsWhile(
      this IRowStrategy strategy,
      Func<ISpace, int, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhile(predicate)).ToAreaStrategy();

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhileAll(Func{CellValue, bool})"/>, rows measured first.</summary>
    public static IAreaStrategy TakeColumnsWhileAll(
      this IRowStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhileAll(predicate)).ToAreaStrategy();

    /// <summary>Combines <paramref name="strategy"/>'s rows with columns selected by <see cref="TakeColumnsWhileAny(Func{CellValue, bool})"/>, rows measured first.</summary>
    public static IAreaStrategy TakeColumnsWhileAny(
      this IRowStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeColumnsWhileAny(predicate)).ToAreaStrategy();

    /// <summary>Those rows, at the columns that carry values — <see cref="TakeColumnsWhileAny(Func{CellValue, bool})"/> with <c>HasValue</c> as the predicate.</summary>
    public static IAreaStrategy TakeColumnsWhileAnyValue(this IRowStrategy strategy)
      => strategy.TakeColumnsWhileAny(v => v.HasValue);

    /// <summary>Those rows, at the full available width.</summary>
    public static IAreaStrategy AllColumns(this IRowStrategy strategy)
      => strategy.TakeColumnsWhile((_, _) => true);
  }
}
