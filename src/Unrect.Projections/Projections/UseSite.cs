namespace Unrect.Projections
{
  /// <summary>
  /// Where a child was written, as far as the compiler could tell: the identifier the declaration
  /// used for it, and which child of its parent it is. The label belongs to the use site rather
  /// than to the projection, so the same projection declared in two places is called two different
  /// things. <c>default</c> is a site nothing was captured at.
  /// </summary>
  public readonly struct UseSite
  {
    private UseSite(string? name, int? ordinal)
    {
      Name = name;
      Ordinal = ordinal;
    }

    /// <summary>The identifier the child was written as, when it was written as a bare one.</summary>
    public string? Name { get; }

    /// <summary>
    /// Which child of its parent this is, counting from one, where that is a meaningful thing to
    /// say. A repeat has one item rather than an nth, so it supplies none.
    /// </summary>
    public int? Ordinal { get; }

    /// <summary>
    /// The lower two rungs of the naming ladder, applied wherever a declaration captures the text
    /// of an argument. A child written as a plain identifier is called that, verbatim — the point
    /// of the label is to lead a reader back to the line that produced it, so humanising it would
    /// break the grep and invent a name nobody wrote. Anything else — an inline factory call, a
    /// member access, a modifier chain — has no name to borrow and falls back to
    /// <paramref name="ordinal"/>, or to its description where there is no ordinal either. The top
    /// rung needs no code: a projection's own name always wins.
    /// </summary>
    internal static UseSite From(string? declared, int? ordinal)
      => new UseSite(IsIdentifier(declared) ? declared : null, ordinal);

    /// <summary>
    /// Whether <paramref name="text"/> is a bare ASCII identifier. Hand-rolled rather than a
    /// regular expression: no dependency, no allocation, and the rule is short enough to read.
    /// </summary>
    private static bool IsIdentifier(string? text)
    {
      if (string.IsNullOrEmpty(text))
        return false;

      if (!IsLetterOrUnderscore(text![0]))
        return false;

      for (var index = 1; index < text.Length; index++)
        if (!IsLetterOrUnderscore(text[index]) && (text[index] < '0' || text[index] > '9'))
          return false;

      return true;
    }

    private static bool IsLetterOrUnderscore(char character)
      => (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || character == '_';
  }
}
