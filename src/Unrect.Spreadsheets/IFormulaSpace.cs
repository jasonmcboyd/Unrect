using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// EXPERIMENT (typed-spaces): a space whose cells may carry formulas as well as values.
  /// <para>
  /// The capability lives here, in the package that owns the vocabulary, and nothing in
  /// <c>Unrect.Core</c> or <c>Unrect</c> names it — the law of §5. Strategies still decide extents
  /// from content; a formula-aware matcher is a declaration like any other, shipped beside the
  /// backend that can answer it.
  /// </para>
  /// <para>
  /// <b>The slicing law.</b> An implementation's subspaces must be capable too, with translated
  /// coordinates: a slice may never invent capability its parent lacked nor shed what its parent
  /// had.
  /// </para>
  /// </summary>
  public interface IFormulaSpace : ISpace
  {
    /// <summary>
    /// The formula behind the cell at <paramref name="column"/>, <paramref name="row"/>, in this
    /// space's own coordinates, or null where the cell is a plain value.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    /// <param name="row">The 0-based row.</param>
    string? FormulaAt(int column, int row);
  }

  /// <summary>
  /// EXPERIMENT (typed-spaces): the formula capability's declaration vocabulary — one leaf, one
  /// matcher, one reach-through. What a backend package would ship if the experiment were adopted.
  /// </summary>
  public static class FormulaProjections
  {
    /// <summary>
    /// The formula capability's ascription witness: <c>rows.Demanding(Formulas)</c> states that a
    /// declaration needs formulas where nothing in its types could say so.
    /// </summary>
    public static Demand<IFormulaSpace> Formulas => Demand<IFormulaSpace>.Instance;

    /// <summary>
    /// One cell, read as the formula behind it (null where the cell is a plain value). The checked
    /// spelling: a declaration containing this one cannot be applied to a space without formulas,
    /// and the demand travels up through whatever composes it.
    /// </summary>
    public static IProjection<IFormulaSpace, string?> Formula()
      => Projection.Range(1, 1, block => block.Space.Capability<IFormulaSpace>()?.FormulaAt(0, 0))
        .Named("Formula")
        .Demanding<IFormulaSpace, string?>();

    /// <summary>
    /// The first row holding a formula anywhere in it. A boundary, so absence of the
    /// <em>capability</em> is a fault and never a no-match — and in the typed design that fault is
    /// unreachable, because the only way to reach this matcher is through a lift that has already
    /// demanded the capability.
    /// </summary>
    public static IRowLandmark<IFormulaSpace> RowWithFormula()
      => new FormulaRowLandmark();

    /// <summary>
    /// The formula behind one cell of <paramref name="row"/>, or null where there is none — the
    /// <em>unchecked</em> spelling, and the contrast the experiment is about. Nothing in a
    /// projection lambda's type says the declaration around it needs formulas, so this compiles
    /// against any table and answers null over a space that cannot carry them.
    /// </summary>
    /// <param name="row">The table row.</param>
    /// <param name="column">The 0-based column within the row.</param>
    public static string? FormulaAt(this TableRow row, int column)
      => (row ?? throw new ArgumentNullException(nameof(row)))
        .Space.Capability<IFormulaSpace>()?.FormulaAt(column, 0);

    private sealed class FormulaRowLandmark : IRowLandmark<IFormulaSpace>, IRowLandmark
    {
      public string Description => "no row holding a formula";

      IRowLandmark IRowLandmark<IFormulaSpace>.Landmark => this;

      public int? FindRow(ISpace space)
      {
        // A boundary that cannot look must fault: "I could not look" and "I looked and it is not
        // there" must never share a spelling. Unreachable under the typed layer, and kept anyway —
        // it is the runtime-fault design's whole behaviour, and this is the line that prices it.
        var formulas = space.Capability<IFormulaSpace>()
          ?? throw new InvalidOperationException(
            "RowWithFormula was applied to a space that cannot carry formulas; a boundary that cannot look must fault.");

        for (var row = 0; row < space.Area.Height; row++)
          for (var column = 0; column < space.Area.Width; column++)
            if (formulas.FormulaAt(column, row) is not null)
              return row;

        return null;
      }
    }
  }
}
