using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A rectangular block of cells, addressable by coordinate, row, or column, and knowing where it
  /// sits so a caller can cite any of them.
  /// </summary>
  public sealed class CellBlock
  {
    // Views are built per projection and are not covered by the shape thread-safety guarantee; the
    // caches race benignly (reference assignment is atomic, so the worst case is duplicated work).
    private IReadOnlyList<CellStrip>? _rows;
    private IReadOnlyList<CellStrip>? _columns;

    internal CellBlock(ISpace space, Offset origin)
    {
      Space = space;
      Origin = origin;
    }

    public ISpace Space { get; }

    /// <summary>Where the block starts, relative to the space <c>Map</c> was called with.</summary>
    private Offset Origin { get; }

    public int Width => Space.Area.Width;
    public int Height => Space.Area.Height;

    public CellValue this[int column, int row]
    {
      get
      {
        Validate(column, row);

        return Space[column, row];
      }
    }

    /// <summary>The address of the block's top-left cell.</summary>
    public ShapeLocation Location => ShapeLocation.At(Origin, Space.Area.Size);

    /// <summary>The address of one cell of the block, for citing it in a message.</summary>
    public ShapeLocation AddressOf(int column, int row)
    {
      Validate(column, row);

      return ShapeLocation.At(Origin + new Offset(column, row), Space.Area.Size);
    }

    public CellStrip Row(int index)
    {
      if (index < 0 || index >= Height)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Height} rows tall.");

      var offset = new Offset(0, index);
      return new CellStrip(Space.GetSubspace(offset, new Area(Width, 1)), Orientation.Horizontal, Origin + offset);
    }

    public CellStrip Column(int index)
    {
      if (index < 0 || index >= Width)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The block is {Width} columns wide.");

      var offset = new Offset(index, 0);
      return new CellStrip(Space.GetSubspace(offset, new Area(1, Height)), Orientation.Vertical, Origin + offset);
    }

    public IReadOnlyList<CellStrip> Rows => _rows ??= Build(Height, Row);
    public IReadOnlyList<CellStrip> Columns => _columns ??= Build(Width, Column);

    private void Validate(int column, int row)
    {
      if (column < 0 || column >= Width)
        throw new ArgumentOutOfRangeException(nameof(column), column, $"The block is {Width} columns wide.");
      if (row < 0 || row >= Height)
        throw new ArgumentOutOfRangeException(nameof(row), row, $"The block is {Height} rows tall.");
    }

    private static IReadOnlyList<CellStrip> Build(int count, Func<int, CellStrip> select)
    {
      var strips = new CellStrip[count];

      for (var index = 0; index < count; index++)
        strips[index] = select(index);

      return strips;
    }
  }
}
