using System;
using System.Globalization;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The value at one cell of a sheet: exactly one of a closed set of cases — text, a number, a
  /// date, a boolean, an error, or nothing — and the payload of that case. A sum type, spelled the
  /// way C# can spell one without allocating: <see cref="Kind"/> is the tag, one <c>TryGet</c> per
  /// case hands back its payload, and a switch over the tag is exhaustive because the cases
  /// partition the cell. (A switch over a <em>point</em> never is: a point may also have a formula,
  /// a font and a fill, none of which is a case of the value.)
  /// <para>
  /// Faithful to the store. An <c>.xlsx</c> cell is tagged number, string, boolean or error, or is
  /// absent, and every number in it is a double; a date is a double the format shows as one, and
  /// this vocabulary lexes it as its own case on purpose, so a number read refuses a date. There is
  /// no integer, decimal or currency here because there is none in the file: those are conversions
  /// a reader asks for, above the space.
  /// </para>
  /// <para>
  /// Small on purpose: a sheet is an array of these and a million-row workbook holds millions.
  /// Three fields carry every case — the tag, eight bytes of payload, and one reference for the two
  /// cases that need a string — so a number, a date, a boolean and a blank cost nothing beyond
  /// their slot. <c>default(CellValue)</c> is <see cref="Blank"/>, which is what makes a freshly
  /// allocated array an already-blank sheet.
  /// </para>
  /// </summary>
  public readonly struct CellValue : IEquatable<CellValue>
  {
    // Ticks occupy the low 62 bits of a DateTime and its DateTimeKind the top two — the runtime's
    // own encoding, replicated here so a date packs into the payload word without losing the Kind
    // a caller reads back.
    private const long TicksMask = 0x3FFFFFFFFFFFFFFF;

    private readonly CellKind _kind;

    // The payload that fits in a word: a double's bits, a packed DateTime, a boolean as 0/1, or an
    // error's code. Unused, and zero, for Blank and Text.
    private readonly long _value;

    // The payload that does not: a Text cell's string, or an Error cell's literal when it differs
    // from the canonical spelling. Null whenever the case has nothing to hang here.
    private readonly string? _text;

    private CellValue(CellKind kind, long value, string? text)
    {
      _kind = kind;
      _value = value;
      _text = text;
    }

    /// <summary>The blank value — <see cref="CellKind.Blank"/> carries no payload, so nothing distinguishes two blanks.</summary>
    public static CellValue Blank => default;

    /// <summary>A <see cref="CellKind.Text"/> value, or <see cref="Blank"/> when <paramref name="text"/> is null.</summary>
    public static CellValue Of(string? text) => text is null ? Blank : new CellValue(CellKind.Text, 0L, text);

    /// <summary>
    /// A <see cref="CellKind.Number"/> value. The only numeric case there is: an integer literal
    /// converts on the way in, and a fixture written with a decimal literal converts explicitly,
    /// because a sheet holds neither.
    /// </summary>
    public static CellValue Of(double number) => new CellValue(CellKind.Number, BitConverter.DoubleToInt64Bits(number), null);

    /// <summary>A <see cref="CellKind.Temporal"/> value, its time of day kept.</summary>
    public static CellValue Of(DateTime moment) => new CellValue(CellKind.Temporal, moment.Ticks | ((long)moment.Kind << 62), null);

    /// <summary>A <see cref="CellKind.Boolean"/> value.</summary>
    public static CellValue Of(bool flag) => new CellValue(CellKind.Boolean, flag ? 1L : 0L, null);

    /// <summary>
    /// A spreadsheet error. An error is something the cell says, so it is a value and never blank —
    /// a strategy looking for the end of a region must not step over it as empty space.
    /// <para>
    /// <paramref name="literal"/> is the text the error arrived as, and matters most when
    /// <paramref name="error"/> is <see cref="CellError.Other"/>: an adapter that meets an error it
    /// cannot name must still be able to say what it saw, or a reader staring at
    /// <c>Error(Other)</c> cannot tell <c>Err:522</c> from <c>#PYTHON!</c>. Pass null — the usual
    /// case — when the canonical spelling is the whole truth; a literal that only repeats it, or
    /// says nothing, is not kept.
    /// </para>
    /// </summary>
    public static CellValue OfError(CellError error, string? literal = null)
      => new CellValue(
        CellKind.Error,
        (long)error,
        string.IsNullOrWhiteSpace(literal) || literal == Display(error) ? null : literal);

    /// <summary>Which case this value is — the tag of the sum type, and the one thing a switch turns on.</summary>
    public CellKind Kind => _kind;

    /// <summary>The text, when this value is <see cref="CellKind.Text"/>.</summary>
    /// <param name="text">The cell's own string, when the answer is true.</param>
    public bool TryGetText(out string text)
    {
      text = (_kind == CellKind.Text ? _text : null)!;
      return _kind == CellKind.Text;
    }

    /// <summary>The number, when this value is <see cref="CellKind.Number"/> — the double the sheet holds.</summary>
    /// <param name="number">The number, when the answer is true.</param>
    public bool TryGetNumber(out double number)
    {
      number = _kind == CellKind.Number ? BitConverter.Int64BitsToDouble(_value) : default;
      return _kind == CellKind.Number;
    }

    /// <summary>The date or time, verbatim, when this value is <see cref="CellKind.Temporal"/>. Truncating is the caller's.</summary>
    /// <param name="moment">The date and time, when the answer is true.</param>
    public bool TryGetDate(out DateTime moment)
    {
      moment = _kind == CellKind.Temporal ? Temporal : default;
      return _kind == CellKind.Temporal;
    }

    /// <summary>The boolean, when this value is <see cref="CellKind.Boolean"/>.</summary>
    /// <param name="flag">The boolean, when the answer is true.</param>
    public bool TryGetBoolean(out bool flag)
    {
      flag = _kind == CellKind.Boolean && _value != 0L;
      return _kind == CellKind.Boolean;
    }

    /// <summary>
    /// The error, when this value is <see cref="CellKind.Error"/>. How the file spelled it is what
    /// <see cref="AsText"/> says.
    /// </summary>
    /// <param name="error">The error, when the answer is true.</param>
    public bool TryGetError(out CellError error)
    {
      error = _kind == CellKind.Error ? (CellError)_value : default;
      return _kind == CellKind.Error;
    }

    private DateTime Temporal => new DateTime(_value & TicksMask, (DateTimeKind)((_value >> 62) & 3L));

    /// <summary>
    /// What the cell says: a text's own string, a number's digits, a date's ISO form, <c>TRUE</c>
    /// or <c>FALSE</c>, an error's spreadsheet literal. Null exactly when the value is
    /// <see cref="Blank"/>.
    /// <para>
    /// A rendering and not a reading: it is invariant, carries no trace of the format the backend
    /// displayed, and that a cell says "42" does not mean it holds the text "42". A number renders
    /// as the shortest digits that read back exactly, found the same way on both targets, so
    /// 0.1 + 0.2 says 0.30000000000000004 through either. Two residuals worth knowing: a negative
    /// zero says <c>-0</c> on .NET Core and <c>0</c> on .NET Framework, and a large enough magnitude
    /// says <c>1E+20</c> rather than its digits.
    /// </para>
    /// </summary>
    public string? AsText() =>
      _kind switch
      {
        CellKind.Text => _text,
        CellKind.Number => Renderings.ShortestRoundTrip(BitConverter.Int64BitsToDouble(_value)),
        // A date says its date; a moment within a day says the time too, rather than silently
        // rendering as the midnight it is not. Sub-second digits are carried only when there are
        // some, so a whole second still says hh:mm:ss.
        CellKind.Temporal => Temporal.ToString(
          Temporal.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
          CultureInfo.InvariantCulture),
        CellKind.Boolean => _value != 0L ? "TRUE" : "FALSE",
        CellKind.Error => _text ?? Display((CellError)_value),
        _ => null
      };

    /// <summary>
    /// Two values are equal when they are the same case with an equal payload. Numbers compare as
    /// doubles through <see cref="double.Equals(double)"/>, so NaN equals NaN — equality is
    /// reflexive and hash-consistent, deviating from IEEE <c>==</c> on purpose — and an error's
    /// kept literal counts, since two unnamed errors spelled differently are different cells.
    /// <para>
    /// Public, and the interface with it, for one concrete reason: without
    /// <see cref="IEquatable{T}"/> every comparison through
    /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/> boxes both values —
    /// which a streamed sheet and the retention rig feel, since they hold them by the million.
    /// </para>
    /// </summary>
    public bool Equals(CellValue other)
    {
      if (_kind != other._kind)
        return false;

      return _kind switch
      {
        CellKind.Text => _text == other._text,
        CellKind.Number => BitConverter.Int64BitsToDouble(_value).Equals(BitConverter.Int64BitsToDouble(other._value)),
        CellKind.Error => _value == other._value && _text == other._text,
        _ => _value == other._value
      };
    }

    /// <summary>Equality against any object — see <see cref="Equals(CellValue)"/> when the other is a <see cref="CellValue"/>.</summary>
    public override bool Equals(object? obj) => obj is CellValue other && Equals(other);

    /// <summary>Consistent with <see cref="Equals(CellValue)"/>: the case and its payload, numbers as doubles.</summary>
    public override int GetHashCode()
    {
      var payload = _kind switch
      {
        CellKind.Text => _text!.GetHashCode(),
        CellKind.Number => BitConverter.Int64BitsToDouble(_value).GetHashCode(),
        CellKind.Error => Hashes.Combine(_value.GetHashCode(), _text?.GetHashCode() ?? 0),
        _ => _value.GetHashCode()
      };

      return Hashes.Combine(_kind.GetHashCode(), payload);
    }

    /// <summary>
    /// The case and its payload, for diagnostics — <c>Number(42)</c>, <c>Error(#DIV/0!)</c> — and
    /// never display output: formatting a value for presentation is the consumer's job, where the
    /// value's meaning is known.
    /// </summary>
    public override string ToString() =>
      _kind switch
      {
        CellKind.Text => $"Text({_text})",
        CellKind.Number => $"Number({BitConverter.Int64BitsToDouble(_value)})",
        CellKind.Temporal => $"Temporal({Temporal})",
        CellKind.Boolean => $"Boolean({_value != 0L})",
        CellKind.Error => $"Error({AsText()})",
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
  }
}
