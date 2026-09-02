using System;
using System.Collections.Generic;

namespace Unrect.Shapes
{
  /// <summary>
  /// How a column caption is matched to a member name: ignoring case, and ignoring whitespace
  /// entirely, because a caption may contain spaces and a C# identifier may not. So
  /// <c>"Contribution ITD"</c> binds to <c>ContributionItd</c> with nothing declared.
  /// <para>
  /// <b>This is not how cell content is matched, and the two must not be merged.</b> Content
  /// matching bridges a cell and a literal a human wrote into a document, where the only noise is
  /// presentation whitespace and case at the ends; this bridges two identifier spaces, where
  /// interior whitespace is the difference between a caption and a name. A content matcher that
  /// ignored interior whitespace would let <c>RowContaining("Net Income")</c> match a cell reading
  /// <c>"NetIncome"</c> — a false anchor of exactly the kind whole-cell matching prevents.
  /// </para>
  /// <para>
  /// Nothing but whitespace and case is ignored: punctuation, parentheses and <c>%</c> all count, so
  /// <c>"Net (USD)"</c> needs an explicit caption rather than binding to <c>NetUsd</c>. Every
  /// character this stripped would be a character two captions could collide on.
  /// </para>
  /// <para>
  /// It is public because the dictionaries <c>TableRows()</c> and <c>Fields</c> hand back are built
  /// with it: a consumer who copies one, or builds a lookup beside one, needs to be able to say so.
  /// </para>
  /// </summary>
  public sealed class CaptionComparer : IEqualityComparer<string>
  {
    private CaptionComparer()
    {
    }

    /// <summary>The one instance; it holds no state.</summary>
    public static CaptionComparer Default { get; } = new CaptionComparer();

    /// <summary>
    /// Whether two names are the same caption: equal once whitespace is removed from both and case
    /// is ignored. Two nulls are equal; one null never is.
    /// </summary>
    public bool Equals(string? x, string? y)
    {
      if (ReferenceEquals(x, y))
        return true;

      if (x is null || y is null)
        return false;

      int i = 0, j = 0;

      while (true)
      {
        while (i < x.Length && char.IsWhiteSpace(x[i]))
          i++;
        while (j < y.Length && char.IsWhiteSpace(y[j]))
          j++;

        if (i == x.Length || j == y.Length)
          return i == x.Length && j == y.Length;

        if (char.ToUpperInvariant(x[i]) != char.ToUpperInvariant(y[j]))
          return false;

        i++;
        j++;
      }
    }

    /// <summary>
    /// A hash over the same characters <see cref="Equals(string?, string?)"/> compares — whitespace
    /// skipped, case folded — so equal captions always share a bucket.
    /// </summary>
    public int GetHashCode(string obj)
    {
      if (obj is null)
        throw new ArgumentNullException(nameof(obj));

      var hash = 17;

      foreach (var character in obj)
        if (!char.IsWhiteSpace(character))
          hash = (hash * 31) + char.ToUpperInvariant(character);

      return hash;
    }
  }
}
