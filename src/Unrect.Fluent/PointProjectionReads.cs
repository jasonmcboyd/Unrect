using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Fluent
{
  /// <summary>
  /// Every reading of one cell, as an extension on the one cell leaf: <c>Point().Decimal()</c> is
  /// the cell <c>Point()</c> declares, read as a decimal. There is one leaf, and what a declaration
  /// can read from it is whatever the receiver's space answers — these seven over any
  /// <see cref="ISpace"/>, because every space answers the four reads on its contract, and a
  /// backend's own (<c>Formula()</c>) beside them, constrained to the space that carries it.
  /// <para>
  /// Each read keeps whatever the point was given — a placement from <c>On(mark).Point()</c>, a
  /// name — and fails the way a cell leaf always has: a located sentence in the space's own words,
  /// <c>expected Number at B4, found Text</c>, under a path that names the kind.
  /// </para>
  /// </summary>
  public static class PointProjectionReads
  {
    /// <summary>
    /// The cell, read as the text it holds: refuses a cell that holds anything else, in the
    /// space's own words (<c>expected Text at B4, found Number</c>), where <see cref="AsText{TSpace}"/>
    /// takes whatever the cell says.
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, string> Text<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, string>(point, "Text", (Point<TSpace> cell, out string v, out CellProblem? p) => Readings.TryText(cell, out v, out p));

    /// <summary>The cell, read as a number — a <see cref="double"/>, which is what a store holds.</summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, double> Double<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, double>(point, "Double", (Point<TSpace> cell, out double v, out CellProblem? p) => Readings.TryDouble(cell, out v, out p));

    /// <summary>
    /// The cell, read as a number and converted to a <see cref="decimal"/> — the reading for money,
    /// where the binary noise a double carries is rounded away.
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, decimal> Decimal<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, decimal>(point, "Decimal", (Point<TSpace> cell, out decimal v, out CellProblem? p) => Readings.TryDecimal(cell, out v, out p));

    /// <summary>
    /// The cell, read as a whole number. A number that is really there but is fractional or out of
    /// range fails as a conversion, not as a kind — the cell is a number either way.
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, int> Integer<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, int>(point, "Integer", (Point<TSpace> cell, out int v, out CellProblem? p) => Readings.TryInteger(cell, out v, out p));

    /// <summary>
    /// The cell, read as a date or time, verbatim. The time of day is kept: truncating is
    /// consumer-side (<c>.Select(d =&gt; d.Date)</c>), because a reading that silently handed back
    /// less than the cell holds would be the only one that did.
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, DateTime> Date<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, DateTime>(point, "Date", (Point<TSpace> cell, out DateTime v, out CellProblem? p) => Readings.TryDate(cell, out v, out p));

    /// <summary>The cell, read as a boolean.</summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, bool> Boolean<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, bool>(point, "Boolean", (Point<TSpace> cell, out bool v, out CellProblem? p) => Readings.TryBoolean(cell, out v, out p));

    /// <summary>
    /// The cell, read as what it says — the total reading. Every space renders every cell, so the
    /// only thing that can go wrong is that there is nothing there; <c>OrBlank</c> turns that from
    /// a failure into a null. For a cell holding words it is the words; for anything else it is
    /// the space's own rendering, where <see cref="Text{TSpace}"/> would refuse.
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, string> AsText<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, ISpace
      => CellReadLeaves.Over<TSpace, string>(point, "AsText", ReadSaid);

    /// <summary>The total reading: every space renders every cell, so the only thing that can go wrong is that the cell does not count as content.</summary>
    private static bool ReadSaid<TSpace>(Point<TSpace> cell, out string value, out CellProblem? problem)
      where TSpace : class, ISpace
    {
      if (cell.IsBlank())
      {
        value = string.Empty;
        problem = new CellProblem("expected a value at ", ", found a blank cell");
        return false;
      }

      value = cell.AsText();

      problem = null;
      return true;
    }
  }

  /// <summary>
  /// How a reading becomes a leaf. A read over the bare <c>Point()</c> IS the cell leaf it always
  /// was — the one-cell check, blank tolerance and the located failure of <see cref="ReadDefinition{TSpace, TResult}"/>,
  /// under the kind's name, keeping the point's placement and annotations — which is what keeps a
  /// <c>Point().Decimal()</c> and a bound <c>decimal</c> column describing a bad cell identically.
  /// A point something has already wrapped (bounded, headed, padded) cannot be rebuilt as one, so
  /// the reading is applied to what it yields, under the same name.
  /// </summary>
  internal static class CellReadLeaves
  {
    /// <summary>A fresh one-cell leaf of <paramref name="kind"/>, placed as <c>Point()</c> is.</summary>
    internal static IProjectionDefinition<TSpace, T> Leaf<TSpace, T>(string kind, CellRead<TSpace, T> read)
      where TSpace : class, ISpace
      => new ReadDefinition<TSpace, T>(kind, read, Placement<TSpace>.Of(ProjectionBuilders<TSpace>.Extent(1, 1)), blankIsNull: false);

    /// <summary><paramref name="point"/>'s cell read as <paramref name="kind"/>, keeping whatever the point was given.</summary>
    internal static IProjectionDefinition<TSpace, T> Over<TSpace, T>(IProjectionDefinition<TSpace, Point<TSpace>> point, string kind, CellRead<TSpace, T> read)
      where TSpace : class, ISpace
    {
      if (point is null)
        throw new ArgumentNullException(nameof(point));

      if (point is PointDefinition<TSpace>)
        return (IProjectionDefinition<TSpace, T>)new ReadDefinition<TSpace, T>(kind, read, point.Placement, blankIsNull: false).With(point.Annotations);

      return point
        .Select(cell => read(cell, out var value, out var problem) ? value : throw new CellReadException(cell.Erased(), problem!.Value))
        .AsUnit(kind);
    }
  }
}
