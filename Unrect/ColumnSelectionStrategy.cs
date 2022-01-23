using System;
using Unrect.Core;
using Unrect.ColumnSelectionStrategies;
using Unrect.Size;

namespace Unrect
{
  public static class ColumnSelectionStrategy
  {
    public static IColumnSelectionStrategy<TSpace> TakeColumnsWhile<TSpace>(
      Func<ISpace<TSpace>, uint, bool> predicate) 
      => new TakeWhileColumnSelectionStrategy<TSpace>(predicate);

    public static IAreaStrategy<TSpace> TakeColumnsWhile<TSpace>(
      this IRowSelectionStrategy<TSpace> strategy,
      Func<ISpace<TSpace>, uint, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeColumnsWhile(predicate)).ToAreaStrategy();

    public static IColumnSelectionStrategy<TSpace> TakeColumnsWhileAll<TSpace>(Func<TSpace, bool> predicate) 
      => new TakeWhileAllColumnSelectionStrategy<TSpace>(predicate);

    public static IAreaStrategy<TSpace> TakeColumnsWhileAll<TSpace>(
      this IRowSelectionStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeColumnsWhileAll(predicate)).ToAreaStrategy();

    public static IColumnSelectionStrategy<TSpace> TakeColumnsWhileAny<TSpace>(Func<TSpace, bool> predicate) 
      => new TakeWhileAllColumnSelectionStrategy<TSpace>(value => !predicate(value));

    public static IAreaStrategy<TSpace> TakeColumnsWhileAny<TSpace>(
      this IRowSelectionStrategy<TSpace> strategy,
      Func<TSpace, bool> predicate)
      => new RowAndColumnSizeStrategy<TSpace>(strategy, TakeColumnsWhileAny(predicate)).ToAreaStrategy();
  }
}
