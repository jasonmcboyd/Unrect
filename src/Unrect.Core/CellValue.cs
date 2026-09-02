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

    private CellValue(CellError error, string? errorText)
    {
      Kind = CellKind.Error;
      Error = error;
      ErrorText = errorText;
    }

    public static CellValue Blank { get; } = new CellValue();

    public static CellValue Of(string? value) => value is null ? Blank : new CellValue(value);
    public static CellValue Of(int value) => new CellValue(value, value);
    public static CellValue Of(long value) => new CellValue(value, value);
    public static CellValue Of(double value) => new CellValue(value, null);
    public static CellValue Of(decimal value) => new CellValue((double)value, value);
    public static CellValue Of(DateTime value) => new CellValue(value);
    public static CellValue Of(bool value) => new CellValue(value);

    /// <summary>
    /// A cell holding a spreadsheet error. An error is something the cell says, so an error cell
    /// has a value and is never blank — it must not be skippable as empty space.
    /// <para>
    /// <paramref name="literal"/> is the text the error arrived as, and matters most when
    /// <paramref name="error"/> is <see cref="CellError.Other"/>: an adapter that meets an error it
    /// cannot name must still be able to say what it saw, or a reader staring at
    /// <c>Error(Other)</c> cannot tell <c>Err:522</c> from <c>#PYTHON!</c>. Pass null — the usual
    /// case — when the canonical spelling is the whole truth.
    /// </para>
    /// </summary>
    public static CellValue OfError(CellError error, string? literal = null)
      // Kept only when it says something the canonical spelling does not, so the ordinary path
      // stores no extra string. A blank literal says nothing at all, and storing one would render
      // as "Error()".
      => new CellValue(error, string.IsNullOrWhiteSpace(literal) || literal == Display(error) ? null : literal);

    public CellKind Kind { get; }
    public bool IsBlank => Kind == CellKind.Blank;
    public bool HasValue => !IsBlank;

    private string? Text { get; }
    private double Number { get; }
    private decimal? ExactNumber { get; }
    private DateTime Temporal { get; }
    private bool Boolean { get; }
    private CellError Error { get; }
    private string? ErrorText { get; }

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
    /// <summary>The date part, or null when the cell is not temporal — the Try twin of <see cref="GetDate"/>.</summary>
    public DateTime? TryGetDate() => TryGetDateTime()?.Date;

    public DateTime GetDate() => GetDateTime().Date;

    public bool? TryGetBoolean() => Kind == CellKind.Boolean ? Boolean : null;
    public bool GetBoolean() => TryGetBoolean() ?? throw WrongKind(CellKind.Boolean);

    public CellError? TryGetError() => Kind == CellKind.Error ? Error : null;
    public CellError GetError() => TryGetError() ?? throw WrongKind(CellKind.Error);

    /// <summary>
    /// The text this error arrived as, when it differs from the canonical spelling of its
    /// <see cref="CellError"/> — otherwise null, including for every cell that is not an error.
    /// Use it to recover what an <see cref="CellError.Other"/> actually was.
    /// </summary>
    public string? TryGetErrorText() => Kind == CellKind.Error ? ErrorText : null;

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
        CellKind.Error => Error == other.Error && ErrorText == other.ErrorText,
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
        CellKind.Error => HashCode.Combine(Error, ErrorText),
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
        CellKind.Error => $"Error({ErrorText ?? Display(Error)})",
        _ => "Blank"
      };

    /// <summary>The canonical spreadsheet spelling of an error, as Excel shows it in the cell.</summary>
    private static string Display(CellError error) =>
      error switch
      {
        CellError.Null => "#NULL!",
        CellError.DivisionByZero => "#DIV/0!",
        CellError.Value => "#VALUE!",
        CellError.Reference => "#REF!",
        CellError.Name => "#NAME?",
        CellError.Number => "#NUM!",
        CellError.NotAvailable => "#N/A",
        CellError.GettingData => "#GETTING_DATA",
        CellError.Spill => "#SPILL!",
        CellError.Calc => "#CALC!",
        CellError.Field => "#FIELD!",
        CellError.Blocked => "#BLOCKED!",
        CellError.Connect => "#CONNECT!",
        CellError.Busy => "#BUSY!",
        CellError.External => "#EXTERNAL!",
        _ => error.ToString()
      };

    // Doubles at or beyond decimal's bounds (and NaN / infinity) have no decimal representation.
    // The bounds are compared strictly because decimal.MaxValue rounds up when widened to double.
    private static bool IsRepresentableAsDecimal(double value)
      => value > (double)decimal.MinValue && value < (double)decimal.MaxValue;

    private InvalidOperationException WrongKind(CellKind expected) => new InvalidOperationException(WrongKindMessage(expected));

    // An error cell says why it has no usable value, so the message says which error it is.
    private string WrongKindMessage(CellKind expected) =>
      Kind == CellKind.Error
      ? $"Cell value is Error ({ErrorText ?? Display(Error)}); expected {expected}."
      : $"Cell value is {Kind}; expected {expected}.";
  }
}
