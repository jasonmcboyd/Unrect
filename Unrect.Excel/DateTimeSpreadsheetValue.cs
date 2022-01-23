using System;

namespace Unrect.Excel
{
  public readonly struct DateTimeSpreadsheetValue : ISpreadsheetValue
  {
    public DateTimeSpreadsheetValue(DateTime value)
    {
      TypedValue = value;
    }

    private DateTime TypedValue { get; }
    public object? Value => TypedValue;

    public bool HasValue => true;

    private static readonly Type _ValueType = typeof(DateTime);
    public Type GetValueType() => _ValueType;

    public DateTime GetDateTime() => TypedValue;
    public double GetDouble() => throw new InvalidOperationException("Incorrect type.");
    public int GetInt() => throw new InvalidOperationException("Incorrect type.");
    public string GetString() => throw new InvalidOperationException("Incorrect type.");
    public string? TryGetString() => null;
  }
}
