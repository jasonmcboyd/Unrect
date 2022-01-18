using ExcelDataReader;

using System;

namespace Unrect.Excel
{
  internal static class ExcelDataReaderExtensions
  {
    internal static ISpreadsheetValue GetSpreadsheetValue(this IExcelDataReader dataReader, int index)
    {
      return dataReader.GetValue(index) switch
      {
        DateTime => new DateTimeSpreadsheetValue(dataReader.GetDateTime(index)),
        double => new DoubleSpreadsheetValue(dataReader.GetDouble(index)),
        int => new IntSpreadsheetValue(dataReader.GetInt32(index)),
        string => new StringSpreadsheetValue(dataReader.GetString(index)),
        null => new NullSpreadsheetValue(),
        _ => throw new InvalidOperationException($"{dataReader.GetFieldType(index).Name} is not a supported type.")
      };
    }
  }
}
