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
    /// <summary>The cell's text; a cell of another kind throws the reading diagnostic.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string Text<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.TextAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>
    /// The cell's number as a <see cref="decimal"/> — the accessor that keeps a spreadsheet's exact
    /// decimal where the file carried one.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static decimal Decimal<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.DecimalAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>
    /// The cell's number as a whole 32-bit one. A number that is really there but is fractional or
    /// out of range fails as a conversion, not as a kind.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static int Integer<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.IntegerAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>The cell's number as a <see cref="double"/>.</summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static double Double<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.DoubleAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>
    /// The cell's date or time, verbatim. The time of day is kept: truncating is the caller's
    /// (<c>.Date().Date</c>), because a read that silently handed back less than the cell holds
    /// would be the only one here that did.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static DateTime Date<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.DateTimeAt(point.Column, point.Row, out var value, out var problem)
        ? value
        : throw Failed(point, problem);

    /// <summary>The cell's boolean.</summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static bool Boolean<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.BooleanAt(point.Column, point.Row, out var value, out var problem)
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

    /// <summary>The cell's text, or null when the cell is blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? TextOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? null : point.Text();

    /// <summary>The cell's number as a <see cref="decimal"/>, or null when the cell is blank.</summary>
    /// <inheritdoc cref="TextOrBlank{TSpace}"/>
    public static decimal? DecimalOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (decimal?)null : point.Decimal();

    /// <summary>The cell's number as a whole 32-bit one, or null when the cell is blank.</summary>
    /// <inheritdoc cref="TextOrBlank{TSpace}"/>
    public static int? IntegerOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (int?)null : point.Integer();

    /// <summary>The cell's number as a <see cref="double"/>, or null when the cell is blank.</summary>
    /// <inheritdoc cref="TextOrBlank{TSpace}"/>
    public static double? DoubleOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (double?)null : point.Double();

    /// <summary>The cell's date or time, or null when the cell is blank.</summary>
    /// <inheritdoc cref="TextOrBlank{TSpace}"/>
    public static DateTime? DateOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (DateTime?)null : point.Date();

    /// <summary>The cell's boolean, or null when the cell is blank.</summary>
    /// <inheritdoc cref="TextOrBlank{TSpace}"/>
    public static bool? BooleanOrBlank<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.IsBlank ? (bool?)null : point.Boolean();

    /// <summary>
    /// Whether the cell carries an error rather than a value — <c>#DIV/0!</c> and its kin.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static bool IsError<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.IsErrorAt(point.Column, point.Row);

    /// <summary>The file's own spelling of the cell's error, or null where the cell is not one.</summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static string? ErrorText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.ErrorTextAt(point.Column, point.Row);

    /// <summary>
    /// Which kind the cell is — the question a predicate puts to a cell, where
    /// <see cref="Text{TSpace}"/>, <see cref="Decimal{TSpace}"/>, <see cref="Integer{TSpace}"/>,
    /// <see cref="Double{TSpace}"/>, <see cref="Date{TSpace}"/> and <see cref="Boolean{TSpace}"/>
    /// assert one and refuse a cell that disagrees:
    /// <c>RowsWhileAny(p =&gt; p.Kind() == CellKind.Number)</c>.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static CellKind Kind<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.KindAt(point.Column, point.Row);

    /// <summary>
    /// What kind of thing the cell is, in the document's own vocabulary — for a caller's own
    /// complaint about a cell this vocabulary has no reading for.
    /// </summary>
    /// <inheritdoc cref="Text{TSpace}"/>
    public static string Describe<TSpace>(this Point<TSpace> point)
      where TSpace : class, ISheetCells
      => point.Space.Describe(point.Column, point.Row);

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
