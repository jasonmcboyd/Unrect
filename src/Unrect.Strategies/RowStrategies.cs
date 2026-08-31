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
      => new TakeToAllRowStrategy(predicate, false);

    public static IRowStrategy TakeRowsWhileAny(Func<CellValue, bool> predicate)
      => new TakeToAnyRowStrategy(predicate, false);

    public static IRowStrategy TakeRowsWhileAnyValue()
      => TakeRowsWhileAny(v => v.HasValue);

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
  }
}
