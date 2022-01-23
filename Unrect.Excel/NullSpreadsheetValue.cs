using System;

namespace Unrect.Excel
{
  public readonly struct NullSpreadsheetValue : ISpreadsheetValue
  {

    public object? Value => null;
    public bool HasValue => false;

    public Type GetValueType() => throw new InvalidOperationException("Not value");

    public DateTime GetDateTime() => throw new InvalidOperationException("Incorrect type.");
    public double GetDouble() => throw new InvalidOperationException("Incorrect type.");
    public int GetInt() => throw new InvalidOperationException("Incorrect type.");
    public string GetString() => throw new InvalidOperationException("Incorrect type.");
    public string? TryGetString() => null;
  }
}
