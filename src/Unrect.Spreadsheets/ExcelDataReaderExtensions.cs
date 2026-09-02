using ExcelDataReader;

using System;

using Unrect.Core;

// Both namespaces spell this type; the aliases keep which is which unmistakable.
using CellError = Unrect.Core.CellError;
using ExcelError = ExcelDataReader.CellError;

namespace Unrect.Spreadsheets
{
  internal static class ExcelDataReaderExtensions
  {
    // This adapter's blankness default: a null or empty cell is an empty cell.
    internal static CellValue GetCellValue(this IExcelDataReader dataReader, int index)
    {
      // Errors first: an error cell has no value to read, so GetValue reports it as null and it
      // would otherwise be adapted into a Blank — a missing cell, which is not what it is.
      if (dataReader.GetCellError(index) is ExcelError error)
        return Adapt(error);

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
        // An elapsed-time cell — built-in number format 46 ([h]:mm:ss), 79, or any custom [h]/[m]/[s]
        // format. A duration is not an instant, so it cannot honestly lex to Temporal; it lexes to a
        // Number of days, the unit every serial-based format already agrees on. Lossless by
        // construction: the reader produced this TimeSpan as TimeSpan.FromDays(serial), so TotalDays
        // hands back the serial it started from. (FromDays rounds to the nearest millisecond, so the
        // round trip is not bit-exact; that loss is the reader's and is one more argument for a
        // first-party OOXML reader behind the native-payload seam.)
        TimeSpan value => CellValue.Of(value.TotalDays),
        var value => throw new InvalidOperationException($"Unsupported cell type {value.GetType()}.")
      };
    }

    /// <summary>
    /// The reader's error vocabulary, adapted. An error this adapter cannot name is
    /// <see cref="Unrect.Core.CellError.Other"/> carrying whatever the reader called it — never an exception:
    /// on the .xls path the reader casts a raw byte to its enum, so an undefined code is a file
    /// this library should still be able to read.
    /// <para>
    /// The literal is the enum value's own text because that is all the reader exposes here. On the
    /// .xlsx path an unrecognised literal never reaches us at all — see
    /// <see cref="SpreadsheetSpace"/> for that limitation and what it costs.
    /// </para>
    /// </summary>
    private static CellValue Adapt(ExcelError error) =>
      error switch
      {
        ExcelError.NULL => CellValue.OfError(CellError.Null),
        ExcelError.DIV0 => CellValue.OfError(CellError.DivisionByZero),
        ExcelError.VALUE => CellValue.OfError(CellError.Value),
        ExcelError.REF => CellValue.OfError(CellError.Reference),
        ExcelError.NAME => CellValue.OfError(CellError.Name),
        ExcelError.NUM => CellValue.OfError(CellError.Number),
        ExcelError.NA => CellValue.OfError(CellError.NotAvailable),
        ExcelError.GETTING_DATA => CellValue.OfError(CellError.GettingData),
        _ => CellValue.OfError(CellError.Other, error.ToString())
      };
  }
}
