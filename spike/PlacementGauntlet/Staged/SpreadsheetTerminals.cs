using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — the terminals a BACKEND package would add, standing in for what
  /// <c>Unrect.Spreadsheets</c> would ship beside <c>Formula()</c>.
  /// <para>
  /// <b>Finding.</b> The terminal spread is extensible from outside <c>Unrect</c> after all, but only
  /// for terminals that state no type argument. These two are extension methods, so their type
  /// arguments are inferred from the receiver alone; a hypothetical
  /// <c>stage.Fields&lt;T&gt;()</c>-shaped backend terminal could not be written this way, for the
  /// all-or-none reason that forced the stages to be classes.
  /// </para>
  /// </summary>
  public static class SpreadsheetTerminals
  {
    /// <summary>The formula behind the cell this pipeline places — <c>On(total).Right(3).Formula()</c>.</summary>
    public static IProjection<IFormulaSpace, string?> Formula(this PlacementStage stage)
      => stage.Of(SpreadsheetProjections.Formula());

    /// <summary>
    /// The scoped twin. The stage's demand and the leaf's must be reconciled, and C# has no "the
    /// more demanding of the two", so the constraint states it: this terminal is available only on a
    /// pipeline already scoped to a space that carries formulas.
    /// </summary>
    public static IProjection<TSpace, string?> Formula<TSpace>(this PlacementStage<TSpace> stage)
      where TSpace : class, ISpace, IFormulaSpace
      => stage.Of<string?>(SpreadsheetProjections.Formula());
  }
}
