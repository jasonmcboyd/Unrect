using Unrect.Core;

namespace Unrect.Strategies
{
  internal class RowAndColumnSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public RowAndColumnSizeStrategy(
      IRowStrategy<TSpace> rowSelectionStrategy,
      IColumnStrategy<TSpace> columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = true;
    }

    public RowAndColumnSizeStrategy(
      IColumnStrategy<TSpace> columnSelectionStrategy,
      IRowStrategy<TSpace> rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = false;
    }

    private IRowStrategy<TSpace> RowSelectionStrategy { get; }
    private IColumnStrategy<TSpace> ColumnSelectionStrategy { get; }
    private bool RowFirst { get; }

    public Size GetSize(ISpace<TSpace> availableSpace)
    {
      if (RowFirst)
      {
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Area(availableSpace.Area.Size.Width, rowCount));
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        return new Size(columnCount, rowCount);
      }
      else
      {
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Area(columnCount, availableSpace.Area.Size.Height));
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        return new Size(columnCount, rowCount);
      }
    }
  }
}
