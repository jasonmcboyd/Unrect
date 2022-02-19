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
  public class SpreadsheetSpace : ISpace<SpreadsheetValueBase>
  {
    private SpreadsheetSpace(ISpace<SpreadsheetValueBase> innerSpace)
    {
      InnerSpace = innerSpace;
    }

    private ISpace<SpreadsheetValueBase> InnerSpace { get; }
    public SpreadsheetValueBase this[uint column, uint row] => InnerSpace[column, row];
    public Area Area => InnerSpace.Area;
    public ISpace<SpreadsheetValueBase> GetSubspace(Offset offset, Area size) => new SpreadsheetSpace(InnerSpace.GetSubspace(offset, size));

    private static void RegisterEncoding()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static SpreadsheetSpace Create(string path, string sheetName, bool caseSensitive = false) =>
      Create(path, c => caseSensitive ? sheetName == c.Name : sheetName.Equals(c.Name, StringComparison.OrdinalIgnoreCase)).First();

    public static IEnumerable<SpreadsheetSpace> Create(string path, Func<SpreadsheetContext, bool> predicate)
    {
      RegisterEncoding();

      using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
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

        var array = new SpreadsheetValueBase[reader.RowCount, reader.FieldCount];
        var space = new ArraySpace<SpreadsheetValueBase>(array);

        var row = 0;
        while (reader.Read())
        {
          for (int i = 0; i < reader.FieldCount; i++)
          {
            array[row, i] = reader.GetSpreadsheetValue(i);
          }

          row++;
        }

        yield return new SpreadsheetSpace(space);

      } while (reader.NextResult());
    }
  }
}
