using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// What it means for a row or a column to hold a piece of text. Seeks and landmarks both ask the
  /// question, and they must not answer it differently: <c>SeekRowContaining("Total")</c> and
  /// <c>RowContaining("Total")</c> have to find the same row or a declaration that starts at one and
  /// ends at the other would be quietly wrong.
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
