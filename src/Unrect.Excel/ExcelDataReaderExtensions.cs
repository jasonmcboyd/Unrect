using ExcelDataReader;

using System;

namespace Unrect.Excel
{
  internal static class ExcelDataReaderExtensions
  {
    internal static SpreadsheetValueBase GetSpreadsheetValue(this IExcelDataReader dataReader, int index)
    {
      return dataReader.GetValue(index) switch
      {
        DateTime => new SpreadsheetValue<DateTime>(dataReader.GetDateTime(index)),
        double => new SpreadsheetValue<double>(dataReader.GetDouble(index)),
        int => new SpreadsheetValue<int>(dataReader.GetInt32(index)),
        string => new SpreadsheetValue<string>(dataReader.GetString(index)),
        null => NullSpreadsheetValue.Instance,
        _ => throw new InvalidOperationException($"{dataReader.GetFieldType(index).Name} is not a supported type.")
      };
    }
  }
}
