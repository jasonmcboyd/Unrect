using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  public static class ColumnStrategies<TSpace>
  {
    public static IColumnStrategy<TSpace> TakeColumnsWhile(
      Func<ISpace<TSpace>, uint, bool> predicate)
      => new TakeWhileColumnStrategy<TSpace>(predicate);

    public static IColumnStrategy<TSpace> TakeColumnsWhileAll(Func<TSpace, bool> predicate)
      => new TakeWhileAllColumnStrategy<TSpace>(predicate);

    public static IColumnStrategy<TSpace> TakeColumnsWhileAny(Func<TSpace, bool> predicate)
      => new TakeWhileAllColumnStrategy<TSpace>(value => !predicate(value));
  }

  public static class ColumnStrategies
  {
    public static IAreaStrategy<TSpace> TakeColumnsWhile<TSpace>(
      this IRowStrategy<TSpace> strategy,
      Func<ISpace<TSpace>, uint, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, ColumnStrategies<TSpace>.TakeColumnsWhile(predicate)).ToAreaStrategy();

    public static IAreaStrategy<TSpace> TakeColumnsWhileAll<TSpace>(
      this IRowStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, ColumnStrategies<TSpace>.TakeColumnsWhileAll(predicate)).ToAreaStrategy();

    public static IAreaStrategy<TSpace> TakeColumnsWhileAny<TSpace>(
      this IRowStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, ColumnStrategies<TSpace>.TakeColumnsWhileAny(predicate)).ToAreaStrategy();
  }
}
