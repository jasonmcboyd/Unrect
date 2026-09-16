using ExcelDataReader;

using System;

// Both namespaces spell this type; the alias keeps which is which unmistakable.
using ExcelError = ExcelDataReader.CellError;

namespace Unrect.Spreadsheets
{
  internal static class ExcelDataReaderExtensions
  {
    // This adapter's blankness default: a null or empty cell is an empty cell.
    internal static Cell GetCellValue(this IExcelDataReader dataReader, int index)
    {
      // Errors first: an error cell has no value to read, so GetValue reports it as null and it
      // would otherwise be adapted into a Blank — a missing cell, which is not what it is.
      if (dataReader.GetCellError(index) is ExcelError error)
        return Adapt(error);

      return dataReader.GetValue(index) switch
      {
        null => Cell.Blank,
        double value => Cell.Of(value),
        DateTime value => Cell.Of(value),
        bool value => Cell.Of(value),
        int value => Cell.Of(value),
        long value => Cell.Of(value),
        float value => Cell.Of((double)value),
        string value => string.IsNullOrEmpty(value) ? Cell.Blank : Cell.Of(value),
        // An elapsed-time cell — built-in number format 46 ([h]:mm:ss), 79, or any custom [h]/[m]/[s]
        // format. A duration is not an instant, so it cannot honestly lex to Temporal; it lexes to a
        // Number of days, the unit every serial-based format already agrees on. Lossless by
        // construction: the reader produced this TimeSpan as TimeSpan.FromDays(serial), so TotalDays
        // hands back the serial it started from. (FromDays rounds to the nearest millisecond, so the
        // round trip is not bit-exact; that loss is the reader's and is one more argument for a
        // first-party OOXML reader behind the native-payload seam.)
        TimeSpan value => Cell.Of(value.TotalDays),
        var value => throw new InvalidOperationException($"Unsupported cell type {value.GetType()}.")
      };
    }

    /// <summary>
    /// The reader's error vocabulary, adapted. An error this adapter cannot name is
    /// <see cref="CellError.Other"/> carrying whatever the reader called it — never an exception:
    /// on the .xls path the reader casts a raw byte to its enum, so an undefined code is a file
    /// this library should still be able to read.
    /// <para>
    /// The literal is the enum value's own text because that is all the reader exposes here. On the
    /// .xlsx path an unrecognised literal never reaches us at all — see
    /// <see cref="SpreadsheetSpace"/> for that limitation and what it costs.
    /// </para>
    /// </summary>
    private static Cell Adapt(ExcelError error) =>
      error switch
      {
        ExcelError.NULL => Cell.OfError(CellError.Null),
        ExcelError.DIV0 => Cell.OfError(CellError.DivisionByZero),
        ExcelError.VALUE => Cell.OfError(CellError.Value),
        ExcelError.REF => Cell.OfError(CellError.Reference),
        ExcelError.NAME => Cell.OfError(CellError.Name),
        ExcelError.NUM => Cell.OfError(CellError.Number),
        ExcelError.NA => Cell.OfError(CellError.NotAvailable),
        ExcelError.GETTING_DATA => Cell.OfError(CellError.GettingData),
        _ => Cell.OfError(CellError.Other, error.ToString())
      };
  }
}
