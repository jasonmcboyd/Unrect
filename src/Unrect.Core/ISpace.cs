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

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, 0-based from this space's own
    /// origin. Implementations must throw <see cref="OutOfBoundsException"/> for a coordinate
    /// outside <see cref="Area"/>.
    /// <para>
    /// That type, and not <see cref="System.IndexOutOfRangeException"/>, because reading past the
    /// edge of a space is a statement about the data rather than a bug in the reader: it is how a
    /// declaration discovers it has run out of room, and the shape layer classifies it as a
    /// recoverable bounds condition. An <c>IndexOutOfRangeException</c> is on the engine's fault
    /// list — a bug in the code, never absorbed by a tolerance boundary — so a space that threw one
    /// for an ordinary overrun would make that overrun unrecoverable.
    /// </para>
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    CellValue this[int column, int row] { get; }

    /// <summary>
    /// A view onto the rectangle starting at <paramref name="offset"/> and <paramref name="area"/>
    /// wide/tall, sharing the same backing data. Implementations should throw
    /// <see cref="OutOfBoundsException"/> when the requested rectangle does not fit.
    /// </summary>
    ISpace GetSubspace(Offset offset, Area area);
  }
}
