using System;
using Unrect.Core;
using Unrect.RowSelectionStrategies;
using Unrect.Size;

namespace Unrect
{
  public static class RowSelectionStrategy
  {
    public static IRowSelectionStrategy<TSpace> TakeRowsWhile<TSpace>(Func<ISpace<TSpace>, uint, bool> predicate) 
      => new TakeWhileRowSelectionStrategy<TSpace>(predicate);

    public static IAreaStrategy<TSpace> TakeRowsWhile<TSpace>(
      this IColumnSelectionStrategy<TSpace> strategy,
      Func<ISpace<TSpace>, uint, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeRowsWhile(predicate));

    public static IRowSelectionStrategy<TSpace> TakeRowsWhileAll<TSpace>(Func<TSpace, bool> predicate) 
      => new TakeWhileAllRowSelectionStrategy<TSpace>(predicate);

    public static IAreaStrategy<TSpace> TakeRowsWhileAll<TSpace>(
      this IColumnSelectionStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeRowsWhileAll(predicate));

    public static IRowSelectionStrategy<TSpace> TakeRowsWhileAny<TSpace>(Func<TSpace, bool> predicate) 
      => new TakeWhileAllRowSelectionStrategy<TSpace>(value => !predicate(value));

    public static IAreaStrategy<TSpace> TakeRowsWhileAny<TSpace>(
      this IColumnSelectionStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeRowsWhileAny(predicate));
  }
}
