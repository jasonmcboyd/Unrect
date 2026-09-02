using System;

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
  /// </para>
  /// <para>
  /// <see cref="LabelEquals"/> is <b>label matching</b>: the same question, narrowed for the one
  /// place the matched text is known to be a label, where a trailing colon is presentation.
  /// </para>
  /// <para>
  /// A third rule lives elsewhere and must not be folded in here: <c>CaptionComparer</c> bridges a
  /// caption and a C# identifier, and so ignores whitespace <em>everywhere</em>. That is meaningful
  /// between two identifier spaces and harmful in a content matcher, where it would let
  /// <c>RowContaining("Net Income")</c> match a cell reading <c>"NetIncome"</c>. Three rules is two
  /// more than anyone wants; each is scoped on purpose, and they are not to be unified.
  /// </para>
  /// </summary>
  internal static class CellMatching
  {
    public static Func<ISpace, int, bool> AnyCellInRow(Func<CellValue, bool> cell)
      => (space, row) =>
      {
        for (var column = 0; column < space.Area.Size.Width; column++)
          if (cell(space[column, row]))
            return true;

        return false;
      };

    public static Func<ISpace, int, bool> AnyCellInColumn(Func<CellValue, bool> cell)
      => (space, column) =>
      {
        for (var row = 0; row < space.Area.Size.Height; row++)
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
    public static Func<CellValue, bool> LabelEquals(string label)
    {
      var needle = TrimLabel(label);

      return cell => cell.TryGetString() is string value
        && TrimLabel(value).Equals(needle, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Strips a trailing run of colons and whitespace, so "EIN: :" reduces to "EIN".</summary>
    private static string TrimLabel(string text)
    {
      var trimmed = text.Trim();

      while (trimmed.Length > 0 && (trimmed[trimmed.Length - 1] == ':' || char.IsWhiteSpace(trimmed[trimmed.Length - 1])))
        trimmed = trimmed.Substring(0, trimmed.Length - 1);

      return trimmed;
    }

    /// <summary>
    /// Whole-cell equality, trimmed and case-insensitive. Not a substring: labels are cell values,
    /// and substring matching invites false anchors.
    /// </summary>
    public static Func<CellValue, bool> TextEquals(string text)
    {
      var needle = text.Trim();

      return cell => cell.TryGetString() is string value
        && value.Trim().Equals(needle, StringComparison.OrdinalIgnoreCase);
    }
  }
}
