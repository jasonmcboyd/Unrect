namespace Unrect.Core
{
  /// <summary>
  /// The canonical surface of a grid: its extent, and the three questions anything may ask of a cell
  /// without knowing what kind of data lies behind it — whether the cell is empty, what it says, and
  /// the text it holds, if it holds any.
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
    /// Whether the cell at <paramref name="column"/>, <paramref name="row"/> carries no value at
    /// all. Blankness is decided where the data is adapted, not here.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    bool IsBlank(int column, int row);

    /// <summary>
    /// What the cell says, whatever it holds: a word says itself, and anything else says the
    /// rendering the backend chose for it. Total — every cell that is not blank says something — and
    /// null exactly where <see cref="IsBlank"/> is true. It is a rendering and never a reading: that
    /// a cell says "42" does not mean it holds the text "42".
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    string? AsText(int column, int row);

    /// <summary>
    /// The text the cell holds, if text is what it holds: true with the cell's own string for a cell
    /// holding words, false for a blank and for every cell <see cref="AsText"/> has to render. Text
    /// matching asks this, which is why a numeric 42 is not a cell holding "42".
    /// <para>
    /// A refusal may say why in <paramref name="problem"/>, in the vocabulary of the store the space
    /// reads; a space with nothing particular to say leaves it null, and the reader is told what was
    /// expected and what the cell says instead (<see cref="CellProblem.Expected"/>).
    /// </para>
    /// </summary>
    /// <param name="column">The cell's column.</param>
    /// <param name="row">The cell's row.</param>
    /// <param name="value">The cell's own text, when the answer is true.</param>
    /// <param name="problem">Why not, when the answer is false and the space has a reason to give.</param>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Area"/>.</exception>
    bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem);
  }
}
