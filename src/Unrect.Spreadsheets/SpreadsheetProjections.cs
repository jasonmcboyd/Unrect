using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// What a declaration can say about a spreadsheet that it could not say about any grid: the
  /// formula behind a cell, and the two matchers that look for one.
  /// <para>
  /// These are the narrowest forms — each demands the one capability it reads, so a shared helper
  /// written against them composes into any file whose space can answer. A file that names its
  /// space once imports <see cref="SpreadsheetProjectionBuilders{TSpace}"/> instead, where every
  /// member is already closed over that space. Either way a declaration that uses one of these
  /// cannot be applied to a space that does not carry formulas: that is a compile error, not a
  /// run-time surprise.
  /// </para>
  /// <para>
  /// <b>There is no reach-through.</b> A <c>row.FormulaAt(2)</c> extension would compile against
  /// any table and read null over a plain grid, and nothing in its type would say the declaration
  /// needs formulas — the trapped-knowledge shape this vocabulary exists to avoid. The capability
  /// is spelled as a leaf so that composing it raises a demand.
  /// </para>
  /// </summary>
  public static partial class SpreadsheetProjections
  {
    // --- The kinded leaves ------------------------------------------------------------------------
    //
    // A cell whose kind the declaration states. The family is closed over what a sheet can be asked
    // and mirrors it 1:1 — six readings over six kinds, because a number is read three ways and two
    // kinds have no leaf at all: Blank and Error are conditions, not values a leaf projects. There
    // is no Long(), Single(), Money() or Enum<T>(): a CLR conversion beyond that set is Select
    // territory (Integer().Select(i => (long)i)), one-way and honest about it.
    //
    // Each is written at the narrowest space that can answer it, so a shared helper composes into
    // any file whose space is a sheet. A file that names its space once imports the closed twins on
    // SpreadsheetProjectionBuilders instead.

    /// <summary>One cell holding text.</summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, string> Text<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, string>("Text", (ISheetCells s, int c, int r, out string v, out CellProblem? p) => s.TextAt(c, r, out v, out p));

    /// <summary>
    /// One cell holding a number, read as a <see cref="decimal"/> — the accessor that keeps a
    /// spreadsheet's exact decimal where the file carried one.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, decimal> Decimal<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, decimal>("Decimal", (ISheetCells s, int c, int r, out decimal v, out CellProblem? p) => s.DecimalAt(c, r, out v, out p));

    /// <summary>
    /// One cell holding a whole number. A number that is really there but is fractional or out of
    /// range fails as a conversion, not as a kind — the cell is a number either way.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, int> Integer<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, int>("Integer", (ISheetCells s, int c, int r, out int v, out CellProblem? p) => s.IntegerAt(c, r, out v, out p));

    /// <summary>One cell holding a number, read as a <see cref="double"/>.</summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, double> Double<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, double>("Double", (ISheetCells s, int c, int r, out double v, out CellProblem? p) => s.DoubleAt(c, r, out v, out p));

    /// <summary>
    /// One cell holding a date or time, verbatim. The time of day is kept: truncating is
    /// consumer-side (<c>Date().Select(d =&gt; d.Date)</c>), because a leaf that silently handed
    /// back less than the cell holds would be the only one in the vocabulary that did.
    /// </summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, DateTime> Date<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, DateTime>("Date", (ISheetCells s, int c, int r, out DateTime v, out CellProblem? p) => s.DateTimeAt(c, r, out v, out p));

    /// <summary>One cell holding a boolean.</summary>
    /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
    public static IProjection<TSpace, bool> Boolean<TSpace>()
      where TSpace : class, ISheetCells
      => Kinded<TSpace, bool>("Boolean", (ISheetCells s, int c, int r, out bool v, out CellProblem? p) => s.BooleanAt(c, r, out v, out p));

    internal static IProjection<TSpace, T> Kinded<TSpace, T>(string description, KindRead<T> read)
      where TSpace : class, ISheetCells
      => new KindedCellProjection<TSpace, T>(description, read, Placement.Of(ProjectionBuilders<TSpace>.Extent(1, 1)), blankIsNull: false);

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
    /// <typeparam name="TSpace">The space the leaf is declared over; anything carrying formulas.</typeparam>
    public static IProjection<TSpace, string?> Formula<TSpace>()
      where TSpace : class, IFormulaSpace
      => ProjectionBuilders<TSpace>.Range(1, 1, cell => FormulaAt(cell.Space)).Named("Formula");

    /// <summary>
    /// The formula behind the region's first cell, or null. Asked at the point's own coordinates
    /// rather than at (0, 0): a space answers about its own cells, and a region may name a
    /// rectangle part-way into one. The two formula landmarks below ask the same way, so there is
    /// one rule here rather than a rule and an assumption.
    /// </summary>
    private static string? FormulaAt<TSpace>(Plane<TSpace> region)
      where TSpace : class, IFormulaSpace
    {
      var cell = region[0, 0];

      return cell.Space.FormulaAt(cell.Column, cell.Row);
    }

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

      protected FormulaLandmark(string axis, string? containing)
      {
        _containing = containing;
        Description = containing is null
          ? $"no {axis.ToLowerInvariant()} with a formula"
          : $"no {axis.ToLowerInvariant()} with a formula mentioning '{containing}'";
      }

      /// <summary>The negative noun a failure renders, in the matcher family's own voice.</summary>
      public string Description { get; }

      /// <summary>
      /// The space as the one this matcher was built for. A cast rather than a test: the matcher's
      /// own type names the capability, and the only pipeline that accepts it is closed over a space
      /// that has it — so the only way here is a declaration that cast the demand away, and a
      /// boundary that could not look must not answer "not there". The failure is an
      /// <see cref="InvalidCastException"/>, which no tolerance boundary absorbs.
      /// </summary>
      protected IFormulaSpace Formulas(ISpace space) => (IFormulaSpace)space;

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

      public int? FindRow(Plane<ISpace> space)
      {
        var formulas = Formulas(space.Space);

        // Through the region's own points, because a capability answers in the SPACE's coordinates
        // and a region may name a rectangle part-way into it. The point carries the translation the
        // region would otherwise have to do by hand.
        for (var row = 0; row < space.Area.Height; row++)
          for (var column = 0; column < space.Width; column++)
          {
            var cell = space[column, row];

            if (Matches(formulas.FormulaAt(cell.Column, cell.Row)))
              return row;
          }

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

      public int? FindColumn(Plane<ISpace> space)
      {
        var formulas = Formulas(space.Space);

        for (var column = 0; column < space.Width; column++)
          for (var row = 0; row < space.Area.Height; row++)
          {
            var cell = space[column, row];

            if (Matches(formulas.FormulaAt(cell.Column, cell.Row)))
              return column;
          }

        return null;
      }
    }
  }
}
