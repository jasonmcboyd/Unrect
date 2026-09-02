using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A matcher names a row by its content. This is the whole vocabulary for "a row that matches",
  /// and every use of it goes through one of three lifts — <c>To</c> and <c>Past</c>, which turn a
  /// match into a placement, and <c>.Until</c>, which turns one into a bound. Because they share
  /// this family, a section can start at one matcher and end at another without the two disagreeing
  /// about what a caption is.
  /// <para>
  /// A matcher only locates: it returns null when there is nothing to find, and never throws.
  /// Deciding what absence means belongs to the lift — required, for a placement; either an error
  /// or "run to the end", for a bound. One locator, a per-use policy.
  /// </para>
  /// <para>
  /// <b>The naming law these three obey, and so does every strategy factory:</b> a bare
  /// <c>Where</c> or <c>While</c> takes a <em>space</em> predicate, <c>(space, index)</c>. A
  /// <em>cell</em> predicate is always marked in the name — <c>WithCell</c>, <c>WhileAll</c>,
  /// <c>WhileAny</c>. Text is <c>Containing</c>, and it means whole-cell equality, trimmed and
  /// case-insensitive.
  /// </para>
  /// </summary>
  public static class RowLandmarks
  {
    /// <summary>The first row satisfying <paramref name="predicate"/>.</summary>
    /// <summary>
    /// A matcher that says what it was looking for. The description is the negative noun phrase a
    /// failure renders — "no row with the label 'EIN'" — so a shape that anchors on something
    /// other than a caption can still fail in the vocabulary's own voice.
    /// </summary>
    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate, string description)
      => new PredicateRowLandmark(NotNull(predicate, nameof(predicate)), NotNull(description, nameof(description)));

    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate)
      => new PredicateRowLandmark(NotNull(predicate, nameof(predicate)), "no matching row");

    /// <summary>The first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(NotNull(anyCell, nameof(anyCell))),
        "no row with a matching cell");

    /// <summary>
    /// The first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively — whole-cell, because labels are cell values and substring matching
    /// invites false anchors.
    /// </summary>
    public static IRowLandmark RowContaining(string text)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(CellMatching.TextEquals(NotNull(text, nameof(text)))),
        $"no row containing '{text}'");

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
