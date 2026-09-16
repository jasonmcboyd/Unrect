using System;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// One cell read as a declared kind — what the six kinded leaves are made of. The kind is
  /// declaration data rather than a lambda body, which is what lets a failure name the cell and what
  /// a writer would need to emit it.
  /// </summary>
  /// <typeparam name="T">What the reading produces.</typeparam>
  /// <param name="space">The sheet the cell belongs to.</param>
  /// <param name="column">The cell's column, in the sheet's own coordinates.</param>
  /// <param name="row">The cell's row, in the sheet's own coordinates.</param>
  /// <param name="value">What the cell holds, when the read succeeded.</param>
  /// <param name="problem">Why it did not, when it did not.</param>
  /// <returns>Whether the cell could be read.</returns>
  internal delegate bool KindRead<T>(ISheetCells space, int column, int row, out T value, out CellProblem? problem);

  /// <summary>
  /// A cell leaf that asserts a kind: <c>Decimal()</c> and its five siblings. A real primitive
  /// rather than a <c>Select</c> over something else, so a failure names the cell and the path
  /// exactly as a core leaf's does.
  /// </summary>
  /// <typeparam name="TSpace">The sheet the leaf is declared over.</typeparam>
  /// <typeparam name="TResult">What the leaf hands back.</typeparam>
  internal sealed class KindedCellProjection<TSpace, TResult> : ProjectionBase<TSpace, TResult>
    where TSpace : class, ISheetCells
  {
    internal KindedCellProjection(string description, KindRead<TResult> read, Placement placement, bool blankIsNull)
      : base(placement)
    {
      Description = description;
      Read = read;
      BlankIsNull = blankIsNull;
    }

    private KindRead<TResult> Read { get; }

    /// <summary>
    /// Whether a blank cell reads as null instead of failing — what <c>OrBlank</c> declares. It
    /// tolerates a blank and nothing else: a cell of the wrong kind still fails exactly as loudly,
    /// because a missing value says something about the data and a wrong kind says something about
    /// the format.
    /// </summary>
    private bool BlankIsNull { get; }

    public override string Description { get; }

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"a {Description} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var cell = extent[0, 0];

      // Quietly: the declaration said this cell may be absent, so its absence is the answer rather
      // than something to report. That is the whole difference from Optional, which absorbs a
      // failure and says so with a Warning.
      if (BlankIsNull && cell.IsBlank)
        return new ProjectionResult<TResult>(default!, size);

      if (!Read(cell.Space, cell.Column, cell.Row, out var value, out var problem))
        throw context.Failure(problem!(context.Locate(extent).A1), extent);

      return new ProjectionResult<TResult>(value, size);
    }

    internal override IProjection<TSpace, TValue> Tolerating<TValue>(Func<TResult, TValue> widen)
    {
      bool Tolerant(ISheetCells space, int column, int row, out TValue value, out CellProblem? problem)
      {
        if (!Read(space, column, row, out var raw, out problem))
        {
          value = default!;
          return false;
        }

        value = widen(raw);
        return true;
      }

      return Naming(new KindedCellProjection<TSpace, TValue>(Description + "?", Tolerant, Placement, blankIsNull: true));
    }
  }
}
