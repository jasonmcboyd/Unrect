using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using Unrect.Array;
using Unrect.Core;

namespace Unrect.Excel
{
  public class SpreadsheetSpace : ISpace<object>
  {
    private SpreadsheetSpace(ISpace<object> innerSpace)
    {
      InnerSpace = innerSpace;
    }

    private ISpace<Object> InnerSpace { get; }
    public object this[int column, int row] => InnerSpace[column, row];
    public Size Size => InnerSpace.Size;
    public ISpace<object> GetSubspace(Offset offset, Size size) => new SpreadsheetSpace(InnerSpace.GetSubspace(offset, size));

    public IEnumerable<SpreadsheetSpace> Create(string path, Func<SpreadsheetContext, bool> predicate)
    {
      using (var stream = File.Open(path, FileMode.Open, FileAccess.Read))
      {
        // Auto-detect format, supports:
        //  - Binary Excel files (2.0-2003 format; *.xls)
        //  - OpenXml Excel files (2007 format; *.xlsx, *.xlsb)
        using (var reader = ExcelReaderFactory.CreateReader(stream))
        {
          var sheetIndex = -1;
          do
          {
            sheetIndex++;
            var context = new SpreadsheetContext(sheetIndex, reader.Name);
            
            if (!predicate(context))
              continue;

            var array = new object[reader.FieldCount, reader.RowCount];
            var space = new ArraySpace<object>(array);

            var row = 0;
            while (reader.Read())
            {
              for (int i = 0; i < reader.FieldCount; i++)
              {
                array[i, row] = reader.GetValue(i);
                row++;
              }
            }

            yield return new SpreadsheetSpace(space);

          } while (reader.NextResult());
        }
      }
    }
  }
}
