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
  /// case-insensitive. <c>Saying</c> is that same equality against what a cell renders as,
  /// whatever kind it is — the one rule here that looks past a cell's kind, and so the one a
  /// declaration has to ask for by name.
  /// </para>
  /// </summary>
  public static class RowLandmarks
  {
    /// <summary>
    /// A matcher that says what it was looking for. The description is the negative noun phrase a
    /// failure renders — "no row with the label 'EIN'" — so a projection that anchors on something
    /// other than a caption can still fail in the vocabulary's own voice.
    /// </summary>
    public static IRowLandmark RowWhere(Func<Plane<ISpace>, int, bool> predicate, string description)
      => new PredicateRowLandmark(NotNull(predicate, nameof(predicate)), NotNull(description, nameof(description)));

    /// <summary>The first row satisfying <paramref name="predicate"/>, described generically as "no matching row" when it fails.</summary>
    public static IRowLandmark RowWhere(Func<Plane<ISpace>, int, bool> predicate)
      => new PredicateRowLandmark(NotNull(predicate, nameof(predicate)), "no matching row");

    /// <summary>The first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IRowLandmark RowWithCell(Func<Point<ISpace>, bool> anyCell)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(NotNull(anyCell, nameof(anyCell))),
        "no row with a matching cell");

    /// <summary>
    /// The first row in which some cell <em>says</em> <paramref name="text"/> — the same whole-cell
    /// comparison as a value vocabulary's <c>RowContaining</c>, trimmed and case-insensitive, against every cell's
    /// rendering rather than against text cells alone.
    /// <para>
    /// This is the opt-in one. A numeric 42 says "42", a date says its ISO form, a boolean says
    /// <c>TRUE</c> and an error says <c>#DIV/0!</c>; none of them is found by <c>Containing</c>, and
    /// all of them are found by this. That is deliberate both ways: a rendering is the backend's
    /// choice rather than the cell's content, so a declaration that wants to anchor on one says so.
    /// </para>
    /// <para>
    /// There is no numeric overload, and there will not be: it would make the declaration responsible
    /// for knowing how the backend spells a number, which is the one thing this family exists to keep
    /// out of a declaration.
    /// </para>
    /// </summary>
    public static IRowLandmark RowSaying(string text)
      => new PredicateRowLandmark(
        CellMatching.AnyCellInRow(CellMatching.SaysEquals(NotNull(text, nameof(text)))),
        $"no row saying '{text}'");

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);
  }
}
