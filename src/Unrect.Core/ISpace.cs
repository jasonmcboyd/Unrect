namespace Unrect.Core
{
  /// <summary>
  /// The canonical surface of a grid: its extent, and the two questions anything may ask of a cell
  /// without knowing what kind of data lies behind it — whether the cell counts as content, and
  /// what it says. They are independent: neither is defined in terms of the other.
  /// The text facet, which every space has because every space can render its cells, and which
  /// carries nothing about kind: a plain CSV is an <see cref="ISpace"/> and nothing more.
  /// <para>
  /// One canonical surface, not one per capability: a backend that can do more says so by adding an
  /// interface of its own — the value facet, <c>IValueSpace&lt;TValue&gt;</c> in Unrect, first among them —
  /// never by answering these differently.
  /// </para>
  /// <para>
  /// Every member is bounds-checked on the same terms: a coordinate outside <see cref="Extent"/> is an
  /// <see cref="OutOfBoundsException"/>, because running off the edge of a space is a statement about
  /// the data rather than a bug in the reader — it is how a declaration discovers it has run out of
  /// room, and the projection layer classifies it as a recoverable bounds condition.
  /// </para>
  /// </summary>
  public interface ISpace
  {
    /// <summary>
    /// The space's own extent — how wide and how tall it is.
    /// <para>
    /// It must be already known: answering it may neither throw nor go and measure anything. A space
    /// is asked how big it is on paths that cannot fail — building a failure's message is one, since
    /// citing a cell needs the extent it sits in — and a backend whose extent were discovered lazily
    /// would turn a read failure into a second failure raised from inside the first one's message.
    /// </para>
    /// </summary>
    Size Extent { get; }

    /// <summary>
    /// Whether the cell at <paramref name="column"/>, <paramref name="row"/> counts as content —
    /// the structural question: a flow steps over blank rows, a size rule stops at them, a nullable
    /// member reads one as null. Each space decides what blank means, by a rule its adapter is
    /// given, and the rule is about presence alone: a blank cell may still say something (a CSV's
    /// <c>NULL</c> marker, a cell of whitespace), and may still carry a formula or a fill.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Extent"/>.</exception>
    bool IsBlankAt(int column, int row);

    /// <summary>
    /// What the cell says, whatever it holds: a word says itself, anything else says the rendering
    /// the backend chose for it, and a cell with nothing in it says the empty string. Total, and
    /// independent of <see cref="IsBlankAt"/>: what a cell says and whether it counts as content are
    /// two questions, so a cell the adapter's rule calls blank still says what it says. It is a
    /// rendering and never a reading: that a cell says "42" does not mean it holds the text "42",
    /// and whether it does is the value facet's question.
    /// </summary>
    /// <exception cref="OutOfBoundsException">The coordinate lies outside <see cref="Extent"/>.</exception>
    string AsTextAt(int column, int row);
  }
}
