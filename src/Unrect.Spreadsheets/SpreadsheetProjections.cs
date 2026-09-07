using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What a declaration can say about a spreadsheet that it could not say about any grid: the
  /// formula behind a cell, and the two matchers that look for one.
  /// <para>
  /// Import it beside the vocabulary itself — <c>using static Unrect.Projections.Projection;</c>
  /// and <c>using static Unrect.Spreadsheets.SpreadsheetProjections;</c> — and the demand climbs
  /// out of the leaves by inference, with the space named nowhere until <c>Map</c>. A declaration
  /// that uses any of these cannot be applied to a space that does not carry formulas: that is a
  /// compile error, not a run-time surprise.
  /// </para>
  /// <para>
  /// <b>There is no reach-through.</b> A <c>row.FormulaAt(2)</c> extension would compile against
  /// any table and read null over a plain grid, and nothing in its type would say the declaration
  /// needs formulas — the trapped-knowledge shape this vocabulary exists to avoid. The capability
  /// is spelled as a leaf so that composing it raises a demand.
  /// </para>
  /// </summary>
  public static class SpreadsheetProjections
  {
    /// <summary>
    /// The formula capability's witness: <c>VerticalFlow(Formulas, v =&gt; …)</c> and
    /// <c>rows.Demanding(Formulas)</c> state the requirement where nothing in the surrounding types
    /// could say it — a layout whose demand lives in its lambda's body, or a projection lambda that
    /// reaches a capability the type system cannot see into.
    /// </summary>
    public static Demand<IFormulaSpace> Formulas => Demand<IFormulaSpace>.Instance;

    /// <summary>
    /// One cell, read as the formula behind it: the file's own expression without the leading
    /// <c>=</c>, and null where the cell holds a plain value.
    /// <para>
    /// Null is the honest answer at a projection site, and it says one thing only — <em>that cell
    /// is not computed</em>. It never means "this space could not tell me", because a declaration
    /// containing this leaf cannot be applied to a space that does not carry formulas.
    /// </para>
    /// <para>
    /// A cell has both a value and a formula, and reading both is an overlay's job rather than a
    /// flow's: <c>Overlay(o =&gt; new Line(o.Next(Decimal()), o.Next(Formula())))</c> hands each
    /// child the same cell, where a flow would step past it.
    /// </para>
    /// <para>
    /// It is <c>Named</c>, unlike every leaf in the core vocabulary, and that is a cost rather than
    /// a choice: a projection class cannot be authored outside <c>Unrect</c>, so a backend's leaf
    /// is built from a public factory and inherits that factory's description. Naming it makes a
    /// failure path say <c>Formula</c> instead of <c>Range(1, 1)</c>; the price is that a hoisted
    /// <c>Formula()</c> cannot borrow the identifier at its use site the way <c>Text()</c> can.
    /// </para>
    /// </summary>
    public static IProjection<IFormulaSpace, string?> Formula()
      => Projection.Range(1, 1, cell => cell.Space.Capability<IFormulaSpace>()?.FormulaAt(0, 0))
        .Named("Formula")
        .Demanding(Formulas);

    /// <summary>
    /// The first row holding a formula anywhere in it — the boundary form, for a section that
    /// starts or ends where a sheet stops stating figures and starts computing them.
    /// <para>
    /// A matcher only locates; the lift decides what absence means, as everywhere else. What is
    /// different here is absence of the <em>capability</em>: that is a fault, never a no-match, so
    /// no <c>Optional</c> or <c>Else</c> can absorb a wrong backend as an absent section. Under the
    /// typed layer it is unreachable — a lift that takes this matcher demands the capability — and
    /// it is implemented anyway, for the declaration that reaches the plain lift through
    /// <c>Landmark</c>.
    /// </para>
    /// </summary>
    public static IRowLandmark<IFormulaSpace> RowWithFormula() => new FormulaRowLandmark(null);

    /// <summary>
    /// The first row holding a formula that mentions <paramref name="containing"/> —
    /// <c>RowWithFormula("SUBTOTAL")</c> finds the row a report totals itself on.
    /// <para>
    /// <b>A substring, case-insensitively</b>, and deliberately not the whole-cell rule
    /// <c>RowContaining</c> uses. A caption is a value and matching part of one invites false
    /// anchors; a formula is an expression, and the only useful question about one is whether it
    /// mentions something — a function, a sheet, a column.
    /// </para>
    /// </summary>
    /// <param name="containing">The text the formula must mention.</param>
    public static IRowLandmark<IFormulaSpace> RowWithFormula(string containing)
      => new FormulaRowLandmark(NotEmpty(containing));

    /// <inheritdoc cref="RowWithFormula()"/>
    public static IColumnLandmark<IFormulaSpace> ColumnWithFormula() => new FormulaColumnLandmark(null);

    /// <inheritdoc cref="RowWithFormula(string)"/>
    /// <param name="containing">The text the formula must mention.</param>
    public static IColumnLandmark<IFormulaSpace> ColumnWithFormula(string containing)
      => new FormulaColumnLandmark(NotEmpty(containing));

    private static string NotEmpty(string containing)
      => string.IsNullOrEmpty(containing)
        ? throw new ArgumentException("A formula matcher needs something to look for.", nameof(containing))
        : containing;

    /// <summary>
    /// The half the two matchers share: what counts as a hit, how a failure describes itself, and
    /// the demand a boundary makes of the space before it looks at all.
    /// </summary>
    private abstract class FormulaLandmark
    {
      private readonly string? _containing;
      private readonly string _demandedBy;

      protected FormulaLandmark(string axis, string? containing)
      {
        _containing = containing;
        _demandedBy = containing is null ? $"{axis}WithFormula()" : $"{axis}WithFormula(\"{containing}\")";
        Description = containing is null
          ? $"no {axis.ToLowerInvariant()} with a formula"
          : $"no {axis.ToLowerInvariant()} with a formula mentioning '{containing}'";
      }

      /// <summary>The negative noun a failure renders, in the matcher family's own voice.</summary>
      public string Description { get; }

      /// <summary>
      /// The capability, or a fault. A boundary that cannot look must not answer "not there": that
      /// would be a claim about the document made by a reader describing itself.
      /// </summary>
      protected IFormulaSpace Formulas(ISpace space) => space.RequiredCapability<IFormulaSpace>(_demandedBy);

      protected bool Matches(string? formula)
        => formula is not null
          && (_containing is null || formula.IndexOf(_containing, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private sealed class FormulaRowLandmark : FormulaLandmark, IRowLandmark<IFormulaSpace>, IRowLandmark
    {
      internal FormulaRowLandmark(string? containing)
        : base("Row", containing)
      {
      }

      IRowLandmark IRowLandmark<IFormulaSpace>.Landmark => this;

      public int? FindRow(ISpace space)
      {
        var formulas = Formulas(space);

        for (var row = 0; row < space.Area.Height; row++)
          for (var column = 0; column < space.Area.Width; column++)
            if (Matches(formulas.FormulaAt(column, row)))
              return row;

        return null;
      }
    }

    private sealed class FormulaColumnLandmark : FormulaLandmark, IColumnLandmark<IFormulaSpace>, IColumnLandmark
    {
      internal FormulaColumnLandmark(string? containing)
        : base("Column", containing)
      {
      }

      IColumnLandmark IColumnLandmark<IFormulaSpace>.Landmark => this;

      public int? FindColumn(ISpace space)
      {
        var formulas = Formulas(space);

        for (var column = 0; column < space.Area.Width; column++)
          for (var row = 0; row < space.Area.Height; row++)
            if (Matches(formulas.FormulaAt(column, row)))
              return column;

        return null;
      }
    }
  }
}
