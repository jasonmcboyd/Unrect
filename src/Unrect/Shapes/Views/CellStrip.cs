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

    public ISpace Space { get; }

    private Orientation Orientation { get; }

    /// <summary>Where the strip starts, relative to the space <c>Map</c> was called with.</summary>
    private Offset Origin { get; }

    public int Count => Orientation == Orientation.Horizontal ? Space.Area.Size.Width : Space.Area.Size.Height;

    public CellValue this[int index]
    {
      get
      {
        Validate(index);

        return Orientation == Orientation.Horizontal ? Space[index, 0] : Space[0, index];
      }
    }

    /// <summary>The address of the strip's first cell.</summary>
    public ShapeLocation Location => ShapeLocation.At(Origin, Space.Area.Size);

    /// <summary>The address of one cell of the strip, for citing it in a message.</summary>
    public ShapeLocation AddressOf(int index)
    {
      Validate(index);

      return ShapeLocation.At(Origin + Step(index), Space.Area.Size);
    }

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
