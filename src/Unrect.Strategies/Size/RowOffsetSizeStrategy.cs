using Unrect.Core;

namespace Unrect.Strategies
{
  internal class RowOffsetSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public RowOffsetSizeStrategy(IRowStrategy<TSpace> rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
    }

    private IRowStrategy<TSpace> RowSelectionStrategy { get; }

    public Size GetSize(ISpace<TSpace> availableSpace)
      => new Size(0, RowSelectionStrategy.SelectRows(availableSpace));
  }
}
