using Unrect.Core;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The eager door's sheet: a value grid and the formulas laid over it, addressed by the same
  /// coordinates. The values are an ordinary <c>GridSpace</c> — this type adds the second layer and
  /// nothing else, so a cell reads exactly as it would have without formulas.
  /// <para>
  /// <b>It exists to obey the slicing law.</b> A subspace is one of these too, sharing the formula
  /// grid and carrying a translated origin, so <see cref="FormulaAt"/> answers about the cells the
  /// slice addresses and never about its parent's. A slice that handed back a plain space would
  /// shed a capability the file has; one that forwarded without translating would answer about the
  /// wrong cells, which is worse.
  /// </para>
  /// </summary>
  internal sealed class SpreadsheetGridSpace : ISpreadsheetSpace
  {
    private readonly ISpace _values;
    private readonly string?[,] _formulas;
    private readonly Offset _origin;

    internal SpreadsheetGridSpace(ISpace values, string?[,] formulas)
      : this(values, formulas, default)
    {
    }

    private SpreadsheetGridSpace(ISpace values, string?[,] formulas, Offset origin)
    {
      _values = values;
      _formulas = formulas;
      _origin = origin;
    }

    public Area Area => _values.Area;

    public CellValue this[int column, int row] => _values[column, row];

    public ISpace GetSubspace(Offset offset, Area area)
      => new SpreadsheetGridSpace(_values.GetSubspace(offset, area), _formulas, _origin + offset);

    public string? FormulaAt(int column, int row)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();

      return _formulas[_origin.Height + row, _origin.Width + column];
    }
  }
}
