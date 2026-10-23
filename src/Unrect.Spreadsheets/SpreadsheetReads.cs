using Unrect.Core;
using Unrect.Fluent;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What a declaration can read from a cell of a spreadsheet that it could not read from any grid:
  /// the formula behind it. The reads a sheet answers for everyone — text, number, date, boolean —
  /// are <see cref="PointProjectionReads"/> and <see cref="LayoutCursorReads"/>; this is the one
  /// beside them that demands a capability, and its constraint says so: <c>Point().Formula()</c>
  /// compiles only in a file whose space carries formulas, so a declaration that reads one cannot
  /// be applied to a space that has none. That is a compile error, not a run-time surprise.
  /// <para>
  /// A matcher for a formula is spelled with the point reads it already has:
  /// <c>Rows.WithCell(p =&gt; p.HasFormula())</c>, or <c>Rows.WithCell(p =&gt; p.Formula() is string f
  /// &amp;&amp; f.IndexOf("SUBTOTAL", StringComparison.OrdinalIgnoreCase) &gt;= 0)</c> for one that
  /// mentions something.
  /// </para>
  /// </summary>
  public static class SpreadsheetReads
  {
    /// <summary>
    /// The cell, read as the formula behind it: the file's own expression without the leading
    /// <c>=</c>, and null where the cell holds a plain value.
    /// <para>
    /// Null is the honest answer at a projection site, and it says one thing only — <em>that cell
    /// is not computed</em>. It never means "this space could not tell me", because a declaration
    /// containing this read cannot be applied to a space that does not carry formulas.
    /// </para>
    /// <para>
    /// A cell has both a value and a formula, and reading both is an overlay's job rather than a
    /// flow's: <c>Overlay(o =&gt; new Line(o.Decimal(), o.Formula()))</c> hands each child the same
    /// cell, where a flow would step past it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the point is declared over; anything carrying formulas.</typeparam>
    /// <param name="point">The cell to read.</param>
    public static IProjectionDefinition<TSpace, string?> Formula<TSpace>(this IProjectionDefinition<TSpace, Point<TSpace>> point)
      where TSpace : class, IFormulaSpace
      => CellReadLeaves.Over<TSpace, string?>(point, "Formula", ReadFormula);

    /// <summary>The next cell of a layout, read as the formula behind it; see <see cref="Formula{TSpace}(IProjectionDefinition{TSpace, Point{TSpace}})"/>.</summary>
    /// <typeparam name="TSpace">The space the layout is declared over; anything carrying formulas.</typeparam>
    /// <param name="cursor">The layout's cursor.</param>
    public static string? Formula<TSpace>(this LayoutCursor<TSpace> cursor)
      where TSpace : class, IFormulaSpace
      => cursor.Next(ProjectionBuilders<TSpace>.Point().Formula(), declared: null);

    /// <summary>Total: a cell with no formula reads as null, which is an answer and not a failure.</summary>
    private static bool ReadFormula<TSpace>(Point<TSpace> cell, out string? value, out CellProblem? problem)
      where TSpace : class, IFormulaSpace
    {
      value = cell.Formula();
      problem = null;
      return true;
    }
  }
}
