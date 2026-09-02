using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class RowOffsetSizeStrategy : ISizeStrategy
  {
    public RowOffsetSizeStrategy(IRowStrategy rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
    }

    private IRowStrategy RowSelectionStrategy { get; }

    public Size GetSize(ISpace availableSpace)
      => new Size(0, RowSelectionStrategy.SelectRows(availableSpace));
  }
}
