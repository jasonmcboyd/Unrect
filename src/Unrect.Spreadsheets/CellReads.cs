using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What only a sheet answers of a cell: the <see cref="CellValue"/> at it, for a reader that
  /// wants to switch on <see cref="CellValue.Kind"/>, and the spreadsheet error a cell may carry.
  /// Every other reading — <c>row["Amount"].Decimal()</c>, <c>IsDate()</c>, <c>TryGetText(out …)</c>
  /// — is <see cref="Projections.PointReads"/>, over any space, because a space says how its cells
  /// may be read and a sheet says so from its value.
  /// </summary>
  public static class CellReads
  {
    /// <summary>The value at the cell — the sum type itself.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static CellValue Value<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.Space.ValueAt(point.Column, point.Row);

    /// <summary>Whether the cell carries a spreadsheet error — <c>#DIV/0!</c> and its kin. An error is a value, never a blank.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static bool IsError<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => Content(point).Kind == CellKind.Error;

    /// <summary>The file's own spelling of the cell's error, or null where the cell carries none.</summary>
    /// <typeparam name="TSpace">The sheet the point addresses a cell of.</typeparam>
    /// <param name="point">The cell.</param>
    public static string? ErrorText<TSpace>(this Point<TSpace> point)
      where TSpace : class, ICellSpace
      => Content(point) is { Kind: CellKind.Error } value ? value.AsText() : null;

    /// <summary>
    /// The value a reading sees: what the cell holds, unless the space calls the cell blank, in
    /// which case a blank — a cell that does not count as content holds nothing a reading may
    /// assert, whatever text the rule was applied to. The value facet itself, <see cref="Value{TSpace}"/>,
    /// stays faithful to the store.
    /// </summary>
    private static CellValue Content<TSpace>(Point<TSpace> point)
      where TSpace : class, ICellSpace
      => point.IsBlank() ? CellValue.Blank : point.Value();
  }
}
