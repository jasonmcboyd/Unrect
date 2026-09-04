using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// One dimension measured first and the other measured inside what it found — the two-pass reading
  /// of that, which is every combination the interleave of
  /// <see cref="InterleavedRowAndColumnSizeStrategy"/> cannot serve.
  /// </summary>
  internal sealed class RowAndColumnSizeStrategy : ISizeStrategy
  {
    private RowAndColumnSizeStrategy(
      IRowStrategy rowSelectionStrategy,
      IColumnStrategy columnSelectionStrategy,
      bool rowFirst)
    {
      RowSelectionStrategy = rowSelectionStrategy;
      ColumnSelectionStrategy = columnSelectionStrategy;
      RowFirst = rowFirst;
    }

    private IRowStrategy RowSelectionStrategy { get; }
    private IColumnStrategy ColumnSelectionStrategy { get; }
    private bool RowFirst { get; }

    /// <summary>
    /// Rows over the full width, then columns within them — as one forward walk where both halves are
    /// per-row rules, and as two passes where either is not. The choice is made here, once, so that
    /// every spelling of rows-then-columns gets the incremental reading when it can have it and no
    /// spelling claims to be discoverable when it is not.
    /// </summary>
    public static ISizeStrategy RowsThenColumns(IRowStrategy rows, IColumnStrategy columns)
    {
      if (rows is IIncrementalRowStrategy incrementalRows && columns is IRowMajorColumnStrategy rowMajorColumns)
        return new InterleavedRowAndColumnSizeStrategy(incrementalRows, rowMajorColumns);

      return new RowAndColumnSizeStrategy(rows, columns, rowFirst: true);
    }

    /// <summary>
    /// Columns over the full height, then rows within them — always two passes, and deliberately so.
    /// A column rule read down the whole available height decides the width from rows the row bound
    /// may never reach, which is precisely the reading an <see cref="IAreaScan"/> may not have: its
    /// width "may never consume rows the height scan would not". The same judgement rules out
    /// <see cref="SizeStrategies.ColumnsWhileAny"/>, and for the same reason.
    /// </summary>
    public static ISizeStrategy ColumnsThenRows(IColumnStrategy columns, IRowStrategy rows)
      => new RowAndColumnSizeStrategy(rows, columns, rowFirst: false);

    public Size GetSize(ISpace availableSpace)
    {
      if (RowFirst)
      {
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Area(availableSpace.Area.Width, rowCount));
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        return new Size(columnCount, rowCount);
      }
      else
      {
        var columnCount = ColumnSelectionStrategy.SelectColumns(availableSpace);
        availableSpace = availableSpace.GetSubspace(new Area(columnCount, availableSpace.Area.Height));
        var rowCount = RowSelectionStrategy.SelectRows(availableSpace);
        return new Size(columnCount, rowCount);
      }
    }
  }
}
