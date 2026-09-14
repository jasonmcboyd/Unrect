namespace Unrect.Core
{
  /// <summary>
  /// The canonical surface of a grid: its extent, and the three questions anything may ask of a cell
  /// without knowing what kind of data lies behind it — whether the cell is empty, whether it says a
  /// word of its own, and what it says.
  /// <para>
  /// One canonical surface, not one per capability: a backend that can do more says so by adding an
  /// interface of its own, never by answering these four differently.
  /// </para>
  /// <para>
  /// Every member is bounds-checked on the same terms: a coordinate outside <see cref="Area"/> is an
  /// <see cref="OutOfBoundsException"/>, because running off the edge of a space is a statement about
  /// the data rather than a bug in the reader — it is how a declaration discovers it has run out of
  /// room, and the projection layer classifies it as a recoverable bounds condition.
  /// </para>
  /// </summary>
  public interface ISpace
  {
    /// <summary>The space's own extent.</summary>
    Area Area { get; }

    /// <summary>
    /// Whether the cell at <paramref name="column"/>, <paramref name="row"/> carries no value at
    /// all. Blankness is decided where the data is adapted, not here.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    bool IsBlank(int column, int row);

    /// <summary>
    /// Whether the cell's canonical text is its own value: true for a cell holding words, false for
    /// a blank, and false for every cell <see cref="AsText"/> has to render. Text matching asks this
    /// first, which is why a numeric 42 is not a cell saying "42".
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    bool IsText(int column, int row);

    /// <summary>
    /// What the cell says: its own string where <see cref="IsText"/> is true, otherwise the
    /// rendering the backend chose for it. Null exactly where <see cref="IsBlank"/> is true.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    string? AsText(int column, int row);
  }
}
