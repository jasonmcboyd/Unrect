using System;

namespace Unrect.Excel
{
  public interface ISpreadsheetValue
  {
    bool HasValue{ get; }
    object? Value { get; }

    Type GetValueType();

    DateTime GetDateTime();
    double GetDouble();
    int GetInt();
    string GetString();
    string? TryGetString();
  }
}
