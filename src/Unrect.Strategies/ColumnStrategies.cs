using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  public static class ColumnStrategies
  {
    public static IColumnStrategy TakeColumnsWhile(Func<ISpace, int, bool> predicate)
      => new TakeWhileColumnStrategy(predicate);

    public static IColumnStrategy TakeColumns(int count)
      => new ExplicitColumnCountStrategy(count);

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
  }
}
