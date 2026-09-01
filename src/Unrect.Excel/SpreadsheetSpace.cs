using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unrect.Array;
using Unrect.Core;

namespace Unrect.Excel
{
  public class SpreadsheetSpace : ISpace
  {
    private SpreadsheetSpace(ISpace innerSpace)
    {
      InnerSpace = innerSpace;
    }

    private ISpace InnerSpace { get; }
    public CellValue this[int column, int row] => InnerSpace[column, row];
    public Area Area => InnerSpace.Area;
    public ISpace GetSubspace(Offset offset, Area size) => new SpreadsheetSpace(InnerSpace.GetSubspace(offset, size));

    private static readonly Func<CellValue, bool> WhitespaceIsBlank =
      value => value.TryGetString() is string text && string.IsNullOrWhiteSpace(text);

    private static void RegisterEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

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
      .First();

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
    /// </summary>
    public static IEnumerable<SpreadsheetSpace> Create(
      string path,
      Func<SpreadsheetContext, bool> predicate,
      Func<CellValue, bool>? isBlank = null)
    {
      RegisterEncoding();

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

        var rowCount = reader.RowCount;
        var fieldCount = reader.FieldCount;

        var cells = new CellValue[rowCount, fieldCount];
        for (int i = 0; i < rowCount; i++)
          for (int j = 0; j < fieldCount; j++)
            cells[i, j] = CellValue.Blank;

        var row = 0;
        while (row < rowCount && reader.Read())
        {
          var columnCount = Math.Min(fieldCount, reader.FieldCount);
          for (int i = 0; i < columnCount; i++)
          {
            var value = reader.GetCellValue(i);
            cells[row, i] = blank(value) ? CellValue.Blank : value;
          }

          row++;
        }

        yield return new SpreadsheetSpace(new ArraySpace(cells));

      } while (reader.NextResult());
    }
  }
}
