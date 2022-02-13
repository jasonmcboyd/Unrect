using System;

namespace Unrect.Excel
{
  public abstract class SpreadsheetValueBase // : IEquatable<SpreadsheetValue>, IEquatable<DateTime>, IEquatable<double>, IEquatable<int>, IEquatable<string>
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
        DateTime value => value == TryGetDateTime().Value,
        double value => value == TryGetDouble().Value,
        int value => value == TryGetInt().Value,
        string value => value == TryGetString(),
        _ => base.Equals(obj)
      };
    }
  }
}
