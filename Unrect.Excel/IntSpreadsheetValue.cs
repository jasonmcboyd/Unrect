using System;

namespace Unrect.Excel
{
  public readonly struct IntSpreadsheetValue : ISpreadsheetValue
  {
    public IntSpreadsheetValue(int value)
    {
      TypedValue = value;
    }

    private int TypedValue { get; }
    public object? Value => TypedValue;

    public bool HasValue => true;

    private static readonly Type _ValueType = typeof(int);
    public Type GetValueType() => _ValueType;

    public DateTime GetDateTime() => throw new InvalidOperationException("Incorrect type.");
    public double GetDouble() => throw new InvalidOperationException("Incorrect type.");
    public int GetInt() => TypedValue;
    public string GetString() => throw new InvalidOperationException("Incorrect type.");
  }
}
