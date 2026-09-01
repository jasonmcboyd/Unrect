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

    private static void RegisterEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static SpreadsheetSpace Create(string path, string sheetName, bool caseSensitive = false) =>
      Create(path, c => caseSensitive ? sheetName == c.Name : sheetName.Equals(c.Name, StringComparison.OrdinalIgnoreCase)).First();

    public static IEnumerable<SpreadsheetSpace> Create(string path, Func<SpreadsheetContext, bool> predicate)
    {
      RegisterEncoding();

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
            cells[row, i] = reader.GetCellValue(i);
          }

          row++;
        }

        yield return new SpreadsheetSpace(new ArraySpace(cells));

      } while (reader.NextResult());
    }
  }
}
