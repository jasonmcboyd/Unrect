using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  public static class RowStrategies
  {
    public static IRowStrategy TakeRowsWhile(Func<ISpace, int, bool> predicate)
      => new TakeToRowStrategy(predicate.Not(), false);

    public static IRowStrategy TakeRowsWhile(int column, Func<CellValue, int, bool> predicate)
      => TakeRowsWhile((space, row) => predicate(space[column, row], row));

    public static IRowStrategy TakeRows(int count)
      => new ExplicitRowCountStrategy(count);

    public static IRowStrategy TakeRowsTo(Func<ISpace, int, bool> predicate)
      => new TakeToRowStrategy(predicate, true);

    public static IRowStrategy TakeRowsToValue(int column, CellValue value)
      => TakeRowsTo((space, row) => space[column, row].Equals(value));

    public static IRowStrategy TakeRowsWhileAll(Func<CellValue, bool> predicate)
      => new TakeWhileAllRowStrategy(predicate);

    public static IRowStrategy TakeRowsWhileAny(Func<CellValue, bool> predicate)
      => new TakeWhileAnyRowStrategy(predicate);

    public static IRowStrategy TakeRowsWhileAnyValue()
      => TakeRowsWhileAny(v => v.HasValue);

    /// <summary>
    /// Every row of the available space. The declared spelling of "the full height", which otherwise
    /// has to be written as the opaque constant predicate <c>(s, r) =&gt; true</c>.
    /// </summary>
    public static IRowStrategy AllRows() => TakeRowsWhile((_, _) => true);

    public static IAreaStrategy TakeRowsWhile(
      this IColumnStrategy strategy,
      Func<ISpace, int, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeRowsWhile(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeRowsWhileAll(
      this IColumnStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeRowsWhileAll(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeRowsWhileAny(
      this IColumnStrategy strategy,
      Func<CellValue, bool> predicate)
      => new RowAndColumnSizeStrategy(strategy, TakeRowsWhileAny(predicate)).ToAreaStrategy();

    public static IAreaStrategy TakeRowsWhileAnyValue(this IColumnStrategy strategy)
      => strategy.TakeRowsWhileAny(v => v.HasValue);

    /// <summary>Those columns, at the full available height.</summary>
    public static IAreaStrategy AllRows(this IColumnStrategy strategy)
      => strategy.TakeRowsWhile((_, _) => true);
  }
}
