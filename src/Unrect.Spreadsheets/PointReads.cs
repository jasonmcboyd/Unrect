using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Reading a cell of a sheet — <c>row["Amount"].Decimal()</c> — as the value at it, and as each
  /// case of that value. Every read here is written over <see cref="Value{TSpace}"/>, so the point
  /// reads, the kinded leaves and the table binder cannot describe one cell differently.
  /// <para>
  /// The receiver's type carries the requirement: these are extensions on a point over an
  /// <see cref="ICellSpace"/>, so a declaration written over a plain grid cannot reach them and a
  /// declaration written over a sheet needs nothing annotated.
  /// </para>
  /// <para>
  /// Three spellings of every case. The asserting read (<c>Double()</c>) throws
  /// <see cref="CellReadException"/> for a cell that disagrees — not a fault: the projection
  /// reading the cell catches it and rethrows it with the declaration path and the cell's A1
  /// address, and a tolerance boundary may absorb it like any other statement about the data. The
  /// asking read (<c>IsDouble()</c>) is true exactly when the asserting one would succeed, because
  /// it is the same read. The try (<c>TryGetDouble(out …)</c>) hands back the value, or the reason
  /// it could not be had. A blank-tolerant twin (<c>DoubleOrBlank()</c>) reads a blank as null and
  /// is as loud as ever about the wrong kind.
  /// </para>
  /// </summary>
  public static class PointReads
  {
    /// <summary>The value at the cell — the sum type itself, for a reader that wants to switch on <see cref="CellValue.Kind"/>.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static CellValue Value<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Space.ValueAt(point.Column, point.Row);

    // --- Asserting ------------------------------------------------------------------------------

    /// <summary>
    /// The text the cell holds — <c>row["Name"].Text()</c>. Where <see cref="CanonicalReads.AsText{TSpace}"/>
    /// takes whatever the cell says, this refuses a cell that holds anything else: a numeric 42 says
    /// "42" and holds no text (<c>expected Text at B4, found Number</c>).
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string Text<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.TryGetText(out var value, out var problem) ? value : throw Failed(point, problem);

    /// <summary>
    /// The cell's number as a <see cref="decimal"/> — a conversion, not a reading: a sheet holds
    /// doubles, and this converts the one it holds, rounding to the fifteen significant digits a
    /// double carries (a stored 0.30000000000000004 reads as 0.3, which for an amount is the right
    /// answer). A number that will not fit fails as a conversion, not as a kind.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static decimal Decimal<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => CellReading.Decimal(point, out var value, out var problem) ? value : throw Failed(point, problem);

    /// <summary>
    /// The cell's number as a whole 32-bit one — a conversion over the double the sheet holds. A
    /// number that is really there but is fractional or out of range fails as a conversion, not as
    /// a kind.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static int Integer<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => CellReading.Integer(point, out var value, out var problem) ? value : throw Failed(point, problem);

    /// <summary>The cell's number, as the double the sheet holds.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static double Double<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.TryGetDouble(out var value, out var problem) ? value : throw Failed(point, problem);

    /// <summary>
    /// The cell's date or time, verbatim. The time of day is kept: truncating is the caller's, not
    /// the sheet's (<c>p.Date().Date</c>).
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static DateTime Date<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.TryGetDate(out var value, out var problem) ? value : throw Failed(point, problem);

    /// <summary>The cell's boolean.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool Boolean<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.TryGetBoolean(out var value, out var problem) ? value : throw Failed(point, problem);

    // --- The blank-tolerant twins -------------------------------------------------------------------
    //
    // Blank-tolerant, kind-intolerant: a blank cell reads as null with no complaint, and a cell of the
    // wrong kind throws exactly as loudly as it does above. That is the law the OrBlank leaves state,
    // and it is stated the same way here because it is the same claim — a missing value says something
    // about the data, a wrong kind says something about the format, and no real format tolerates one.
    //
    // They are separate methods rather than an OrBlank() chained on, because a read returns a value,
    // not a projection there is anything left to modify.

    /// <summary>The text the cell holds, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? TextOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? null : point.Text();

    /// <summary>The cell's number as a <see cref="decimal"/>, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static decimal? DecimalOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? (decimal?)null : point.Decimal();

    /// <summary>The cell's number as a whole 32-bit one, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static int? IntegerOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? (int?)null : point.Integer();

    /// <summary>The cell's number, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static double? DoubleOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? (double?)null : point.Double();

    /// <summary>The cell's date or time, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static DateTime? DateOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? (DateTime?)null : point.Date();

    /// <summary>The cell's boolean, or null when the cell is blank. A cell of another kind still throws.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool? BooleanOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? (bool?)null : point.Boolean();

    // --- Asking rather than asserting -------------------------------------------------------------
    //
    // Each Is… is true exactly when the read of the same name would succeed, because it IS that
    // read: there is one definition of what a cell can be read as, and these cannot drift from it.
    // They ask about a READING, not a kind — a cell holding 1.5 is a double and not an integer.
    // A reader that wants "which one of the cases is it" switches on Value().Kind instead.

    /// <summary>Whether <see cref="Text{TSpace}"/> would succeed — the cell holds text of its own.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value().Kind == CellKind.Text;

    /// <summary>Whether <see cref="Double{TSpace}"/> would succeed — the cell holds a number.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDouble<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value().Kind == CellKind.Number;

    /// <summary>Whether <see cref="Decimal{TSpace}"/> would succeed — the cell holds a number a decimal can carry.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDecimal<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => CellReading.Decimal(point, out _, out _);

    /// <summary>Whether <see cref="Integer{TSpace}"/> would succeed — the cell holds a whole number that fits in 32 bits.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsInteger<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => CellReading.Integer(point, out _, out _);

    /// <summary>Whether <see cref="Date{TSpace}"/> would succeed — the cell holds a date or time.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDate<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value().Kind == CellKind.Temporal;

    /// <summary>Whether <see cref="Boolean{TSpace}"/> would succeed — the cell holds a boolean.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsBoolean<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value().Kind == CellKind.Boolean;

    /// <summary>Whether the cell carries a spreadsheet error — <c>#DIV/0!</c> and its kin. An error is a value, never a blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsError<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value().Kind == CellKind.Error;

    /// <summary>The file's own spelling of the cell's error, or null where the cell carries none.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? ErrorText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Value() is { Kind: CellKind.Error } value ? value.AsText() : null;

    // --- Trying ---------------------------------------------------------------------------------

    /// <summary>The text the cell holds, if text is what it holds.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The cell's own text, when the answer is true.</param>
    public static bool TryGetText<TSpace>(this Point<TSpace> point, out string value)
      where TSpace : class, ICellSpace
      => point.TryGetText(out value, out _);

    /// <summary>The text the cell holds, or the reason it holds none — <c>expected Text, found Number</c>.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The cell's own text, when the answer is true.</param>
    /// <param name="problem">Why not, when the answer is false; null when it is true.</param>
    public static bool TryGetText<TSpace>(this Point<TSpace> point, out string value, out CellProblem? problem)
      where TSpace : class, ICellSpace
      => CellReading.Text(point.Value(), out value, out problem);

    /// <summary>The cell's number, if a number is what it holds.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The double the sheet holds, when the answer is true.</param>
    public static bool TryGetDouble<TSpace>(this Point<TSpace> point, out double value)
      where TSpace : class, ICellSpace
      => point.TryGetDouble(out value, out _);

    /// <summary>The cell's number, or the reason it holds none — <c>expected Number, found Text</c>.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The double the sheet holds, when the answer is true.</param>
    /// <param name="problem">Why not, when the answer is false; null when it is true.</param>
    public static bool TryGetDouble<TSpace>(this Point<TSpace> point, out double value, out CellProblem? problem)
      where TSpace : class, ICellSpace
      => CellReading.Double(point.Value(), out value, out problem);

    /// <summary>The cell's date or time, verbatim, if that is what it holds.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The date and time, when the answer is true.</param>
    public static bool TryGetDate<TSpace>(this Point<TSpace> point, out DateTime value)
      where TSpace : class, ICellSpace
      => point.TryGetDate(out value, out _);

    /// <summary>The cell's date or time, or the reason it holds none.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The date and time, when the answer is true.</param>
    /// <param name="problem">Why not, when the answer is false; null when it is true.</param>
    public static bool TryGetDate<TSpace>(this Point<TSpace> point, out DateTime value, out CellProblem? problem)
      where TSpace : class, ICellSpace
      => CellReading.DateTime(point.Value(), out value, out problem);

    /// <summary>The cell's boolean, if a boolean is what it holds.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The boolean, when the answer is true.</param>
    public static bool TryGetBoolean<TSpace>(this Point<TSpace> point, out bool value)
      where TSpace : class, ICellSpace
      => point.TryGetBoolean(out value, out _);

    /// <summary>The cell's boolean, or the reason it holds none.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The boolean, when the answer is true.</param>
    /// <param name="problem">Why not, when the answer is false; null when it is true.</param>
    public static bool TryGetBoolean<TSpace>(this Point<TSpace> point, out bool value, out CellProblem? problem)
      where TSpace : class, ICellSpace
      => CellReading.Boolean(point.Value(), out value, out problem);

    private static CellReadException Failed<TSpace>(Point<TSpace> point, CellProblem? problem)
      where TSpace : class, ICellSpace
      => new CellReadException(
        point.Erased(),
        problem ?? throw new InvalidOperationException("a failed read must say why"));
  }
}
