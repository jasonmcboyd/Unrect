using System;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace TypedSpacesGauntlet
{
  /// <summary>
  /// SPIKE. A <c>GridSpace</c> that also carries formulas — the test double the experiment's verdict
  /// is about typing rather than about parsing xlsx. Both arrays are indexed <c>[row, column]</c>,
  /// the way an array literal reads.
  /// <para>
  /// It exists to obey the slicing law: a subspace is a <see cref="FormulaGridSpace"/> too, sharing
  /// the arrays and carrying a translated origin, so <see cref="FormulaAt"/> answers about the cells
  /// the slice addresses and never about the parent's.
  /// </para>
  /// </summary>
  public sealed class FormulaGridSpace : IFormulaSpace
  {
    private readonly CellValue[,] _values;
    private readonly string?[,] _formulas;
    private readonly Offset _origin;

    public FormulaGridSpace(CellValue[,] values, string?[,] formulas)
      : this(values, formulas, default, new Area(values.GetLength(1), values.GetLength(0)))
    {
      if (formulas.GetLength(0) != values.GetLength(0) || formulas.GetLength(1) != values.GetLength(1))
        throw new ArgumentException("The formula grid must be the same shape as the value grid.", nameof(formulas));
    }

    private FormulaGridSpace(CellValue[,] values, string?[,] formulas, Offset origin, Area area)
    {
      if (origin.Width + area.Width > values.GetLength(1) || origin.Height + area.Height > values.GetLength(0))
        throw new OutOfBoundsException();

      _values = values;
      _formulas = formulas;
      _origin = origin;
      Area = area;
    }

    public Area Area { get; }

    public CellValue this[int column, int row]
    {
      get
      {
        Check(column, row);

        return _values[_origin.Height + row, _origin.Width + column];
      }
    }

    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new FormulaGridSpace(_values, _formulas, offset + _origin, area);
    }

    public string? FormulaAt(int column, int row)
    {
      Check(column, row);

      return _formulas[_origin.Height + row, _origin.Width + column];
    }

    private void Check(int column, int row)
    {
      if (column < 0 || column >= Area.Width || row < 0 || row >= Area.Height)
        throw new OutOfBoundsException();
    }
  }
}
