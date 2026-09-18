using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class ColumnOffsetSizeStrategy : ISizeStrategy
  {
    public ColumnOffsetSizeStrategy(IColumnStrategy columnSelectionStrategy)
    {
      ColumnSelectionStrategy = columnSelectionStrategy;
    }

    internal IColumnStrategy ColumnSelectionStrategy { get; }

    public Size GetSize(Plane<ISpace> availableSpace)
      => new Size(ColumnSelectionStrategy.SelectColumns(availableSpace), 0);
  }
}
