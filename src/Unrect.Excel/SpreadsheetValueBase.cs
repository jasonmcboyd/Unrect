using System;

namespace Unrect.Excel
{
  public abstract class SpreadsheetValueBase
  {
    public abstract bool HasValue { get; }

    public abstract Type? GetValueType();

    public DateTime GetDateTime() => TryGetDateTime() ?? throw new InvalidOperationException();
    public double GetDouble() => TryGetDouble() ?? throw new InvalidOperationException();
    public int GetInt() => TryGetInt() ?? throw new InvalidOperationException();
    public string GetString() => TryGetString() ?? throw new InvalidOperationException();

    public abstract DateTime? TryGetDateTime();
    public abstract double? TryGetDouble();
    public abstract int? TryGetInt();
    public abstract string? TryGetString();

    public override bool Equals(object? obj)
    {
      return obj switch
      {
        SpreadsheetValueBase value when value.GetValueType() == typeof(DateTime) => value.GetDateTime() == TryGetDateTime(),
        SpreadsheetValueBase value when value.GetValueType() == typeof(double) => value.GetDouble() == TryGetDouble(),
        SpreadsheetValueBase value when value.GetValueType() == typeof(int) => value.GetInt() == TryGetInt(),
        SpreadsheetValueBase value when value.GetValueType() == typeof(string) => value.GetString() == TryGetString(),
        DateTime value => TryGetDateTime() is DateTime dt && value == dt,
        double value => TryGetDouble() is double d && value == d,
        int value => TryGetInt() is int i && value == i,
        string value => value == TryGetString(),
        _ => base.Equals(obj)
      };
    }

    public override int GetHashCode()
    {
      var valueType = GetValueType();
      if (valueType == typeof(DateTime)) return TryGetDateTime().GetHashCode();
      if (valueType == typeof(double)) return TryGetDouble().GetHashCode();
      if (valueType == typeof(int)) return TryGetInt().GetHashCode();
      if (valueType == typeof(string)) return TryGetString()?.GetHashCode() ?? 0;
      return 0;
    }
  }
}
