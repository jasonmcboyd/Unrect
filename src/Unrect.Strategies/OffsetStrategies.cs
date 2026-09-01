using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  public static class OffsetStrategies
  {
    public static IOffsetStrategy MaxOffset()
      => MaxSize().ToOffsetStrategy();

    public static IOffsetStrategy MinOffset()
      => MinSize().ToOffsetStrategy();

    public static IOffsetStrategy ExplicitOffset(int width, int height)
      => ExplicitSize(width, height).ToOffsetStrategy();

    public static IOffsetStrategy SelectOffset(Func<ISpace, Size> selector)
      => SelectSize(selector).ToOffsetStrategy();

    public static IOffsetStrategy SkipRowsWhileAll(Func<CellValue, bool> predicate)
      => new RowOffsetSizeStrategy(RowStrategies.TakeRowsWhileAll(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipRowsWhileAny(Func<CellValue, bool> predicate)
      => new RowOffsetSizeStrategy(RowStrategies.TakeRowsWhileAny(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipBlankRows()
      => SkipRowsWhileAll(v => v.IsBlank);

    public static IOffsetStrategy SkipColumnsWhileAll(Func<CellValue, bool> predicate)
      => new ColumnOffsetSizeStrategy(ColumnStrategies.TakeColumnsWhileAll(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipColumnsWhileAny(Func<CellValue, bool> predicate)
      => new ColumnOffsetSizeStrategy(ColumnStrategies.TakeColumnsWhileAny(predicate)).ToOffsetStrategy();

    public static IOffsetStrategy SkipBlankColumns()
      => SkipColumnsWhileAll(v => v.IsBlank);

    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets)
      => new CompositeOffsetSizeStrategy(offsets).ToOffsetStrategy();
  }
}
