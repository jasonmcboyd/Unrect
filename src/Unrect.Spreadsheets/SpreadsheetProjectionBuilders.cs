using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The spreadsheet vocabulary as a file scope, imported beside
  /// <c>ProjectionBuilders&lt;TSpace&gt;</c> and closed over the same space:
  /// <code>
  /// using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  /// using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var line = Overlay(o =&gt; new Line(o.Next(Decimal()), o.Next(Formula().Right(1))));
  /// </code>
  /// <para>
  /// <b>The backend rule: disjoint member names.</b> Two <c>using static</c> imports coexist
  /// exactly when they share no simple name — ambiguity is reported per name, at the call site, and
  /// never for a name only one of them publishes. So a backend re-exports its OWN vocabulary and
  /// nothing else: <c>Formula</c>, <c>RowWithFormula</c>, <c>ColumnWithFormula</c> and the
  /// <c>Formulas</c> witness appear here because <c>Unrect</c> cannot name them, and not one member
  /// of the core vocabulary is repeated here, because repeating one would make it unspellable in
  /// every file that imports both. It is the same rule that forbids importing
  /// <c>Projection</c> beside the closed class — stated from the other side.
  /// </para>
  /// <para>
  /// Every member is raised from the capability it needs to the space the file is written over,
  /// which is what a file scope is for: <c>Formula()</c> demands <c>IFormulaSpace</c> and reads as
  /// an <typeparamref name="TSpace"/> declaration here, so a matcher and a leaf and a layout in one
  /// file all speak of one space. A shared helper wants the opposite — the narrowest demand it
  /// actually reads — and is written against <see cref="SpreadsheetProjections"/> directly, where
  /// <c>Formula()</c> keeps its <c>IFormulaSpace</c> demand and composes into anything able to
  /// answer it.
  /// </para>
  /// <para>
  /// <typeparamref name="TSpace"/> is constrained to <see cref="ISpreadsheetSpace"/> rather than to
  /// the one capability today's members happen to need: the bundle is this package's versioning
  /// commitment, so a capability joining it later joins this class without moving its constraint.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class SpreadsheetProjectionBuilders<TSpace>
    where TSpace : class, ISpreadsheetSpace
  {
    /// <inheritdoc cref="SpreadsheetProjections.Formulas"/>
    public static Demand<TSpace> Formulas => Demand<TSpace>.Instance;

    /// <inheritdoc cref="SpreadsheetProjections.Formula()"/>
    public static IProjection<TSpace, string?> Formula() => SpreadsheetProjections.Formula();

    /// <inheritdoc cref="SpreadsheetProjections.RowWithFormula()"/>
    public static IRowLandmark<TSpace> RowWithFormula() => SpreadsheetProjections.RowWithFormula();

    /// <inheritdoc cref="SpreadsheetProjections.RowWithFormula(string)"/>
    /// <param name="containing">The text the formula must mention.</param>
    public static IRowLandmark<TSpace> RowWithFormula(string containing)
      => SpreadsheetProjections.RowWithFormula(containing);

    /// <inheritdoc cref="SpreadsheetProjections.ColumnWithFormula()"/>
    public static IColumnLandmark<TSpace> ColumnWithFormula() => SpreadsheetProjections.ColumnWithFormula();

    /// <inheritdoc cref="SpreadsheetProjections.ColumnWithFormula(string)"/>
    /// <param name="containing">The text the formula must mention.</param>
    public static IColumnLandmark<TSpace> ColumnWithFormula(string containing)
      => SpreadsheetProjections.ColumnWithFormula(containing);
  }
}
