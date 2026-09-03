using System;

namespace Unrect.Core
{
  /// <summary>
  /// A rectangular grid of <see cref="CellValue"/>s, viewed as an <see cref="ISpace"/>. This is what
  /// every adapter ends up holding: a backend reads its own format, produces one array of canonical
  /// cell values, and hands it here — so the indexing, the bounds checking and the subspace
  /// arithmetic are written once and every adapter agrees on them.
  /// <para>
  /// The array is indexed <c>[row, column]</c>, the way a 2D array literal reads on the page, while
  /// a space is indexed <c>[column, row]</c>, the way a spreadsheet address does. This type is where
  /// that transposition happens, and it is the reason no adapter has to think about it.
  /// </para>
  /// <para>
  /// Slicing is free: a subspace shares the same array and carries an offset, so decomposing a large
  /// sheet allocates nothing but the views themselves.
  /// </para>
  /// <para>
  /// There are two ways in, and which one you want depends on what you are holding. The constructor
  /// takes cells that are <em>already canonical</em> — whatever produced them has settled what each
  /// one is, including which are blank. <see cref="Create{T}(T[,], Func{T, CellValue})"/> takes a
  /// plain array of numbers, strings or anything else and lexes it, which is where <em>blankness is
  /// decided</em>: the one question an adapter must answer that a grid cannot.
  /// </para>
  /// <para>
  /// Useful directly, not only to adapters: a test or a script with values already in hand can build
  /// one and skip the file entirely, and everything above it behaves exactly as it does over a
  /// workbook.
  /// </para>
  /// </summary>
  public sealed class GridSpace : ISpace
  {
    /// <summary>
    /// The whole of <paramref name="values"/>, as a space. Blankness is already decided: whatever
    /// produced the array chose which cells are <see cref="CellValue.Blank"/> — and a cell nobody
    /// filled in is already one, because <c>default(CellValue)</c> is blank.
    /// </summary>
    public GridSpace(CellValue[,] values)
      : this(values, default, new Area(values.GetLength(1), values.GetLength(0)))
    {
    }

    private GridSpace(CellValue[,] values, Offset offset, Area area)
    {
      if (offset.Width + area.Width > values.GetLength(1) || offset.Height + area.Height > values.GetLength(0))
        throw new OutOfBoundsException();

      Values = values;
      Offset = offset;
      Area = area;
    }

    private CellValue[,] Values { get; }
    private Offset Offset { get; }

    /// <inheritdoc/>
    public Area Area { get; }

    /// <inheritdoc/>
    public CellValue this[int column, int row]
    {
      get
      {
        // OutOfBoundsException, per the ISpace contract: running off the edge of a space is a data
        // condition a declaration may recover from, not a bug in the reading code.
        if (column < 0 || column >= Area.Width)
          throw new OutOfBoundsException();

        if (row < 0 || row >= Area.Height)
          throw new OutOfBoundsException();

        return Values[Offset.Height + row, Offset.Width + column];
      }
    }

    /// <inheritdoc/>
    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new GridSpace(Values, offset + Offset, area);
    }

    /// <summary>
    /// Values of any type, mapped to cell values one at a time. <paramref name="map"/> is where
    /// blankness is decided: return <see cref="CellValue.Blank"/> for whatever this source considers
    /// an empty cell.
    /// </summary>
    public static GridSpace Create<T>(T[,] values, Func<T, CellValue> map)
    {
      var cells = new CellValue[values.GetLength(0), values.GetLength(1)];

      for (int row = 0; row < values.GetLength(0); row++)
        for (int column = 0; column < values.GetLength(1); column++)
          cells[row, column] = map(values[row, column]);

      return new GridSpace(cells);
    }

    /// <summary>Numbers, with <paramref name="isBlank"/> deciding which count as empty cells.</summary>
    public static GridSpace Create(int[,] values, Func<int, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    /// <inheritdoc cref="Create(int[,], Func{int, bool})"/>
    public static GridSpace Create(double[,] values, Func<double, bool>? isBlank = null)
      => Create(values, v => isBlank?.Invoke(v) == true ? CellValue.Blank : CellValue.Of(v));

    /// <summary>Text, where the blankness default is that null or empty is an empty cell.</summary>
    public static GridSpace Create(string?[,] values)
      => Create(values, v => string.IsNullOrEmpty(v) ? CellValue.Blank : CellValue.Of(v));
  }
}
