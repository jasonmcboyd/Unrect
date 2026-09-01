using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Landmarks name a row by its content, the way the seeks do — the trio mirrors
  /// <c>OffsetStrategies.SeekRow</c>, <c>SeekRowWhere</c>, <c>SeekRowContaining</c> exactly, and
  /// matches on the same rules, so a shape that starts at a seek can end at the matching landmark.
  /// <para>
  /// The difference is what happens when there is nothing to find: a seek throws, because not
  /// finding a start means the section is absent, while a landmark returns null and lets the shape
  /// bounding itself decide whether a missing end is an error or the end of the sheet.
  /// </para>
  /// </summary>
  public static class RowLandmarks
  {
    /// <summary>The first row satisfying <paramref name="predicate"/>.</summary>
    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate)
      => new PredicateRowLandmark(NotNull(predicate, nameof(predicate)), "no matching row");

    /// <summary>The first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(NotNull(anyCell, nameof(anyCell))),
        "no row with a matching cell");

    /// <summary>
    /// The first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively — the same rule as <c>SeekRowContaining</c>.
    /// </summary>
    public static IRowLandmark RowContaining(string text)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(CellMatching.TextEquals(NotNull(text, nameof(text)))),
        $"no row containing '{text}'");

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
