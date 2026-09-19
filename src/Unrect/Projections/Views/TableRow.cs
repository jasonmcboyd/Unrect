using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One body row of a table. Captions resolve through the ambient <see cref="LabelAxis.Column"/>
  /// scope a scope-introducer pushed, so a row owns no view and a decoupled <c>Record</c> reads by
  /// the same path a built-in table's row does.
  /// <para>
  /// A cell is a <see cref="Point{TSpace}"/> — <c>row["Amount"]</c> is a place, and what can be read
  /// there is whatever the space carries: the canonical questions everywhere, a backend's kinded
  /// reads where the declaration named a space that has them.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the row's cells belong to.</typeparam>
  public sealed class TableRow<TSpace>
    where TSpace : class, ISpace
  {
    internal TableRow(int index, CellStrip<TSpace> cells, ProjectorScope<TSpace> scope)
    {
      Strip = cells;
      Scope = scope;
      Index = index;
    }

    /// <summary>This row's 0-based position among the table's body rows.</summary>
    public int Index { get; }

    /// <summary>How many columns wide the row is — the width of the band it was read from.</summary>
    public int Count => Strip.Count;

    /// <summary>
    /// The cell in <paramref name="column"/>; an index outside the table is a declaration error.
    /// </summary>
    public Point<TSpace> this[int column] => Strip[Checked(column)];

    /// <summary>
    /// The cell in the column named <paramref name="columnName"/>, resolved by the content rule —
    /// trimmed and case-insensitively, the same rule matchers and <c>Caption</c> use. An unknown,
    /// ambiguous, or headerless lookup is a declaration error.
    /// </summary>
    public Point<TSpace> this[string columnName] => Strip[Resolve(columnName)];

    /// <summary>
    /// The cell a PATH through the header names — <c>row["From", "Id"]</c> — where each step is a
    /// name, the nth of a name <c>("Id", 1)</c>, or a position <c>1</c> within what the path has
    /// reached, all counted from zero. See <see cref="LabelStep"/>.
    /// <para>
    /// A path that names nothing, names several things, stops at a band, or steps outside one fails
    /// here, on the first row and every row, with what the header does hold — whatever the data is.
    /// </para>
    /// </summary>
    public Point<TSpace> this[params LabelStep[] path] => Strip[Resolve(path)];

    /// <summary>The address of the row's first cell.</summary>
    public ProjectionLocation Location => Strip.Location;

    /// <summary>
    /// The row's own extent, one row tall and as wide as the table — the mirror of
    /// <see cref="CellStrip{TSpace}.Space"/> and <see cref="CellBlock{TSpace}.Space"/>.
    /// </summary>
    public Plane<TSpace> Space => Strip.Space;

    /// <summary>
    /// The address of one cell of the row, for citing it in a message — a data-quality complaint
    /// can then read like a framework one.
    /// </summary>
    /// <param name="column">The 0-based column.</param>
    public ProjectionLocation AddressOf(int column) => Strip.AddressOf(Checked(column));

    /// <summary>
    /// The address of one cell of the row by column name, resolved exactly as the indexer resolves
    /// it — unknown, ambiguous, and headerless lookups fail the same way.
    /// </summary>
    /// <param name="columnName">The column's caption.</param>
    public ProjectionLocation AddressOf(string columnName) => Strip.AddressOf(Resolve(columnName));

    /// <summary>
    /// Whether the ambient columns carry one named <paramref name="caption"/> — for the record that
    /// reads a column some exports have and others do not.
    /// <para>
    /// A lookup that cannot mean anything still throws: an ambiguous caption is a table nobody can
    /// read by name, a name against a table declared without a header row is a broken declaration,
    /// and a column that has narrowed out of this row — <c>column 'X' is not in this region</c> — is
    /// a label whose column the row does not reach rather than one the table does not carry. A
    /// caption the ambient columns simply do not name is the only absence this reports.
    /// </para>
    /// </summary>
    /// <param name="caption">The column's caption.</param>
    public bool Has(string caption) => Resolvable(caption).Count > 0;

    private CellStrip<TSpace> Strip { get; }

    /// <summary>
    /// The scope this row was read in — the scope a projection applied to the row descends from,
    /// which is the table's own with its column labels pushed.
    /// </summary>
    internal ProjectorScope<TSpace> Scope { get; }

    private int Resolve(string columnName)
    {
      var indices = Resolvable(columnName);

      if (indices.Count == 1)
        return indices[0];

      // Unreachable fallback: Resolvable throws the headerless message when there is no scope, so by
      // the time control reaches here a Column scope exists. Kept as an empty list rather than the
      // owning table's columns — byte-identical, and it is what lets a TableRow have no table.
      var labels = Scope.NearestLabels(LabelAxis.Column)?.Source.Labels ?? Array.Empty<string>();
      var available = labels.Where(name => name.Length > 0).Select(name => $"'{name}'").ToList();

      throw Failure(
        $"there is no column named '{columnName}'; available columns: {(available.Count == 0 ? "none" : string.Join(", ", available))}.");
    }

    /// <summary>
    /// The columns the name resolves to — a single translated index, or empty when the ambient
    /// columns carry no such label. Resolution runs through the label environment the manufacturing
    /// table pushed: the columns are found in the frame the header was read in, then each ordinal is
    /// translated to this row's frame and bounds-checked. A label whose column has narrowed out of
    /// the row (a descendant reading a slice of the table) is a clean, absorbable failure — never a
    /// silent read of the neighbour cell.
    /// </summary>
    private IReadOnlyList<int> Resolvable(string columnName)
    {
      var scope = Scope.NearestLabels(LabelAxis.Column);

      if (scope is null)
        throw Failure($"column '{columnName}' cannot be resolved: the table was declared without a header row; use column indices.");

      var ordinals = scope.Source.IndicesOf(columnName);

      if (ordinals.Count > 1)
        throw Ambiguous(columnName, ordinals);

      if (ordinals.Count == 0)
        return ordinals;

      // One subtraction, and nothing accumulated: both origins are the root space's own, so the
      // frame the header was read in and the frame this row reads in are directly comparable.
      var local = ordinals[0] + scope.CaptureOrigin.Width - Strip.Space.Origin.Width;

      if (local < 0 || local >= Count)
        throw Failure($"column '{columnName}' is not in this region");

      return new[] { local };
    }

    private ProjectionException Ambiguous(string columnName, IReadOnlyList<int> indices)
    {
      // Under bands the columns have paths, which say which is which better than a number does.
      var paths = Scope.NearestLabels(LabelAxis.Column)?.Source.Paths;
      var banded = paths is not null && indices.Any(index => paths[index].Count > 1);

      return Failure(
        $"column '{columnName}' appears at indices {Join(indices)}; use the index"
        + (banded ? $", or its path: {string.Join(" or ", indices.Select(index => LabelPaths.Written(paths![index])))}." : "."));
    }

    private int Resolve(LabelStep[] path)
    {
      var scope = Scope.NearestLabels(LabelAxis.Column)
        ?? throw Failure("a path cannot be resolved: the table was declared without a header row; use column indices.");

      var answer = LabelPaths.Resolve(scope.Source, path, Unrect.Strategies.CellMatching.TextComparer);

      if (answer.Problem is string problem)
        throw Failure(problem);

      if (answer.IsBand)
        throw Failure($"{string.Join(", ", path.Select(step => step.ToString()))} is a band over {answer.Columns.Count} columns, not a column; say which, by name or by position");

      // The same one subtraction a name makes: from the frame the header was read in to this row's.
      var local = answer.Columns[0] + scope.CaptureOrigin.Width - Strip.Space.Origin.Width;

      return local >= 0 && local < Count
        ? local
        : throw Failure($"the column at {string.Join(", ", path.Select(step => step.ToString()))} is not in this region");
    }

    private ProjectionException Failure(string problem) => Strip.Failure(problem);

    /// <summary>A column index the caller supplied by number, checked as the strip checks it.</summary>
    private int Checked(int column)
      => column >= 0 && column < Count
        ? column
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    private static string Join(IReadOnlyList<int> indices)
      => indices.Count == 1
        ? indices[0].ToString()
        : string.Join(", ", indices.Take(indices.Count - 1)) + " and " + indices[indices.Count - 1];
  }
}
