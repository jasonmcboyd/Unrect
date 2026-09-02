using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class RowsWhileAnySizeStrategy : ISizeStrategy
  {
    public RowsWhileAnySizeStrategy(Func<CellValue, bool> predicate)
    {
      RowSelectionStrategy = RowStrategies.TakeRowsWhileAny(predicate);
    }

    private IRowStrategy RowSelectionStrategy { get; }

    public Size GetSize(ISpace availableSpace)
      => new Size(availableSpace.Area.Width, RowSelectionStrategy.SelectRows(availableSpace));
  }
}
