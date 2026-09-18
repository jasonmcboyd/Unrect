using Unrect.Core;

namespace Unrect.Strategies
{
  internal sealed class RowOffsetSizeStrategy : ISizeStrategy
  {
    public RowOffsetSizeStrategy(IRowStrategy rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
    }

    internal IRowStrategy RowSelectionStrategy { get; }

    public Size GetSize(Plane<ISpace> availableSpace)
      => new Size(0, RowSelectionStrategy.SelectRows(availableSpace));
  }
}
