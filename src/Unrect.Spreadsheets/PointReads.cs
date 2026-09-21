using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// Reading a cell of a sheet as the kind the declaration says it is — <c>row["Amount"].Decimal()</c>.
  /// <para>
  /// The receiver's type carries the requirement: these are extensions on a point over an
  /// <see cref="ISheetCells"/>, so a declaration written over a plain grid cannot reach them and a
  /// declaration written over a sheet needs nothing annotated.
  /// </para>
  /// <para>
  /// A cell that disagrees throws <see cref="CellReadException"/>, which is not a fault: the
  /// projection reading the cell catches it and rethrows it with the declaration path and the cell's
  /// A1 address, and a tolerance boundary may absorb it like any other statement about the data.
  /// </para>
  /// </summary>
  public static class PointReads
  {
    /// <summary>
    /// The cell's number as a <see cref="decimal"/> — a conversion, not a reading: a sheet holds
    /// doubles, and this converts the one it holds, rounding to the fifteen significant digits a
    /// double carries (a stored 0.30000000000000004 reads as 0.3, which for an amount is the right
    /// answer). A number that will not fit fails as a conversion, not as a kind.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static decimal Decimal<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => CellReading.Decimal(point, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>
    /// The cell's number as a whole 32-bit one — a conversion over the double the sheet holds. A
    /// number that is really there but is fractional or out of range fails as a conversion, not as
    /// a kind.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static int Integer<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => CellReading.Integer(point, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>The cell's number as a <see cref="double"/>.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static double Double<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDoubleAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>
    /// The cell's date or time, verbatim. The time of day is kept: truncating is the caller's
    /// (<c>.Date().Date</c>), because a read that silently handed back less than the cell holds
    /// would be the only one here that did.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static DateTime Date<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDateTimeAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>The cell's boolean.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool Boolean<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetBooleanAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    // --- The blank-tolerant twins -------------------------------------------------------------------
    //
    // Blank-tolerant, kind-intolerant: a blank cell reads as null with no complaint, and a cell of the
    // wrong kind throws exactly as loudly as it does above. That is the law the OrBlank leaves state,
    // and it is stated the same way here because it is the same claim — a missing value says something
    // about the data, a wrong kind says something about the format, and no real format tolerates one.
    //
    // They are separate methods rather than an OrBlank() chained on, because a read returns a value,
    // not a projection there is anything left to modify.

    /// <summary>The cell's number as a <see cref="decimal"/>, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static decimal? DecimalOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (decimal?)null : point.Decimal();

    /// <summary>The cell's number as a whole 32-bit one, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static int? IntegerOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (int?)null : point.Integer();

    /// <summary>The cell's number as a <see cref="double"/>, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static double? DoubleOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (double?)null : point.Double();

    /// <summary>The cell's date or time, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static DateTime? DateOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (DateTime?)null : point.Date();

    /// <summary>The cell's boolean, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool? BooleanOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (bool?)null : point.Boolean();

    // --- Asking rather than asserting -------------------------------------------------------------
    //
    // Each Is… is true exactly when the read of the same name would succeed, because it IS that
    // read: there is one definition of what a cell can be read as, and these cannot drift from it.
    // They ask about a READING, not a kind — a cell holding 1.5 is a double and not an integer.
    // There is no question that asks "which one is it": a cell can be read several ways at once.

    /// <summary>Whether <see cref="Double{TSpace}"/> would succeed: the cell holds a number.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDouble<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDoubleAt(point.Column, point.Row, out _, out _);

    /// <summary>Whether <see cref="Decimal{TSpace}"/> would succeed: a number a decimal can hold.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDecimal<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => CellReading.Decimal(point, out _, out _);

    /// <summary>Whether <see cref="Integer{TSpace}"/> would succeed: a whole number in 32-bit range.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsInteger<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => CellReading.Integer(point, out _, out _);

    /// <summary>Whether <see cref="Date{TSpace}"/> would succeed: the cell holds a date or time.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsDate<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDateTimeAt(point.Column, point.Row, out _, out _);

    /// <summary>Whether <see cref="Boolean{TSpace}"/> would succeed: the cell holds a boolean.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsBoolean<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetBooleanAt(point.Column, point.Row, out _, out _);

    /// <summary>The cell's number, if it holds one.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The number, when the answer is true.</param>
    public static bool TryGetDouble<TSpace>(this Point<TSpace> point, out double value)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDoubleAt(point.Column, point.Row, out value, out _);

    /// <summary>The cell's date or time, if it holds one.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The date or time, when the answer is true.</param>
    public static bool TryGetDate<TSpace>(this Point<TSpace> point, out DateTime value)
      where TSpace : class, ISheetCells
      => point.Space.TryGetDateTimeAt(point.Column, point.Row, out value, out _);

    /// <summary>The cell's boolean, if it holds one.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    /// <param name="value">The boolean, when the answer is true.</param>
    public static bool TryGetBoolean<TSpace>(this Point<TSpace> point, out bool value)
      where TSpace : class, ISheetCells
      => point.Space.TryGetBooleanAt(point.Column, point.Row, out value, out _);

    /// <summary>
    /// Whether the cell carries an error rather than a value — <c>#DIV/0!</c> and its kin.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsError<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetErrorAt(point.Column, point.Row, out _);

    /// <summary>The file's own spelling of the cell's error, or null where the cell is not one.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? ErrorText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TryGetErrorAt(point.Column, point.Row, out var error) ? error : null;

    /// <summary>
    /// The exception a refused read throws. A read that returns false owes a reason, so a null one
    /// is this package failing its own contract rather than anything about the cell — said out loud
    /// here instead of surfacing as a null reference inside the exception's message.
    /// </summary>
    private static CellReadException Failed<TSpace>(Point<TSpace> point, CellProblem? problem)
      where TSpace : class, ISheetCells
      => new CellReadException(
        point.Erased(),
        problem ?? throw new InvalidOperationException("a failed read must say why"));
  }
}
