using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  internal class WhileAnySizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public WhileAnySizeStrategy(Func<TSpace, bool> predicate)
    {
      RowSelectionStrategy = RowStrategies<TSpace>.TakeRowsWhileAny(predicate);
    }

    private IRowStrategy<TSpace> RowSelectionStrategy { get; }

    public Size GetSize(ISpace<TSpace> availableSpace)
      => new Size(availableSpace.Area.Size.Width, RowSelectionStrategy.SelectRows(availableSpace));
  }
}
