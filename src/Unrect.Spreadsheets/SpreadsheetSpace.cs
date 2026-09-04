using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One worksheet of a spreadsheet file, as a space. Reads <c>.xls</c> and <c>.xlsx</c> through
  /// ExcelDataReader and adapts each cell to a <see cref="CellValue"/> — which is where
  /// <em>blankness is decided</em>, the one question the grid itself cannot answer.
  /// <para>
  /// <b>Known limitation: the modern Excel errors arrive as blank on the .xlsx path.</b>
  /// ExcelDataReader's XML reader returns null for an error literal it does not recognise <em>and</em>
  /// nulls the value with it, so a <c>#SPILL!</c>, <c>#CALC!</c> or <c>#FIELD!</c> cell reaches this
  /// adapter byte-for-byte identical to an empty one. The information is destroyed upstream; there
  /// is nothing here to detect and no workaround worth attempting.
  /// </para>
  /// <para>
  /// It matters more than an ordinary fidelity gap because blankness is load-bearing:
  /// <c>AfterBlankRows</c>, <c>RowsWhileAnyValue</c> and a repeat's separator all key off it, so a
  /// single such cell in a data column can quietly truncate a region rather than fail loudly. The
  /// <c>.xls</c> path is unaffected — it reports an error code, and an unrecognised one lexes to
  /// <see cref="Unrect.Core.CellError.Other"/> carrying its literal. See
  /// <c>docs/design/vendor-type-survey.md</c> §8.4.
  /// </para>
  /// </summary>
  public class SpreadsheetSpace : ISpace
  {
    private SpreadsheetSpace(ISpace innerSpace)
    {
      InnerSpace = innerSpace;
    }

    private ISpace InnerSpace { get; }
    /// <inheritdoc/>
    public CellValue this[int column, int row] => InnerSpace[column, row];

    /// <inheritdoc/>
    public Area Area => InnerSpace.Area;

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area size) => new SpreadsheetSpace(InnerSpace.GetSubspace(offset, size));

    private static readonly Func<CellValue, bool> WhitespaceIsBlank =
      value => value.TryGetString() is string text && string.IsNullOrWhiteSpace(text);

    /// <summary>
    /// The named sheet of <paramref name="path"/>, with blankness decided by
    /// <paramref name="isBlank"/> — see the sibling overload for what the default does.
    /// </summary>
    public static SpreadsheetSpace Create(
      string path,
      string sheetName,
      bool caseSensitive = false,
      Func<CellValue, bool>? isBlank = null) =>
      Create(
        path,
        c => caseSensitive ? sheetName == c.Name : sheetName.Equals(c.Name, StringComparison.OrdinalIgnoreCase),
        isBlank)
      .FirstOrDefault()
      // Named, because "sequence contains no elements" tells a caller nothing about the workbook
      // they opened or the name they asked for.
      ?? throw new ArgumentException($"No sheet named '{sheetName}' in '{path}'.", nameof(sheetName));

    /// <summary>
    /// Every sheet of <paramref name="path"/> matching <paramref name="predicate"/>.
    /// <para>
    /// Blankness belongs to the adapter, so <paramref name="isBlank"/> decides which cells count as
    /// empty space for the strategies downstream. The default treats whitespace-only text as blank:
    /// exported workbooks are full of <c>"  "</c> cells that look empty, are meant to be empty, and
    /// would otherwise anchor a region. Pass <c>_ => false</c> for strict fidelity, where only
    /// genuinely absent cells are blank. Fidelity has one floor: the adapter maps absent cells and
    /// empty-string cells to Blank before this predicate runs, so no predicate can distinguish
    /// <c>""</c> from a cell that does not exist.
    /// </para>
    /// <para>
    /// The default cannot blank an error cell, because an error is not text — which is the right
    /// outcome: <c>#REF!</c> is something the sheet says, not empty space to be skipped.
    /// </para>
    /// <para>
    /// A sheet whose reader will not say how big it is is measured by being read, so it comes back as
    /// tall as the rows that arrive and as wide as the widest of them — the same extent
    /// <see cref="Workbook.Sheet"/> gives for the same sheet. It costs those rows in memory while the
    /// grid is built, and only for such a sheet.
    /// </para>
    /// </summary>
    public static IEnumerable<SpreadsheetSpace> Create(
      string path,
      Func<SpreadsheetContext, bool> predicate,
      Func<CellValue, bool>? isBlank = null)
    {
      SpreadsheetEncodings.Register();

      var blank = isBlank ?? WhitespaceIsBlank;

      // FileShare.ReadWrite: the workbook may be open in Excel (which holds a write handle), and
      // concurrent readers of the same file must not block each other. FileShare.Delete: Excel
      // saves by writing a temporary file and replacing the original, which an open read handle
      // would otherwise block. A workbook replaced mid-read surfaces as a zip or CRC read failure
      // from the reader below, not as silently wrong cells.
      using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      // Auto-detect format, supports:
      //  - Binary Excel files (2.0-2003 format; *.xls)
      //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
      using var reader = ExcelReaderFactory.CreateReader(stream);

      var sheetIndex = -1;
      do
      {
        sheetIndex++;
        var context = new SpreadsheetContext(sheetIndex, reader.Name);

        if (!predicate(context))
          continue;

        // A sheet that declines to report its extent is measured by reading it — the same answer the
        // streaming door gives, rather than the empty space a grid sized from nothing would be.
        var cells = reader.RowCount > 0
          ? ReadDeclared(reader, blank)
          : ReadMeasured(reader, blank);

        yield return new SpreadsheetSpace(new GridSpace(cells));

      } while (reader.NextResult());
    }

    /// <summary>
    /// The sheet at the size the reader gave, filled row by row. A sheet that yields fewer rows or
    /// narrower ones than it claimed keeps the size it claimed; the cells nothing reached are blank.
    /// </summary>
    private static CellValue[,] ReadDeclared(IExcelDataReader reader, Func<CellValue, bool> blank)
    {
      var rowCount = reader.RowCount;
      var fieldCount = reader.FieldCount;

      // Already blank: default(CellValue) is Blank, so a short row leaves the cells it never
      // reached exactly as they should be, with no fill pass over the sheet.
      var cells = new CellValue[rowCount, fieldCount];

      var row = 0;
      while (row < rowCount && reader.Read())
      {
        var columnCount = Math.Min(fieldCount, reader.FieldCount);
        for (int i = 0; i < columnCount; i++)
          cells[row, i] = Adapt(reader, i, blank);

        row++;
      }

      return cells;
    }

    /// <summary>
    /// The sheet as reading it turns out to be — for a reader that will not say how big it is, which
    /// is what a sheet with no <c>dimension</c> element and no valued cell to infer one from amounts
    /// to.
    /// <para>
    /// The extent is the one the streaming door measures for the same sheet: as tall as the rows that
    /// actually arrive, as wide as the widest of them, and blank where a shorter row ran out. Sizing
    /// a grid from a count of nothing instead would yield an empty space for a sheet with rows in it,
    /// and say nothing about having done so — which is the one outcome an adapter must not have.
    /// </para>
    /// <para>
    /// Each row is asked its own width rather than the sheet's, because a reader that was never told
    /// the extent may only learn it as rows go past. It costs the rows in memory, which the declared
    /// path does not: nothing here knows how tall the sheet is until it ends. (No real file can
    /// exercise a per-row-varying width today — the only reader state that reaches this path also
    /// reports every row zero wide — so the rule is forward-proofing mirrored from
    /// <c>Workbook.Measure</c>; the width-learned-from-rows behaviour is pinned through the streaming
    /// door's fakes. The caveat is recorded in full in <c>SpreadsheetSpaceTests</c>.)
    /// </para>
    /// </summary>
    private static CellValue[,] ReadMeasured(IExcelDataReader reader, Func<CellValue, bool> blank)
    {
      var rows = new List<CellValue[]>();
      var width = 0;

      while (reader.Read())
      {
        var values = new CellValue[reader.FieldCount];
        for (int i = 0; i < values.Length; i++)
          values[i] = Adapt(reader, i, blank);

        rows.Add(values);
        width = Math.Max(width, values.Length);
      }

      var cells = new CellValue[rows.Count, width];
      for (int row = 0; row < rows.Count; row++)
        for (int column = 0; column < rows[row].Length; column++)
          cells[row, column] = rows[row][column];

      return cells;
    }

    /// <summary>
    /// One cell of the reader's current row, canonical — which is where blankness is decided. Shared
    /// by both fill paths, so a sheet that reported its extent and one that had to be measured cannot
    /// disagree about what a cell is.
    /// </summary>
    private static CellValue Adapt(IExcelDataReader reader, int column, Func<CellValue, bool> blank)
    {
      var value = reader.GetCellValue(column);

      return blank(value) ? CellValue.Blank : value;
    }
  }
}
