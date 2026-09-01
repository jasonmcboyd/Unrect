using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A rectangular block of cells, addressable by coordinate, row, or column.
  /// </summary>
  public sealed class CellBlock
  {
    // Views are built per projection and are not covered by the shape thread-safety guarantee; the
    // caches race benignly (reference assignment is atomic, so the worst case is duplicated work).
    private IReadOnlyList<CellStrip>? _rows;
    private IReadOnlyList<CellStrip>? _columns;

    internal CellBlock(ISpace space)
    {
      Space = space;
    }

    public ISpace Space { get; }

    public int Width => Space.Area.Size.Width;
    public int Height => Space.Area.Size.Height;

    public CellValue this[int column, int row]
    {
      get
      {
        if (column < 0 || column >= Width)
          throw new ArgumentOutOfRangeException(nameof(column), column, $"The block is {Width} columns wide.");
        if (row < 0 || row >= Height)
          throw new ArgumentOutOfRangeException(nameof(row), row, $"The block is {Height} rows tall.");

        return Space[column, row];
      }
    }

    public CellStrip Row(int index)
    {
      if (index < 0 || index >= Height)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Height} rows tall.");

      return new CellStrip(Space.GetSubspace(new Offset(0, index), new Area(Width, 1)), Orientation.Horizontal);
    }

    public CellStrip Column(int index)
    {
      if (index < 0 || index >= Width)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Width} columns wide.");

      return new CellStrip(Space.GetSubspace(new Offset(index, 0), new Area(1, Height)), Orientation.Vertical);
    }

    public IReadOnlyList<CellStrip> Rows => _rows ??= Build(Height, Row);
    public IReadOnlyList<CellStrip> Columns => _columns ??= Build(Width, Column);

    private static IReadOnlyList<CellStrip> Build(int count, Func<int, CellStrip> select)
    {
      var strips = new CellStrip[count];

      for (var index = 0; index < count; index++)
        strips[index] = select(index);

      return strips;
    }
  }
}
