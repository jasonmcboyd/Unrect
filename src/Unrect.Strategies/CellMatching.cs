using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The one place that decides what matching text means — and it deliberately holds more than one
  /// rule, because more than one question is being asked.
  /// <para>
  /// <see cref="TextEquals"/> is <b>content matching</b>: a cell against a literal the declaration
  /// wrote. Every caller of it must agree, or a section would assert one thing and be bounded by
  /// another — <c>Caption("Total")</c> and <c>RowContaining("Total")</c> have to find the same row.
  /// <see cref="TextComparer"/> is that same rule as a lookup key comparer, for a caller who needs
  /// it by the tableful; agreement between the two is by construction, not by resemblance.
  /// </para>
  /// <para>
  /// <see cref="LabelEquals"/> is <b>label matching</b>: the same question, narrowed for the one
  /// place the matched text is known to be a label, where a trailing colon is presentation.
  /// </para>
  /// <para>
  /// <see cref="SaysEquals"/> is <b>rendering matching</b>, and the only rule here that looks past
  /// a cell's kind. The other two ask what the cell <em>holds</em> and so see text cells alone —
  /// which is why a numeric 42 is not a row containing "42". This one asks what the cell
  /// <em>says</em>, and a declaration reaches it by writing <c>Saying</c> rather than
  /// <c>Containing</c>, so the widening is always something someone asked for.
  /// </para>
  /// <para>
  /// A fourth rule lives elsewhere and must not be folded in here: <c>CaptionComparer</c> bridges a
  /// caption and a C# identifier, and so ignores whitespace <em>everywhere</em>. That is meaningful
  /// between two identifier spaces and harmful in a content matcher, where it would let
  /// <c>RowContaining("Net Income")</c> match a cell reading <c>"NetIncome"</c>. Four rules is three
  /// more than anyone wants; each is scoped on purpose, and they are not to be unified.
  /// </para>
  /// </summary>
  internal static class CellMatching
  {
    public static Func<Plane<ISpace>, int, bool> AnyCellInRow(Func<Point<ISpace>, bool> cell)
      => (space, row) =>
      {
        for (var column = 0; column < space.Width; column++)
          if (cell(space[column, row]))
            return true;

        return false;
      };

    public static Func<Plane<ISpace>, int, bool> AnyCellInColumn(Func<Point<ISpace>, bool> cell)
      => (space, column) =>
      {
        for (var row = 0; space.HasRow(row); row++)
          if (cell(space[column, row]))
            return true;

        return false;
      };

    /// <summary>
    /// Content matching after a trailing run of <c>':'</c> and whitespace is removed from both
    /// sides, so <c>Field("EIN")</c> matches a cell reading <c>EIN</c>, <c>EIN:</c> or <c>EIN :</c>.
    /// <para>
    /// A trailing colon is presentation of a label, not part of it — the same export writes it one
    /// year and drops it the next. The rule is confined to <c>Field</c>, the one place the matched
    /// text is known to be a label, and covers the colon alone: every character we agree to ignore
    /// is a character a label may no longer contain.
    /// </para>
    /// </summary>
    public static Func<Point<ISpace>, bool> LabelEquals(string label)
    {
      var needle = TrimLabel(label);

      return point => point.IsText && Comparison.Equals(TrimLabel(point.AsText()!), needle);
    }

    /// <summary>
    /// The label rule between two strings, for a caller holding both — a declaration checking its
    /// own labels apart before it has any cells to ask about. The same two halves the predicate is
    /// built from, so the rule stays one implementation.
    /// </summary>
    public static bool LabelsMatch(string first, string second) => Comparison.Equals(TrimLabel(first), TrimLabel(second));

    /// <summary>Strips a trailing run of colons and whitespace, so "EIN: :" reduces to "EIN".</summary>
    private static string TrimLabel(string text)
    {
      var trimmed = Trimmed(text);

      while (trimmed.Length > 0 && (trimmed[trimmed.Length - 1] == ':' || char.IsWhiteSpace(trimmed[trimmed.Length - 1])))
        trimmed = trimmed.Substring(0, trimmed.Length - 1);

      return trimmed;
    }

    /// <summary>
    /// Whole-cell equality, trimmed and case-insensitive. Not a substring: labels are cell values,
    /// and substring matching invites false anchors.
    /// </summary>
    public static Func<Point<ISpace>, bool> TextEquals(string text)
    {
      var needle = Trimmed(text);

      return point => point.IsText && Comparison.Equals(Trimmed(point.AsText()!), needle);
    }

    /// <summary>
    /// The same whole-cell comparison against what a cell <em>says</em>, whatever kind it is — the
    /// rule behind <c>RowSaying</c>. It is the one rule here with no text guard, and that is the
    /// whole of the difference: a numeric 42 says "42" and is found by this and by nothing else.
    /// <para>
    /// A rendering is the backend's choice rather than the cell's content, which is why this is the
    /// opt-in rule and <see cref="TextEquals"/> is the default one. Nothing is widened to reach it.
    /// </para>
    /// </summary>
    public static Func<Point<ISpace>, bool> SaysEquals(string text)
    {
      var needle = Trimmed(text);

      return point => point.AsText() is string said && Comparison.Equals(Trimmed(said), needle);
    }

    /// <summary>The trim every rule here begins with — what a cell's edges are allowed to carry.</summary>
    private static string Trimmed(string text) => text.Trim();

    /// <summary>How every rule here compares two texts once they are trimmed — the case policy, in one place.</summary>
    private static readonly StringComparer Comparison = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// The content rule as a lookup key comparer, for a caller that answers the question with a
    /// table rather than a scan — a table of header text, say. A dictionary cannot consult a
    /// predicate, so it consults the two halves the predicate is built from and the rule stays one
    /// implementation. Declared after the halves it reads, so initialization order can never be a
    /// question.
    /// </summary>
    public static IEqualityComparer<string> TextComparer { get; } = new ContentComparer();

    /// <summary>A null has nothing to trim, so the comparison answers for it: two nulls are equal, one is not.</summary>
    private sealed class ContentComparer : IEqualityComparer<string>
    {
      public bool Equals(string? x, string? y)
        => x is null || y is null
          ? Comparison.Equals(x, y)
          : Comparison.Equals(Trimmed(x), Trimmed(y));

      public int GetHashCode(string obj)
        => Comparison.GetHashCode(Trimmed(obj ?? throw new ArgumentNullException(nameof(obj))));
    }
  }
}
