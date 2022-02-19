using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  public static class RowStrategies<TSpace>
  {
    public static IRowStrategy<TSpace> TakeRowsWhile(Func<ISpace<TSpace>, uint, bool> predicate) 
      => new TakeToRowStrategy<TSpace>(predicate.Not(), false);

    public static IRowStrategy<TSpace> TakeRowsWhile(uint column, Func<TSpace, uint, bool> predicate)
      => TakeRowsWhile((space, row) => predicate(space[column, row], row));

    public static IRowStrategy<TSpace> TakeRowsTo(Func<ISpace<TSpace>, uint, bool> predicate)
      => new TakeToRowStrategy<TSpace>(predicate, true);

    public static IRowStrategy<TSpace> TakeRowsToValue<TValue>(uint column, TValue value)
      => TakeRowsTo((space, row) => space[column, row].Equals(value));

    public static IRowStrategy<TSpace> TakeRowsWhileAll(Func<TSpace, bool> predicate) 
      => new TakeToAllRowStrategy<TSpace>(predicate.Not(), false);

    public static IRowStrategy<TSpace> TakeRowsWhileAny(Func<TSpace, bool> predicate) 
      => new TakeToAllRowStrategy<TSpace>(predicate, false);
  }

  internal static class RowStrategies
  {
    public static IAreaStrategy<TSpace> TakeRowsWhile<TSpace>(
      this IColumnStrategy<TSpace> strategy,
      Func<ISpace<TSpace>, uint, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, RowStrategies<TSpace>.TakeRowsWhile(predicate)).ToAreaStrategy();

    public static IAreaStrategy<TSpace> TakeRowsWhileAll<TSpace>(
      this IColumnStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, RowStrategies<TSpace>.TakeRowsWhileAll(predicate)).ToAreaStrategy();

    public static IAreaStrategy<TSpace> TakeRowsWhileAny<TSpace>(
      this IColumnStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, RowStrategies<TSpace>.TakeRowsWhileAny(predicate)).ToAreaStrategy();
  }
}
