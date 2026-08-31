using System;

namespace Unrect.Core
{
  public sealed class CellValue : IEquatable<CellValue>
  {
    private CellValue()
    {
      Kind = CellKind.Blank;
    }

    private CellValue(string text)
    {
      Kind = CellKind.Text;
      Text = text;
    }

    private CellValue(double number, decimal? exactNumber)
    {
      Kind = CellKind.Number;
      Number = number;
      ExactNumber = exactNumber;
    }

    private CellValue(DateTime temporal)
    {
      Kind = CellKind.Temporal;
      Temporal = temporal;
    }

    private CellValue(bool boolean)
    {
      Kind = CellKind.Boolean;
      Boolean = boolean;
    }

    public static CellValue Blank { get; } = new CellValue();

    public static CellValue Of(string? value) => value is null ? Blank : new CellValue(value);
    public static CellValue Of(int value) => new CellValue(value, value);
    public static CellValue Of(long value) => new CellValue(value, value);
    public static CellValue Of(double value) => new CellValue(value, null);
    public static CellValue Of(decimal value) => new CellValue((double)value, value);
    public static CellValue Of(DateTime value) => new CellValue(value);
    public static CellValue Of(bool value) => new CellValue(value);

    public CellKind Kind { get; }
    public bool IsBlank => Kind == CellKind.Blank;
    public bool HasValue => !IsBlank;

    private string? Text { get; }
    private double Number { get; }
    private decimal? ExactNumber { get; }
    private DateTime Temporal { get; }
    private bool Boolean { get; }

    public string? TryGetString() => Kind == CellKind.Text ? Text : null;
    public string GetString() => TryGetString() ?? throw WrongKind(CellKind.Text);

    public double? TryGetDouble() => Kind == CellKind.Number ? Number : null;
    public double GetDouble() => TryGetDouble() ?? throw WrongKind(CellKind.Number);

    public decimal? TryGetDecimal()
    {
      if (Kind != CellKind.Number)
        return null;

      return ExactNumber ?? (IsRepresentableAsDecimal(Number) ? (decimal)Number : null);
    }

    public decimal GetDecimal() =>
      TryGetDecimal()
      ?? throw new InvalidOperationException(
        Kind == CellKind.Number
        ? $"Cell value {Number} is not representable as a {nameof(Decimal)}."
        : WrongKindMessage(CellKind.Number));

    public int? TryGetInt()
    {
      if (Kind != CellKind.Number)
        return null;

      return Number >= int.MinValue && Number <= int.MaxValue && Math.Floor(Number) == Number
        ? (int)Number
        : null;
    }

    public int GetInt() =>
      TryGetInt()
      ?? throw new InvalidOperationException(
        Kind == CellKind.Number
        ? $"Cell value {Number} is not an integer within the range of {nameof(Int32)}."
        : WrongKindMessage(CellKind.Number));

    public DateTime? TryGetDateTime() => Kind == CellKind.Temporal ? Temporal : null;
    public DateTime GetDateTime() => TryGetDateTime() ?? throw WrongKind(CellKind.Temporal);
    public DateTime GetDate() => GetDateTime().Date;

    public bool? TryGetBoolean() => Kind == CellKind.Boolean ? Boolean : null;
    public bool GetBoolean() => TryGetBoolean() ?? throw WrongKind(CellKind.Boolean);

    /// <summary>
    /// Two cell values are equal when they share a kind and an equal payload. Numbers compare on
    /// their double representation, so <c>Of(1m)</c> equals <c>Of(1.0)</c> even though
    /// <see cref="GetDecimal"/> may report a different precision for each. Number comparison uses
    /// <see cref="double.Equals(double)"/>, so NaN equals NaN — equality is reflexive and
    /// hash-consistent, deviating from IEEE <c>==</c> on purpose. Equality is for matching
    /// cells; <see cref="GetDecimal"/> is for extracting values.
    /// </summary>
    public bool Equals(CellValue? other)
    {
      if (other is null)
        return false;
      if (ReferenceEquals(this, other))
        return true;
      if (Kind != other.Kind)
        return false;

      return Kind switch
      {
        CellKind.Text => Text == other.Text,
        CellKind.Number => Number.Equals(other.Number),
        CellKind.Temporal => Temporal == other.Temporal,
        CellKind.Boolean => Boolean == other.Boolean,
        _ => true
      };
    }

    public override bool Equals(object? obj) => Equals(obj as CellValue);

    public override int GetHashCode()
    {
      var payload = Kind switch
      {
        CellKind.Text => Text!.GetHashCode(),
        CellKind.Number => Number.GetHashCode(),
        CellKind.Temporal => Temporal.GetHashCode(),
        CellKind.Boolean => Boolean.GetHashCode(),
        _ => 0
      };

      return HashCode.Combine(Kind, payload);
    }

    public static bool operator ==(CellValue? first, CellValue? second) => first?.Equals(second) ?? second is null;
    public static bool operator !=(CellValue? first, CellValue? second) => !(first == second);

    /// <summary>
    /// A diagnostic rendering of the kind and payload, not display output. Formatting a cell for
    /// presentation is the consumer's job, at the map site where the value's meaning is known.
    /// </summary>
    public override string ToString() =>
      Kind switch
      {
        CellKind.Text => $"Text({Text})",
        CellKind.Number => $"Number({Number})",
        CellKind.Temporal => $"Temporal({Temporal})",
        CellKind.Boolean => $"Boolean({Boolean})",
        _ => "Blank"
      };

    // Doubles at or beyond decimal's bounds (and NaN / infinity) have no decimal representation.
    // The bounds are compared strictly because decimal.MaxValue rounds up when widened to double.
    private static bool IsRepresentableAsDecimal(double value)
      => value > (double)decimal.MinValue && value < (double)decimal.MaxValue;

    private InvalidOperationException WrongKind(CellKind expected) => new InvalidOperationException(WrongKindMessage(expected));

    private string WrongKindMessage(CellKind expected) => $"Cell value is {Kind}; expected {expected}.";
  }
}
