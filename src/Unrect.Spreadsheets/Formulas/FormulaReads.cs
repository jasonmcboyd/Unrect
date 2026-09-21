using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The formula behind a cell, asked of a point: <c>row["Total"].HasFormula()</c>.
  /// <para>
  /// The demand is in the type. These compile only over a space that read its formulas
  /// (<see cref="IFormulaSpace"/>), so "this cell is not computed" never means "nobody looked".
  /// </para>
  /// </summary>
  public static class FormulaReads
  {
    /// <summary>The cell's formula, if it has one — the file's own expression, without the leading <c>=</c>.</summary>
    /// <param name="point">The cell.</param>
    /// <param name="formula">The formula, when the answer is true.</param>
    /// <exception cref="OutOfBoundsException">The point is outside its space.</exception>
    public static bool TryGetFormula<TSpace>(this Point<TSpace> point, out string formula)
      where TSpace : class, IFormulaSpace
      => point.Space.TryGetFormulaAt(point.Column, point.Row, out formula);

    /// <summary>Whether the cell is computed rather than typed in.</summary>
    /// <param name="point">The cell.</param>
    /// <exception cref="OutOfBoundsException">The point is outside its space.</exception>
    public static bool HasFormula<TSpace>(this Point<TSpace> point)
      where TSpace : class, IFormulaSpace
      => point.Space.TryGetFormulaAt(point.Column, point.Row, out _);

    /// <summary>The cell's formula, or null where the cell holds a plain value.</summary>
    /// <param name="point">The cell.</param>
    /// <exception cref="OutOfBoundsException">The point is outside its space.</exception>
    public static string? Formula<TSpace>(this Point<TSpace> point)
      where TSpace : class, IFormulaSpace
      => point.Space.TryGetFormulaAt(point.Column, point.Row, out var formula) ? formula : null;
  }
}
