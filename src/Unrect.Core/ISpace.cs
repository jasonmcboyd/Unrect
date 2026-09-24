namespace Unrect.Core
{
  /// <summary>
  /// The canonical surface of a grid: its extent, and the two questions anything may ask of a cell
  /// without knowing what kind of data lies behind it — whether the cell is empty, and what it says.
  /// The text facet, which every space has because every space can render its cells, and which
  /// carries nothing about kind: a plain CSV is an <see cref="ISpace"/> and nothing more.
  /// <para>
  /// One canonical surface, not one per capability: a backend that can do more says so by adding an
  /// interface of its own — the value facet, <see cref="IValueSpace{TValue}"/>, first among them —
  /// never by answering these differently.
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
    /// <summary>
    /// The space's own extent.
    /// <para>
    /// It must be already known: answering it may neither throw nor go and measure anything. A space
    /// is asked how big it is on paths that cannot fail — building an exception's message is one
    /// (<c>CellReadException</c> cites a cell, and citing it needs the extent it sits in), and a
    /// backend whose extent were discovered lazily would turn a read failure into a second failure
    /// raised from inside the first one's <c>Message</c>.
    /// </para>
    /// </summary>
    Area Area { get; }

    /// <summary>
    /// Whether the cell at <paramref name="column"/>, <paramref name="row"/> says nothing. Each
    /// space decides what blank means, by a rule its adapter is given; a blank cell may still carry
    /// a formula or a fill, which are other facets.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    bool IsBlank(int column, int row);

    /// <summary>
    /// What the cell says, whatever it holds: a word says itself, and anything else says the
    /// rendering the backend chose for it. Total — every cell that is not blank says something — and
    /// null exactly where <see cref="IsBlank"/> is true. It is a rendering and never a reading: that
    /// a cell says "42" does not mean it holds the text "42", and whether it does is the value
    /// facet's question.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    string? AsText(int column, int row);
  }
}
