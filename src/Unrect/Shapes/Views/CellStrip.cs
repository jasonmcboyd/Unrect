using System;
using System.Collections;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One row or one column of cells, indexed along its own axis and knowing where it sits, so a
  /// caller's own complaints about the data can cite a cell the way the framework's do.
  /// </summary>
  public sealed class CellStrip : IReadOnlyList<CellValue>
  {
    internal CellStrip(ISpace space, Orientation orientation, Offset origin)
    {
      Space = space;
      Orientation = orientation;
      Origin = origin;
    }

    /// <summary>The strip's own extent — one cell wide or one cell tall, depending on its orientation.</summary>
    public ISpace Space { get; }

    private Orientation Orientation { get; }

    /// <summary>Where the strip starts, relative to the space <c>Map</c> was called with.</summary>
    private Offset Origin { get; }

    /// <summary>
    /// How many cells the strip holds. A row's length is its extent's width, which is free even where
    /// the height is still being discovered; a column's is that height, and asking settles it.
    /// </summary>
    public int Count => Orientation == Orientation.Horizontal ? BoundedSpace.WidthOf(Space) : Space.Area.Height;

    /// <summary>The cell at <paramref name="index"/> along the strip's own axis; an index outside it throws <see cref="ArgumentOutOfRangeException"/>.</summary>
    public CellValue this[int index]
    {
      get
      {
        Validate(index);

        return Orientation == Orientation.Horizontal ? Space[index, 0] : Space[0, index];
      }
    }

    /// <summary>
    /// The address of the strip's first cell. It carries the extent it was found in, so on one whose
    /// height is still being discovered this settles the bound.
    /// </summary>
    public ShapeLocation Location => ShapeLocation.At(Origin, Space.Area.Size);

    /// <summary>The address of one cell of the strip, for citing it in a message.</summary>
    public ShapeLocation AddressOf(int index)
    {
      Validate(index);

      return ShapeLocation.At(Origin + Step(index), Space.Area.Size);
    }

    /// <summary>The strip's cells, in order.</summary>
    public IEnumerator<CellValue> GetEnumerator()
    {
      for (var index = 0; index < Count; index++)
        yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private Offset Step(int index) => Orientation == Orientation.Horizontal ? new Offset(index, 0) : new Offset(0, index);

    private void Validate(int index)
    {
      if (index < 0 || index >= Count)
        throw new ArgumentOutOfRangeException(nameof(index), index, $"The strip has {Count} cells.");
    }
  }
}
