using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="RowLandmarks"/>.</summary>
  public static class ColumnLandmarks
  {
    /// <summary>
    /// A matcher that says what it was looking for. The description is the negative noun phrase a
    /// failure renders — "no column with the label 'EIN'" — so a projection that anchors on
    /// something other than a caption can still fail in the vocabulary's own voice.
    /// </summary>
    public static ILineLandmark<TSpace> ColumnWhere<TSpace>(Func<Plane<TSpace>, int, bool> predicate, string description)
      where TSpace : class, ISpace
      => new PredicateColumnLandmark<TSpace>(NotNull(predicate, nameof(predicate)), NotNull(description, nameof(description)));

    /// <summary>The first column satisfying <paramref name="predicate"/>, described generically as "no matching column" when it fails.</summary>
    public static ILineLandmark<TSpace> ColumnWhere<TSpace>(Func<Plane<TSpace>, int, bool> predicate)
      where TSpace : class, ISpace
      => new PredicateColumnLandmark<TSpace>(NotNull(predicate, nameof(predicate)), "no matching column");

    /// <summary>The first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static ILineLandmark<TSpace> ColumnWithCell<TSpace>(Func<Point<TSpace>, bool> anyCell)
      where TSpace : class, ISpace
      => new PredicateColumnLandmark<TSpace>(
        CellMatching.AnyCellInColumn(NotNull(anyCell, nameof(anyCell))),
        "no column with a matching cell");

    /// <summary>
    /// The first column in which some cell <em>says</em> <paramref name="text"/> — the transpose of
    /// <see cref="RowLandmarks.RowSaying"/>, with the same rule and the same reasons.
    /// </summary>
    public static ILineLandmark<TSpace> ColumnSaying<TSpace>(string text)
      where TSpace : class, ISpace
      => new PredicateColumnLandmark<TSpace>(
        CellMatching.AnyCellInColumn(CellMatching.SaysEquals<TSpace>(NotNull(text, nameof(text)))),
        $"no column saying '{text}'");

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
