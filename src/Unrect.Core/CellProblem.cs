namespace Unrect.Core
{
  /// <summary>
  /// A sentence about a cell, with the cell's address left as a hole to be filled in. A backend that
  /// reads a cell knows what went wrong and where the cell is in its own space; only the projection
  /// layer knows what to call that place, so the address arrives last.
  /// <para>
  /// It is two pieces of text and nothing else, because a refused read is not always a failure: a
  /// predicate that asks a million cells whether they hold a number refuses most of them, and a
  /// reason that cost an allocation to state would be paid for a million times and read never. A
  /// backend keeps the pieces it says often and hands the same ones back.
  /// </para>
  /// </summary>
  public readonly struct CellProblem
  {
    private readonly string? _before;
    private readonly string? _after;

    /// <summary>
    /// The sentence <paramref name="before"/>, the cell's address, <paramref name="after"/>.
    /// </summary>
    /// <param name="before">Everything the sentence says before the address.</param>
    /// <param name="after">Everything it says after.</param>
    public CellProblem(string before, string after)
    {
      _before = before;
      _after = after;
    }

    /// <summary>
    /// What is said of a cell that was asked for <paramref name="expected"/> by a space that gave no
    /// reason of its own: what was wanted, and what the cell says instead. Every space can answer
    /// the two questions it is built from, so no refused read is ever without a sentence.
    /// </summary>
    /// <param name="expected">What the read asked for, as the reader would name it: <c>Text</c>.</param>
    /// <param name="space">The space the cell is in.</param>
    /// <param name="column">The cell's column.</param>
    /// <param name="row">The cell's row.</param>
    public static CellProblem Expected(string expected, ISpace space, int column, int row)
    {
      if (space is null)
        throw new System.ArgumentNullException(nameof(space));

      return new CellProblem(
        "expected " + expected + " at ",
        space.AsText(column, row) is string says ? "; the cell says '" + says + "'" : "; the cell is blank");
    }

    /// <summary>The whole sentence, ready to be read by someone holding a spreadsheet.</summary>
    /// <param name="at">The cell's address, as the projection layer renders it.</param>
    public string Render(string at) => _before + at + _after;
  }
}
