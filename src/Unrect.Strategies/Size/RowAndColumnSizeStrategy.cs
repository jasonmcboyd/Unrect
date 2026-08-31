using Unrect.Core;

namespace Unrect.Strategies
{
  internal class RowAndColumnSizeStrategy : ISizeStrategy
  {
    public RowAndColumnSizeStrategy(
      IRowStrategy rowSelectionStrategy,
      IColumnStrategy columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = true;
    }

    public RowAndColumnSizeStrategy(
      IColumnStrategy columnSelectionStrategy,
      IRowStrategy rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = false;
    }

    private IRowStrategy RowSelectionStrategy { get; }
    private IColumnStrategy ColumnSelectionStrategy { get; }
    private bool RowFirst { get; }

    public Size GetSize(ISpace availableSpace)
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
