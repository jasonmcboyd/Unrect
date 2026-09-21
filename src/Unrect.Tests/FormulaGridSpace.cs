using System;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A grid that also carries formulas: the second implementation of <see cref="ISpreadsheetSpace"/>,
  /// so the contract theories in <see cref="SpaceContractTests"/> state a law rather than describing
  /// one class. Both arrays are indexed <c>[row, column]</c>, the way an array literal reads.
  /// <para>
  /// It is a <em>backend</em> and not a helper. The eager door's own capable space is internal to
  /// <c>Unrect.Spreadsheets</c> and reachable only through a file, so a theory written over it alone
  /// could not tell a law of the seam from a habit of that one reader. This one is written here, from
  /// the interfaces' documented obligations and nothing else — which is the whole point of asserting
  /// against it: an implementor outside this repository has exactly this much to go on.
  /// </para>
  /// <para>
  /// The kinded reads forward to a <see cref="SheetGrid"/> of the same cells, because a backend that
  /// answered a kind its own way would be stating a habit rather than obeying the contract; the
  /// formulas are this type's own.
  /// </para>
  /// </summary>
  internal sealed class FormulaGridSpace : ISpreadsheetSpace
  {
    private readonly ISheetCells _values;
    private readonly string?[,] _formulas;

    internal FormulaGridSpace(Cell[,] values, string?[,] formulas)
    {
      if (formulas.GetLength(0) != values.GetLength(0) || formulas.GetLength(1) != values.GetLength(1))
        throw new ArgumentException("The formula grid must be the same shape as the value grid.", nameof(formulas));

      _values = SheetGrid.Of(values);
      _formulas = formulas;
    }

    /// <inheritdoc/>
    public Area Area => _values.Area;

    /// <inheritdoc/>
    public bool IsBlank(int column, int row) => _values.IsBlank(column, row);

    /// <inheritdoc/>
    public string? AsText(int column, int row) => _values.AsText(column, row);

    /// <inheritdoc/>
    public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
      => _values.TryGetTextAt(column, row, out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDoubleAt(int column, int row, out double value, out CellProblem? problem)
      => _values.TryGetDoubleAt(column, row, out value, out problem);

    /// <inheritdoc/>
    public bool TryGetDateTimeAt(int column, int row, out DateTime value, out CellProblem? problem)
      => _values.TryGetDateTimeAt(column, row, out value, out problem);

    /// <inheritdoc/>
    public bool TryGetBooleanAt(int column, int row, out bool value, out CellProblem? problem)
      => _values.TryGetBooleanAt(column, row, out value, out problem);

    /// <inheritdoc/>
    public bool TryGetErrorAt(int column, int row, out string error) => _values.TryGetErrorAt(column, row, out error);

    /// <inheritdoc/>
    public string? FormulaAt(int column, int row)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _formulas[row, column];
    }

    /// <summary>A formula fixture written as a literal has no formatting: every cell is set the default way.</summary>
    public CellFont FontAt(int column, int row)
    {
      _ = FormulaAt(column, row);

      return default;
    }

    /// <inheritdoc cref="FontAt"/>
    public CellFill FillAt(int column, int row)
    {
      _ = FormulaAt(column, row);

      return default;
    }
  }
}
