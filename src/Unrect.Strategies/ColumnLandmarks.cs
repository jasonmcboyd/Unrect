using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="RowLandmarks"/>.</summary>
  public static class ColumnLandmarks
  {
    /// <summary>The first column satisfying <paramref name="predicate"/>.</summary>
    public static IColumnLandmark ColumnWhere(Func<ISpace, int, bool> predicate)
      => new PredicateColumnLandmark(NotNull(predicate, nameof(predicate)), "no matching column");

    /// <summary>The first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IColumnLandmark ColumnWithCell(Func<CellValue, bool> anyCell)
      => new PredicateColumnLandmark(
        CellMatching.AnyCellInColumn(NotNull(anyCell, nameof(anyCell))),
        "no column with a matching cell");

    /// <summary>
    /// The first column holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively — the same rule as <c>SeekColumnContaining</c>.
    /// </summary>
    public static IColumnLandmark ColumnContaining(string text)
      => new PredicateColumnLandmark(
        CellMatching.AnyCellInColumn(CellMatching.TextEquals(NotNull(text, nameof(text)))),
        $"no column containing '{text}'");

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
