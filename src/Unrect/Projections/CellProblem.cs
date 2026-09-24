namespace Unrect.Projections
{
  /// <summary>
  /// The reason a read was refused: a sentence awaiting an address. A read that fails knows what it
  /// expected and what it found, and not where the cell sits in the sheet; the layer that knows the
  /// address fills it in last, through <see cref="Render"/>, and nothing is formatted until then.
  /// <para>
  /// Two strings and no closure, so a refusal allocates nothing beyond the halves a reader already
  /// interned: a predicate asking <c>IsDouble()</c> over a million text cells refuses a million times
  /// and builds no sentence.
  /// </para>
  /// </summary>
  public readonly struct CellProblem
  {
    private readonly string? _before;
    private readonly string? _after;

    /// <summary>The sentence in two halves, the address going between them.</summary>
    /// <param name="before">What comes before the address — <c>expected Number at </c>.</param>
    /// <param name="after">What comes after it — <c>, found Text</c>.</param>
    public CellProblem(string before, string after)
    {
      _before = before;
      _after = after;
    }

    /// <summary>The sentence, with the address in it.</summary>
    /// <param name="at">The cell's address as the reader names it — an A1 reference, or a coordinate pair.</param>
    public string Render(string at) => _before + at + _after;
  }
}
