using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The column transpose of <see cref="RowsWhileAnySizeStrategy"/>: full available height, and as
  /// many leading columns as have at least one cell satisfying the predicate.
  /// </summary>
  internal sealed class ColumnsWhileAnySizeStrategy : ISizeStrategy
  {
    public ColumnsWhileAnySizeStrategy(Func<CellValue, bool> predicate)
    {
      ColumnSelectionStrategy = ColumnStrategies.TakeColumnsWhileAny(predicate);
    }

    private IColumnStrategy ColumnSelectionStrategy { get; }

    public Size GetSize(ISpace availableSpace)
      => new Size(ColumnSelectionStrategy.SelectColumns(availableSpace), availableSpace.Area.Height);
  }
}
