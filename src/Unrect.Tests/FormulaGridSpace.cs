using System;

using Unrect.Core;
using Unrect.Spreadsheets;

namespace Unrect.Tests
{
  /// <summary>
  /// A grid that also carries formulas: the second implementation of <see cref="IFormulaSpace"/>, so
  /// the slicing-law theory in <see cref="SpaceContractTests"/> states a law rather than describing
  /// one class. Both arrays are indexed <c>[row, column]</c>, the way an array literal reads.
  /// <para>
  /// It is a <em>backend</em> and not a helper. The eager door's own capable space is internal to
  /// <c>Unrect.Spreadsheets</c> and reachable only through a file, so a theory written over it alone
  /// could not tell a law of the seam from a habit of that one reader. This one is written here, from
  /// the interface's documented obligations and nothing else — which is the whole point of asserting
  /// against it: an implementor outside this repository has exactly this much to go on.
  /// </para>
  /// <para>
  /// It mirrors the typed-spaces spike's space of the same name, and it obeys the law the same way
  /// the eager door does: a subspace is one of these too, sharing the arrays and carrying a
  /// translated origin, so <see cref="FormulaAt"/> answers about the cells the slice addresses and
  /// never about its parent's.
  /// </para>
  /// </summary>
  internal sealed class FormulaGridSpace : ISpreadsheetSpace
  {
    private readonly CellValue[,] _values;
    private readonly string?[,] _formulas;
    private readonly Offset _origin;

    internal FormulaGridSpace(CellValue[,] values, string?[,] formulas)
      : this(values, formulas, default, new Area(values.GetLength(1), values.GetLength(0)))
    {
      if (formulas.GetLength(0) != values.GetLength(0) || formulas.GetLength(1) != values.GetLength(1))
        throw new ArgumentException("The formula grid must be the same shape as the value grid.", nameof(formulas));
    }

    private FormulaGridSpace(CellValue[,] values, string?[,] formulas, Offset origin, Area area)
    {
      _values = values;
      _formulas = formulas;
      _origin = origin;
      Area = area;
    }

    /// <inheritdoc/>
    public Area Area { get; }

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        Check(column, row);

        return _values[_origin.Height + row, _origin.Width + column];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new FormulaGridSpace(_values, _formulas, offset + _origin, area);
    }

    /// <inheritdoc/>
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
