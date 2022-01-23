using System;

namespace Unrect.Excel
{
  public readonly struct DoubleSpreadsheetValue : ISpreadsheetValue
  {
    public DoubleSpreadsheetValue(double value)
    {
      TypedValue = value;
    }

    private double TypedValue { get; }
    public object? Value => TypedValue;

    public bool HasValue => true;

    private static readonly Type _ValueType = typeof(double);
    public Type GetValueType() => _ValueType;

    public DateTime GetDateTime() => throw new InvalidOperationException("Incorrect type.");
    public double GetDouble() => TypedValue;
    public int GetInt() => throw new InvalidOperationException("Incorrect type.");
    public string GetString() => throw new InvalidOperationException("Incorrect type.");
    public string? TryGetString() => null;
  }
}
