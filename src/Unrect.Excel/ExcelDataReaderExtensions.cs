using ExcelDataReader;

using System;

using Unrect.Core;

// Both namespaces spell this type; the aliases keep which is which unmistakable.
using CellError = Unrect.Core.CellError;
using ExcelError = ExcelDataReader.CellError;

namespace Unrect.Excel
{
  internal static class ExcelDataReaderExtensions
  {
    // This adapter's blankness default: a null or empty cell is an empty cell.
    internal static CellValue GetCellValue(this IExcelDataReader dataReader, int index)
    {
      // Errors first: an error cell has no value to read, so GetValue reports it as null and it
      // would otherwise be adapted into a Blank — a missing cell, which is not what it is.
      if (dataReader.GetCellError(index) is ExcelError error)
        return CellValue.OfError(Adapt(error));

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

    private static CellError Adapt(ExcelError error) =>
      error switch
      {
        ExcelError.NULL => CellError.Null,
        ExcelError.DIV0 => CellError.DivisionByZero,
        ExcelError.VALUE => CellError.Value,
        ExcelError.REF => CellError.Reference,
        ExcelError.NAME => CellError.Name,
        ExcelError.NUM => CellError.Number,
        ExcelError.NA => CellError.NotAvailable,
        ExcelError.GETTING_DATA => CellError.GettingData,
        _ => throw new InvalidOperationException($"Unsupported cell error {error}.")
      };
  }
}
