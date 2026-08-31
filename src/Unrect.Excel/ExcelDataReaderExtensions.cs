using ExcelDataReader;

using System;

using Unrect.Core;

namespace Unrect.Excel
{
  internal static class ExcelDataReaderExtensions
  {
    // This adapter's blankness default: a null or empty cell is an empty cell.
    internal static CellValue GetCellValue(this IExcelDataReader dataReader, int index)
    {
      return dataReader.GetValue(index) switch
      {
        null => CellValue.Blank,
        double value => CellValue.Of(value),
        DateTime value => CellValue.Of(value),
        bool value => CellValue.Of(value),
        int value => CellValue.Of(value),
        long value => CellValue.Of(value),
        float value => CellValue.Of((double)value),
        string value => string.IsNullOrEmpty(value) ? CellValue.Blank : CellValue.Of(value),
        TimeSpan => throw new InvalidOperationException("TimeSpan cell values are not yet supported; a Duration kind is an open design question."),
        var value => throw new InvalidOperationException($"Unsupported cell type {value.GetType()}.")
      };
    }
  }
}
