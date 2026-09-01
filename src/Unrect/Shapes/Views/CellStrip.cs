using System;
using System.Collections;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One row or one column of cells, indexed along its own axis.
  /// </summary>
  public sealed class CellStrip : IReadOnlyList<CellValue>
  {
    internal CellStrip(ISpace space, Orientation orientation)
    {
      Space = space;
      Orientation = orientation;
    }

    public ISpace Space { get; }

    private Orientation Orientation { get; }

    public int Count => Orientation == Orientation.Horizontal ? Space.Area.Size.Width : Space.Area.Size.Height;

    public CellValue this[int index]
    {
      get
      {
        if (index < 0 || index >= Count)
          throw new ArgumentOutOfRangeException(nameof(index), index, $"The strip has {Count} cells.");

        return Orientation == Orientation.Horizontal ? Space[index, 0] : Space[0, index];
      }
    }

    public IEnumerator<CellValue> GetEnumerator()
    {
      for (var index = 0; index < Count; index++)
        yield return this[index];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}
