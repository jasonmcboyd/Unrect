using System;

namespace Unrect.Excel
{
  public readonly struct DateTimeSpreadsheetValue : SpreadsheetValue
  {
    public DateTimeSpreadsheetValue(DateTime value)
    {
      TypedValue = value;
    }

    private DateTime TypedValue { get; }
    public object? Value => TypedValue;

    public bool HasValue => true;

    private static readonly Type _ValueType = typeof(DateTime);
    public Type? GetValueType() => _ValueType;

    public DateTime GetDateTime() => TypedValue;
    public double GetDouble() => throw new InvalidOperationException("Incorrect type.");
    public int GetInt() => throw new InvalidOperationException("Incorrect type.");
    public string GetString() => throw new InvalidOperationException("Incorrect type.");
    public string? TryGetString() => null;

    public bool Equals(SpreadsheetValue? other)
    {
      if (other == null || GetValueType() != other.GetValueType())
        return false;

      return GetDateTime() == other.GetDateTime();
    }

    public override bool Equals(object? obj) => Equals(obj as SpreadsheetValue);

    public override int GetHashCode() => TypedValue.GetHashCode();

    public static bool operator ==(DateTimeSpreadsheetValue lhs, SpreadsheetValue? rhs) => lhs.Equals(rhs);
    public static bool operator ==(SpreadsheetValue? lhs, DateTimeSpreadsheetValue rhs) => rhs.Equals(lhs);
    public static bool operator !=(DateTimeSpreadsheetValue lhs, SpreadsheetValue? rhs) => !lhs.Equals(rhs);
    public static bool operator !=(SpreadsheetValue? lhs, DateTimeSpreadsheetValue rhs) => rhs.Equals(lhs);
  }
}
