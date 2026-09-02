namespace Unrect.Core
{
  /// <summary>
  /// A rectangular grid of <see cref="CellValue"/>s — what an adapter turns a backend's data into,
  /// and what everything above it decomposes. Every implementation must agree on kind classification
  /// and blankness; nothing above this layer touches a backend type directly.
  /// </summary>
  public interface ISpace
  {
    /// <summary>The space's own extent.</summary>
    Area Area { get; }

    /// <summary>The cell at <paramref name="column"/>, <paramref name="row"/>, 0-based from this space's own origin.</summary>
    CellValue this[int column, int row] { get; }

    /// <summary>
    /// A view onto the rectangle starting at <paramref name="offset"/> and <paramref name="area"/>
    /// wide/tall, sharing the same backing data. Implementations should throw
    /// <see cref="OutOfBoundsException"/> when the requested rectangle does not fit.
    /// </summary>
    ISpace GetSubspace(Offset offset, Area area);
  }
}
