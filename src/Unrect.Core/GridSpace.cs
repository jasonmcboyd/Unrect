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
  /// Useful directly, not only to adapters: a test or a script with values already in hand can build
  /// one and skip the file entirely.
  /// </para>
  /// </summary>
  public sealed class GridSpace : ISpace
  {
    /// <summary>
    /// The whole of <paramref name="values"/>, as a space. Blankness is already decided: whatever
    /// produced the array chose which cells are <see cref="CellValue.Blank"/>.
    /// </summary>
    /// <exception cref="ArgumentException">A cell is null.</exception>
    public GridSpace(CellValue[,] values)
      : this(ValidateNoNulls(values), default, new Area(values.GetLength(1), values.GetLength(0)))
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

    public Area Area { get; }

    public CellValue this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Area.Width)
          throw new IndexOutOfRangeException();

        if (row < 0 || row >= Area.Height)
          throw new IndexOutOfRangeException();

        return Values[Offset.Height + row, Offset.Width + column];
      }
    }

    public ISpace GetSubspace(Offset offset, Area area)
    {
      if (offset.Width + area.Width > Area.Width || offset.Height + area.Height > Area.Height)
        throw new OutOfBoundsException();

      return new GridSpace(Values, offset + Offset, area);
    }

    private static CellValue[,] ValidateNoNulls(CellValue[,] values)
    {
      for (int row = 0; row < values.GetLength(0); row++)
        for (int column = 0; column < values.GetLength(1); column++)
          if (values[row, column] is null)
            throw new ArgumentException($"The value at column {column}, row {row} is null.", nameof(values));

      return values;
    }
  }
}
