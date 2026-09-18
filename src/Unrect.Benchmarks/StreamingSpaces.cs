using Unrect.Spreadsheets;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The streaming family's fixtures: a synthetic row source, and the two spaces built over it.
  ///
  /// <para><b>No workbook, deliberately.</b> CI runners get no files, and a benchmark that needed
  /// one could not run — the same rule the rest of the rig follows. The internal row-source seam
  /// exists partly for this: a source that generates rows on demand behaves like a reader from the
  /// store's point of view, so everything above it is exercised for real.</para>
  ///
  /// <para><b>What that costs in honesty, said out loud.</b> Opening this source is free, where
  /// opening a spreadsheet costs about five CPU-bound seconds parsing the shared-string table. So
  /// the adversarial rows measure the <em>repositioning</em> half of the pool's value and not the
  /// open half — the half that made warming worth building. The open half is a property of
  /// ExcelDataReader, is measured nowhere in CI on purpose, and no number here should be read as
  /// covering it.</para>
  /// </summary>
  internal static class StreamingSpaces
  {
    /// <summary>
    /// Rows per sheet. Chosen so every row of the family clears the noise floor comfortably while
    /// the whole leg stays CI-sane: at eight columns this is two million cells, and the monotone
    /// parse of it lands in the tens of milliseconds.
    /// </summary>
    public const int Rows = 250_000;

    public const int Columns = 8;

    /// <summary>The default window, in rows — the same default a <c>Workbook</c> would use.</summary>
    /// <summary>
    /// A band five children sweep together. Sized so the pair around it is a real comparison: the
    /// "fits" half gets twice this in window and loads each chunk once; the "too small" half gets
    /// half of it and reloads. Measured at this size, 118 loads / 0 reloads against 590 / 472.
    /// </summary>
    public const int BandRows = 40_000;

    /// <summary>Row 0 carries the captions the table binds against.</summary>
    public static string Caption(int column) => "C" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// The cell at a coordinate, defined once so the eager and windowed fixtures cannot drift: the
    /// two headline rows only mean something as a ratio, and a ratio between different data means
    /// nothing.
    /// </summary>
    public static Cell At(int column, int row)
    {
      if (row == 0)
        return Cell.Of(Caption(column));

      // A mix of kinds, so the parse pays what a real one pays rather than reading a column of
      // identical numbers.
      return (column % 4) switch
      {
        0 => Cell.Of("r" + row.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        1 => Cell.Of(row * 10 + column),
        2 => Cell.Of((decimal)(row + column) / 4m),
        _ => Cell.Of(row % 2 == 0),
      };
    }

    /// <summary>The same rows as a materialised grid: the eager side of the headline ratio.</summary>
    public static ISheetCells Grid(int rows = Rows, int columns = Columns)
    {
      var cells = new Cell[rows, columns];

      for (var row = 0; row < rows; row++)
        for (var column = 0; column < columns; column++)
          cells[row, column] = At(column, row);

      return SheetGrid.Of(cells);
    }

    /// <summary>A pool over a fresh synthetic source.</summary>
    /// <summary>A workbook over a synthetic sheet of <paramref name="rows"/> by <paramref name="columns"/>, with no cap unless one is given.</summary>
    public static Workbook Book(int rows = Rows, int columns = Columns, int? bufferRows = null)
      => Workbook.Over(new SyntheticRowSource(rows, columns), new WorkbookOptions { BufferRows = bufferRows });

    private sealed class SyntheticRowSource : IRowSource
    {
      private readonly int _rows;
      private readonly int _columns;

      internal SyntheticRowSource(int rows, int columns)
      {
        _rows = rows;
        _columns = columns;
      }

      public string Name => "synthetic";

      public IRowCursor Open() => new SyntheticRowCursor(_rows, _columns);
    }

    private sealed class SyntheticRowCursor : IRowCursor
    {
      private readonly int _rows;
      private int _row = -1;

      internal SyntheticRowCursor(int rows, int columns)
      {
        _rows = rows;
        ColumnCount = columns;
      }

      public int SheetIndex => 0;

      public string SheetName => "Data";

      public int RowCount => _rows;

      public int ColumnCount { get; }

      public bool NextSheet() => false;

      public bool Read()
      {
        if (_row + 1 >= _rows)
          return false;

        _row++;

        return true;
      }

      public Cell this[int column] =>
        column < 0 || column >= ColumnCount ? Cell.Blank : At(column, _row);

      public void Dispose()
      {
      }
    }
  }

  /// <summary>The row the streaming family's table declaration binds to.</summary>
  public sealed record StreamedRow(string C0, int C1, decimal C2, bool C3, string C4, int C5, decimal C6, bool C7);
}
