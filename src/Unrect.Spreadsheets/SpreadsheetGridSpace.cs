using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The eager door's sheet with formulas: a <see cref="SheetGrid"/> and the formulas laid over it,
  /// addressed by the same coordinates. This type adds the second layer and nothing else, so a cell
  /// reads exactly as it would have without formulas.
  /// </summary>
  internal sealed class SpreadsheetGridSpace : SheetCellsBase, ISpreadsheetSpace
  {
    private readonly SheetGrid _values;
    private readonly string?[,] _formulas;

    internal SpreadsheetGridSpace(SheetGrid values, string?[,] formulas)
    {
      _values = values;
      _formulas = formulas;
    }

    public override Area Area => _values.Area;

    public string? FormulaAt(int column, int row)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _formulas[row, column];
    }

    private protected override Cell CellAt(int column, int row) => _values.At(column, row);
  }
}
