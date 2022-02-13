using System;

namespace Unrect.Excel
{
  internal class SpreadsheetValue<T> : SpreadsheetValueBase
  {
    public SpreadsheetValue(T value)
    {
      Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    private T Value { get; }

    public override bool HasValue => true;

    public override Type? GetValueType() => typeof(T);

    public override DateTime? TryGetDateTime() => Value is DateTime value ? value : null;
    public override double? TryGetDouble() => Value is double value ? value : null;
    public override int? TryGetInt() => Value is int value ? value : null;
    public override string? TryGetString() => Value is string value ? value.ToString() : null;
  }
}
