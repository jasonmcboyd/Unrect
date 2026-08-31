using Unrect.Core;

namespace Unrect.Strategies
{
  internal class ColumnOffsetSizeStrategy : ISizeStrategy
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
