namespace Unrect.Core
{
  /// <summary>
  /// A rectangular grid of <see cref="CellValue"/>s: the transitional interface, the one whose
  /// indexer hands back the struct and whose subspaces are grids in their own right. Everything
  /// that still reads a cell as a <see cref="CellValue"/> — the kinded leaves, the table binder, the
  /// views — takes one of these, and it retires with the struct once they read through points
  /// instead.
  /// <para>
  /// It is an <see cref="ISpace"/>, so anything speaking it also answers the canonical four and a
  /// declaration written over the canonical surface reads through it unchanged. Every
  /// implementation must agree on kind classification and blankness; nothing above this layer
  /// touches a backend type directly.
  /// </para>
  /// </summary>
  public interface ICellValues : ISpace
  {
    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, 0-based from this space's own
    /// origin. Implementations must throw <see cref="OutOfBoundsException"/> for a coordinate
    /// outside <see cref="ISpace.Area"/>.
    /// <para>
    /// That type, and not <see cref="System.IndexOutOfRangeException"/>, because reading past the
    /// edge of a space is a statement about the data rather than a bug in the reader: it is how a
    /// declaration discovers it has run out of room, and the projection layer classifies it as a
    /// recoverable bounds condition. An <c>IndexOutOfRangeException</c> is on the engine's fault
    /// list — a bug in the code, never absorbed by a tolerance boundary — so a space that threw one
    /// for an ordinary overrun would make that overrun unrecoverable.
    /// </para>
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="ISpace.Area"/>.</exception>
    CellValue this[int column, int row] { get; }

    /// <summary>
    /// A view onto the rectangle starting at <paramref name="offset"/> and <paramref name="area"/>
    /// wide/tall, sharing the same backing data. Implementations should throw
    /// <see cref="OutOfBoundsException"/> when the requested rectangle does not fit.
    /// </summary>
    ICellValues GetSubspace(Offset offset, Area area);
  }
}
