using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ColumnOffsetSizeStrategy : ISizeStrategy
  {
    public ColumnOffsetSizeStrategy(IColumnStrategy columnSelectionStrategy)
    {
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    private IColumnStrategy ColumnSelectionStrategy { get; }

    public Size GetSize(ISpace availableSpace)
      => new Size(ColumnSelectionStrategy.SelectColumns(availableSpace), 0);
  }
}
