using System;

using Unrect;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace TypedSpacesGauntlet
{
  /// <summary>
  /// SPIKE. The reach-through spelling — <c>row.FormulaAt(2)</c> — which the projection model
  /// decided NOT to ship (spec §5: "<c>Formula()</c> is a leaf, not an extension").
  /// <para>
  /// It lives here, in the gauntlet, because scenario 2a is the evidence <em>for</em> that
  /// decision: nothing in this method's type says the declaration around it needs formulas, so it
  /// compiles against any table and answers null over a space that cannot carry them. Keeping it in
  /// the spike lets the contrast stay demonstrable while the shipped package offers only the leaf.
  /// </para>
  /// </summary>
  public static class ReachThrough
  {
    /// <summary>The formula behind one cell of <paramref name="row"/>, or null where there is none.</summary>
    public static string? FormulaAt(this TableRow row, int column)
      => (row ?? throw new ArgumentNullException(nameof(row)))
        .Space.Capability<IFormulaSpace>()?.FormulaAt(column, 0);
  }
}
