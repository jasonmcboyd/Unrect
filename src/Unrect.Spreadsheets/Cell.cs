using System;
using System.Globalization;

using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One cell's value, in the vocabulary every <see cref="ISheetCells"/> speaks: a <see cref="CellKind"/>
  /// plus a payload for that kind. Construct one with an <c>Of</c> overload (or <see cref="OfError"/>
  /// for <see cref="CellKind.Error"/>); read it back with the typed <c>TryGet*</c>/<c>Get*</c> pairs,
  /// never by inspecting a backend type directly — that boundary is the whole point of the canonical
  /// model.
  /// <para>
  /// A value type, and deliberately a small one: a sheet is an array of these, and a million-row
  /// workbook holds eight million of them. Three fields carry every kind — the kind itself, eight
  /// bytes of payload, and one reference for the payloads that need a heap object (a string, or the
  /// exact decimal a number may remember). A number that arrived as a <c>double</c>, a date, a
  /// boolean and a blank therefore cost nothing beyond the array slot they sit in.
  /// </para>
  /// <para>
  /// <c>default(Cell)</c> is <see cref="Blank"/>, which is what makes a freshly allocated
  /// <c>Cell[,]</c> an already-blank sheet.
  /// </para>
  /// </summary>
  public readonly struct Cell : IEquatable<Cell>
  {
    // Ticks occupy the low 62 bits of a DateTime and its DateTimeKind the top two — the runtime's
    // own encoding, replicated here so a temporal cell packs into the payload word without losing
    // the Kind a caller may read back off GetDateTime.
    private const long TicksMask = 0x3FFFFFFFFFFFFFFF;

    private readonly CellKind _kind;

    // The payload that fits in a word: a double's bits, a packed DateTime, a boolean as 0/1, or an
    // error's code. Unused, and zero, for Blank and Text.
    private readonly long _value;

    // The payload that does not: a Text cell's string, a Number cell's exact decimal when it kept
    // one (boxed), or an Error cell's literal when it differs from the canonical spelling. Null
    // whenever the kind has nothing to hang here, which is the common case.
    private readonly object? _overflow;

    private Cell(CellKind kind, long value, object? overflow)
    {
      _kind = kind;
      _value = value;
      _overflow = overflow;
    }

    private Cell(string text)
      : this(CellKind.Text, 0L, text)
    {
    }

    private Cell(double number, decimal? exactNumber)
      : this(CellKind.Number, BitConverter.DoubleToInt64Bits(number), exactNumber.HasValue ? (object)exactNumber.Value : null)
    {
    }

    private Cell(DateTime temporal)
      : this(CellKind.Temporal, temporal.Ticks | ((long)temporal.Kind << 62), null)
    {
    }

    private Cell(bool boolean)
      : this(CellKind.Boolean, boolean ? 1L : 0L, null)
    {
    }

    private Cell(CellError error, string? errorText)
      : this(CellKind.Error, (long)error, errorText)
    {
    }

    /// <summary>The blank value — <see cref="CellKind.Blank"/> carries no payload, so nothing distinguishes two blanks.</summary>
    public static Cell Blank => default;

    /// <summary>A <see cref="CellKind.Text"/> cell, or <see cref="Blank"/> when <paramref name="value"/> is null.</summary>
    public static Cell Of(string? value) => value is null ? Blank : new Cell(value);

    /// <summary>A <see cref="CellKind.Number"/> cell that remembers it arrived as an exact integer (see <see cref="GetDecimal"/>).</summary>
    public static Cell Of(int value) => new Cell(value, value);

    /// <summary>A <see cref="CellKind.Number"/> cell that remembers it arrived as an exact integer (see <see cref="GetDecimal"/>).</summary>
    public static Cell Of(long value) => new Cell(value, value);

    /// <summary>A <see cref="CellKind.Number"/> cell with no exact decimal behind it — <see cref="GetDecimal"/> falls back to converting the double.</summary>
    public static Cell Of(double value) => new Cell(value, null);

    /// <summary>A <see cref="CellKind.Number"/> cell that remembers its exact decimal alongside the double it also stores.</summary>
    public static Cell Of(decimal value) => new Cell((double)value, value);

    /// <summary>A <see cref="CellKind.Temporal"/> cell.</summary>
    public static Cell Of(DateTime value) => new Cell(value);

    /// <summary>A <see cref="CellKind.Boolean"/> cell.</summary>
    public static Cell Of(bool value) => new Cell(value);

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
    public static Cell OfError(CellError error, string? literal = null)
      // Kept only when it says something the canonical spelling does not, so the ordinary path
      // stores no extra string. A blank literal says nothing at all, and storing one would render
      // as "Error()".
      => new Cell(error, string.IsNullOrWhiteSpace(literal) || literal == Display(error) ? null : literal);

    /// <summary>Which kind of value this cell holds.</summary>
    public CellKind Kind => _kind;

    /// <summary>Whether this cell carries no value — <see cref="Kind"/> is <see cref="CellKind.Blank"/>.</summary>
    public bool IsBlank => Kind == CellKind.Blank;

    /// <summary>The negation of <see cref="IsBlank"/>; an error cell has a value and is never blank.</summary>
    public bool HasValue => !IsBlank;

    /// <summary>
    /// Whether this cell's <see cref="AsText"/> is its own value rather than a rendering —
    /// <see cref="Kind"/> is <see cref="CellKind.Text"/>. Text matching asks this first, which is
    /// why a numeric 42 is not a cell saying "42".
    /// </summary>
    public bool IsText => Kind == CellKind.Text;

    // The payloads, unpacked. Each is meaningful only for its own kind; every reader below checks
    // Kind first, exactly as it did when these were fields.
    private string? Text => (string?)_overflow;
    private double Number => BitConverter.Int64BitsToDouble(_value);
    private decimal? ExactNumber => _overflow is decimal exact ? exact : (decimal?)null;
    private DateTime Temporal => new DateTime(_value & TicksMask, (DateTimeKind)((_value >> 62) & 3L));
    private bool Boolean => _value != 0L;
    private CellError Error => (CellError)_value;
    private string? ErrorText => (string?)_overflow;

    /// <summary>The cell's text, or null when <see cref="Kind"/> is not <see cref="CellKind.Text"/>.</summary>
    internal string? TryGetString() => Kind == CellKind.Text ? Text : null;

    /// <summary>The cell's text; throws when <see cref="Kind"/> is not <see cref="CellKind.Text"/> (see <see cref="TryGetString"/>).</summary>
    internal string GetString() => TryGetString() ?? throw WrongKind(CellKind.Text);

    /// <summary>The cell's number as a <see cref="double"/>, or null when <see cref="Kind"/> is not <see cref="CellKind.Number"/>.</summary>
    internal double? TryGetDouble() => Kind == CellKind.Number ? Number : (double?)null;

    /// <summary>The cell's number as a <see cref="double"/>; throws when <see cref="Kind"/> is not <see cref="CellKind.Number"/> (see <see cref="TryGetDouble"/>).</summary>
    internal double GetDouble() => TryGetDouble() ?? throw WrongKind(CellKind.Number);

    /// <summary>
    /// The cell's number as a <see cref="decimal"/>: the exact value it was constructed with
    /// (<see cref="Of(decimal)"/>/<see cref="Of(int)"/>/<see cref="Of(long)"/>), or the double
    /// converted when that fits and no exact value was kept; null when it does not fit, or when
    /// <see cref="Kind"/> is not <see cref="CellKind.Number"/>.
    /// </summary>
    internal decimal? TryGetDecimal()
    {
      if (Kind != CellKind.Number)
        return null;

      return ExactNumber ?? (IsRepresentableAsDecimal(Number) ? (decimal)Number : (decimal?)null);
    }

    /// <summary>The cell's number as a <see cref="decimal"/>; throws when it does not fit or <see cref="Kind"/> is not <see cref="CellKind.Number"/> (see <see cref="TryGetDecimal"/>).</summary>
    internal decimal GetDecimal() =>
      TryGetDecimal()
      ?? throw new InvalidOperationException(
        Kind == CellKind.Number
        ? $"Cell value {Number} is not representable as a {nameof(Decimal)}."
        : WrongKindMessage(CellKind.Number));

    /// <summary>The cell's number as an <see cref="int"/>, when it is a whole number in range; null otherwise, including when <see cref="Kind"/> is not <see cref="CellKind.Number"/>.</summary>
    internal int? TryGetInt()
    {
      if (Kind != CellKind.Number)
        return null;

      var number = Number;

      return number >= int.MinValue && number <= int.MaxValue && Math.Floor(number) == number
        ? (int)number
        : (int?)null;
    }

    /// <summary>The cell's number as an <see cref="int"/>; throws when it is not a whole number in range or <see cref="Kind"/> is not <see cref="CellKind.Number"/> (see <see cref="TryGetInt"/>).</summary>
    internal int GetInt() =>
      TryGetInt()
      ?? throw new InvalidOperationException(
        Kind == CellKind.Number
        ? $"Cell value {Number} is not an integer within the range of {nameof(Int32)}."
        : WrongKindMessage(CellKind.Number));

    /// <summary>The cell's date and time, or null when <see cref="Kind"/> is not <see cref="CellKind.Temporal"/>.</summary>
    internal DateTime? TryGetDateTime() => Kind == CellKind.Temporal ? Temporal : (DateTime?)null;

    /// <summary>The cell's date and time; throws when <see cref="Kind"/> is not <see cref="CellKind.Temporal"/> (see <see cref="TryGetDateTime"/>).</summary>
    internal DateTime GetDateTime() => TryGetDateTime() ?? throw WrongKind(CellKind.Temporal);
    /// <summary>The date part, or null when the cell is not temporal — the Try twin of <see cref="GetDate"/>.</summary>
    internal DateTime? TryGetDate() => TryGetDateTime()?.Date;

    /// <summary>The cell's date, time truncated; throws when <see cref="Kind"/> is not <see cref="CellKind.Temporal"/> (see <see cref="TryGetDate"/>).</summary>
    internal DateTime GetDate() => GetDateTime().Date;

    /// <summary>The cell's boolean, or null when <see cref="Kind"/> is not <see cref="CellKind.Boolean"/>.</summary>
    internal bool? TryGetBoolean() => Kind == CellKind.Boolean ? Boolean : (bool?)null;

    /// <summary>The cell's boolean; throws when <see cref="Kind"/> is not <see cref="CellKind.Boolean"/> (see <see cref="TryGetBoolean"/>).</summary>
    internal bool GetBoolean() => TryGetBoolean() ?? throw WrongKind(CellKind.Boolean);

    /// <summary>The cell's error, or null when <see cref="Kind"/> is not <see cref="CellKind.Error"/>.</summary>
    internal CellError? TryGetError() => Kind == CellKind.Error ? Error : (CellError?)null;

    /// <summary>The cell's error; throws when <see cref="Kind"/> is not <see cref="CellKind.Error"/> (see <see cref="TryGetError"/>).</summary>
    internal CellError GetError() => TryGetError() ?? throw WrongKind(CellKind.Error);

    /// <summary>
    /// The text this error arrived as, when it differs from the canonical spelling of its
    /// <see cref="CellError"/> — otherwise null, including for every cell that is not an error.
    /// Use it to recover what an <see cref="CellError.Other"/> actually was.
    /// </summary>
    internal string? TryGetErrorText() => Kind == CellKind.Error ? ErrorText : null;

    /// <summary>
    /// What the cell says: a <see cref="CellKind.Text"/> cell's own string, a number's digits, a
    /// date's ISO form, <c>TRUE</c> or <c>FALSE</c>, an error's spreadsheet literal. Null exactly
    /// when the cell is <see cref="Blank"/>.
    /// <para>
    /// Not display output, and not the text the backend showed: a number renders invariantly from
    /// the value, carrying no trace of the format it was formatted with. What this is for is the
    /// questions a reader asks without knowing the kind — "does this cell say <c>Total</c>" — and
    /// the answer for a cell that is not text is a rendering rather than something the cell holds,
    /// which is what <see cref="CellKind.Text"/> distinguishes.
    /// </para>
    /// <para>
    /// A number renders from <em>how it arrived</em> rather than from what it is worth, because the
    /// exact decimal it may carry is the digits the file wrote. So <see cref="Of(decimal)"/> of
    /// <c>1.0m</c> and <see cref="Of(double)"/> of <c>1.0</c> are equal cells that say <c>1.0</c>
    /// and <c>1</c>. A number with no exact decimal behind it round-trips its double, which leaves
    /// two residuals worth knowing: a negative zero says <c>-0</c> on .NET Core and <c>0</c> on
    /// .NET Framework, and a large enough magnitude says <c>1E+20</c> rather than its digits.
    /// </para>
    /// </summary>
    public string? AsText() =>
      Kind switch
      {
        CellKind.Text => Text,
        // The shortest digits that read back exactly, found the same way on both targets: neither
        // the default nor "R" agrees between .NET Framework and .NET, so 0.1 + 0.2 would otherwise
        // say "0.3" through one target of this package and "0.30000000000000004" through the other.
        CellKind.Number => ExactNumber?.ToString(CultureInfo.InvariantCulture) ?? Renderings.ShortestRoundTrip(Number),
        // A date says its date; a moment within a day says the time too, rather than silently
        // rendering as the midnight it is not. Sub-second digits are carried only when there are
        // some, so a whole second still says hh:mm:ss.
        CellKind.Temporal => Temporal.ToString(
          Temporal.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
          CultureInfo.InvariantCulture),
        CellKind.Boolean => Boolean ? "TRUE" : "FALSE",
        CellKind.Error => ErrorText ?? Display(Error),
        _ => null
      };

    /// <summary>
    /// Two cell values are equal when they share a kind and an equal payload. Numbers compare on
    /// their double representation, so <c>Of(1m)</c> equals <c>Of(1.0)</c> even though
    /// <see cref="GetDecimal"/> may report a different precision for each. Number comparison uses
    /// <see cref="double.Equals(double)"/>, so NaN equals NaN — equality is reflexive and
    /// hash-consistent, deviating from IEEE <c>==</c> on purpose. Equality is for matching
    /// cells; <see cref="GetDecimal"/> is for extracting values.
    /// <para>
    /// Public, and the interface with it, for one concrete reason: without
    /// <see cref="IEquatable{T}"/> every comparison through
    /// <see cref="System.Collections.Generic.EqualityComparer{T}.Default"/> boxes both cells — which
    /// the chunk store and the retention rig feel, since they hold cells by the million.
    /// </para>
    /// </summary>
    public bool Equals(Cell other)
    {
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

    /// <summary>Equality against any object — see <see cref="Equals(Cell)"/> when the other value is not a <see cref="Cell"/>.</summary>
    public override bool Equals(object? obj) => obj is Cell other && Equals(other);

    /// <summary>Consistent with <see cref="Equals(Cell)"/>: hashes the kind and its payload, numbers on their double representation.</summary>
    public override int GetHashCode()
    {
      var payload = Kind switch
      {
        CellKind.Text => Text!.GetHashCode(),
        CellKind.Number => Number.GetHashCode(),
        CellKind.Temporal => Temporal.GetHashCode(),
        CellKind.Boolean => Boolean.GetHashCode(),
        CellKind.Error => Hashes.Combine(Error.GetHashCode(), ErrorText?.GetHashCode() ?? 0),
        _ => 0
      };

      return Hashes.Combine(Kind.GetHashCode(), payload);
    }

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
