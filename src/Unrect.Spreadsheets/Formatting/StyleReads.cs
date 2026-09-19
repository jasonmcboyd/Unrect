using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What a cell looks like, asked of a point: <c>row["Amount"].Font().Color == CellColor.Red</c>.
  /// <para>
  /// The demand is in the type. These compile only over a space that read its formatting
  /// (<see cref="IStyleSpace"/>), so a declaration that asks what a cell looks like cannot be applied
  /// to a sheet nobody looked at and quietly be told "black" — the file names
  /// <see cref="ISpreadsheetSpace"/> as its space, and the space is opened as one.
  /// </para>
  /// <para>
  /// <b>If <c>.Font()</c> will not compile</b> — "ISheetCells cannot be used as type parameter
  /// TSpace", often reported on the call AROUND it, as an overload that was not found — the file's
  /// <c>using static</c> lines name <see cref="ISheetCells"/>. A declaration's space is fixed by
  /// those imports, not by how the workbook is opened: name <see cref="ISpreadsheetSpace"/> in both,
  /// taking the second from <see cref="SpreadsheetProjectionBuilders{TSpace}"/>, and open the sheet
  /// with <see cref="SpreadsheetSpace.CreateWithFormulas(string, string, bool, System.Func{Cell, bool})"/>.
  /// </para>
  /// </summary>
  public static class StyleReads
  {
    /// <summary>How the cell's text is set: its colour, and whether it is bold, italic or struck through.</summary>
    /// <exception cref="OutOfBoundsException">The point is outside its space.</exception>
    public static CellFont Font<TSpace>(this Point<TSpace> point)
      where TSpace : class, IStyleSpace
      => point.Space.FontAt(point.Column, point.Row);

    /// <summary>How the cell is filled; an automatic colour where it has no fill.</summary>
    /// <exception cref="OutOfBoundsException">The point is outside its space.</exception>
    public static CellFill Fill<TSpace>(this Point<TSpace> point)
      where TSpace : class, IStyleSpace
      => point.Space.FillAt(point.Column, point.Row);
  }
}
