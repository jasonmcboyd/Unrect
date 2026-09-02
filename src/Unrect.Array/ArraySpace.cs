using System;

using Unrect.Core;

namespace Unrect.Array
{
  /// <summary>
  /// Values already in memory, adapted as a space. It is <see cref="GridSpace"/> plus the mapping
  /// step: the <c>Create</c> overloads turn a plain array of numbers, strings or anything else into
  /// canonical cell values, which is where <em>blankness is decided</em> — the one question an
  /// adapter must answer that the grid itself cannot.
  /// <para>
  /// The reference adapter, and the one to reach for in a test or a script: everything above it
  /// behaves exactly as it does over a workbook.
  /// </para>
  /// </summary>
  public class ArraySpace : ISpace
  {
    /// <summary>Cell values that are already canonical.</summary>
    public ArraySpace(CellValue[,] values)
      : this(new GridSpace(values))
    {
    }

    private ArraySpace(ISpace grid)
    {
      Grid = grid;
    }

    private ISpace Grid { get; }

    public Area Area => Grid.Area;

    public CellValue this[int column, int row] => Grid[column, row];

    public ISpace GetSubspace(Offset offset, Area area) => Grid.GetSubspace(offset, area);

    /// <summary>
    /// Values of any type, mapped to cell values one at a time. <paramref name="map"/> is where
    /// blankness is decided: return <see cref="CellValue.Blank"/> for whatever this source considers
    /// an empty cell.
    /// </summary>
    public static ArraySpace Create<T>(T[,] values, Func<T, CellValue> map)
    {
      var cells = new CellValue[values.GetLength(0), values.GetLength(1)];

      for (int row = 0; row < values.GetLength(0); row++)
        for (int column = 0; column < values.GetLength(1); column++)
          cells[row, column] =
            map(values[row, column])
            ?? throw new ArgumentException($"Map returned null for the value at column {column}, row {row}.", nameof(map));

      return new ArraySpace(cells);
    }

    /// <summary>Numbers, with <paramref name="isBlank"/> deciding which count as empty cells.</summary>
    public static ArraySpace Create(int[,] values, Func<int, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    /// <inheritdoc cref="Create(int[,], Func{int, bool})"/>
    public static ArraySpace Create(double[,] values, Func<double, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    /// <summary>Text, where this adapter's blankness default is that null or empty is an empty cell.</summary>
    public static ArraySpace Create(string?[,] values)
      => Create(values, v => string.IsNullOrEmpty(v) ? CellValue.Blank : CellValue.Of(v));
  }
}
