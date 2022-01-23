using Unrect.Core;

namespace Unrect.Size
{
  public class RowAndColumnSizeStrategy<TSpace> : ISizeStrategy<TSpace>
  {
    public RowAndColumnSizeStrategy(
      IRowSelectionStrategy<TSpace> rowSelectionStrategy,
      IColumnSelectionStrategy<TSpace> columnSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = true;
    }

    public RowAndColumnSizeStrategy(
      IColumnSelectionStrategy<TSpace> columnSelectionStrategy,
      IRowSelectionStrategy<TSpace> rowSelectionStrategy)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = false;
    }

    private IRowSelectionStrategy<TSpace> RowSelectionStrategy { get; }
    private IColumnSelectionStrategy<TSpace> ColumnSelectionStrategy { get; }
    private bool RowFirst { get; }

    public Core.Size GetSize(ISpace<TSpace> availableSpace)
    {
      if (RowFirst)
      {
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Core.Area(availableSpace.Area.Size.Width, rowCount));
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        return new Core.Size(columnCount, rowCount);
      }
      else
      {
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Core.Area(columnCount, availableSpace.Area.Size.Height));
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        return new Core.Size(columnCount, rowCount);
      }
    }
  }
}
