using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The eager door's sheet with formulas: a <see cref="SheetGrid"/> and the formulas laid over it,
  /// addressed by the same coordinates. This type adds the second layer and nothing else, so a cell
  /// reads exactly as it would have without formulas.
  /// </summary>
  internal sealed class SpreadsheetGridSpace : CellSpaceBase, ISpreadsheetSpace
  {
    private readonly SheetGrid _values;
    private readonly string?[,] _formulas;
    private readonly int[,] _styles;
    private readonly CellFont[] _fonts;
    private readonly CellFill[] _fills;

    /// <param name="values">The sheet's values.</param>
    /// <param name="formulas">The formula behind each cell, null for a plain value.</param>
    /// <param name="styles">The style each cell names.</param>
    /// <param name="fonts">The font of each style, by the style's index.</param>
    /// <param name="fills">The fill of each style, by the style's index.</param>
    internal SpreadsheetGridSpace(SheetGrid values, string?[,] formulas, int[,] styles, CellFont[] fonts, CellFill[] fills)
    {
      _values = values;
      _formulas = formulas;
      _styles = styles;
      _fonts = fonts;
      _fills = fills;
    }

    public override Area Area => _values.Area;

    public bool TryGetFormulaAt(int column, int row, out string formula)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      formula = _formulas[row, column]!;
      return formula is not null;
    }

    public CellFont FontAt(int column, int row)
      => StyleAt(column, row) is var style && style < _fonts.Length ? _fonts[style] : default;

    public CellFill FillAt(int column, int row)
      => StyleAt(column, row) is var style && style < _fills.Length ? _fills[style] : default;

    private int StyleAt(int column, int row)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _styles[row, column];
    }

    private protected override Cell CellAt(int column, int row) => _values.At(column, row);
  }
}
