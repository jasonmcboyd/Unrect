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
  /// </summary>
  public sealed class TableRow
  {
    internal TableRow(int index, CellStrip cells, ProjectionContext context)
    {
      Strip = cells;
      Context = context;
      Index = index;
    }

    /// <summary>This row's 0-based position among the table's body rows.</summary>
    public int Index { get; }

    /// <summary>How many columns wide the row is — the width of the band it was read from.</summary>
    public int Count => Strip.Count;

    /// <summary>The row's cells, by column index.</summary>
    public IReadOnlyList<CellValue> Cells => Strip;

    /// <summary>
    /// The cell in <paramref name="column"/>; an index outside the table is a declaration error.
    /// </summary>
    public CellValue this[int column]
      => column >= 0 && column < Count
        ? Strip[column]
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    /// <summary>
    /// The cell in the column named <paramref name="columnName"/>, resolved by the content rule —
    /// trimmed and case-insensitively, the same rule matchers and <c>Caption</c> use, not the
    /// whitespace-stripping <c>CaptionComparer</c> that binds <c>Table&lt;T&gt;</c>. An
    /// unknown, ambiguous, or headerless lookup is a declaration error.
    /// </summary>
    public CellValue this[string columnName] => Strip[Resolve(columnName)];

    /// <summary>The address of the row's first cell.</summary>
    public ProjectionLocation Location => Strip.Location;

    /// <summary>
    /// The row's own extent, one row tall and as wide as the table — the mirror of
    /// <see cref="CellStrip.Space"/> and <see cref="CellBlock.Space"/>, and the reach-through a
    /// projection asks a capability through:
    /// <c>row.Space.Capability&lt;IFormulaSpace&gt;()?.FormulaAt(column, 0)</c>.
    /// </summary>
    public ICellValues Space => Strip.Space;

    /// <summary>
    /// The address of one cell of the row, for citing it in a message — a data-quality complaint
    /// can then read like a framework one.
    /// </summary>
    public ProjectionLocation AddressOf(int column)
      => column >= 0 && column < Count
        ? Strip.AddressOf(column)
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    /// <summary>
    /// The address of one cell of the row by column name, resolved exactly as the indexer resolves
    /// it — unknown, ambiguous, and headerless lookups fail the same way.
    /// </summary>
    public ProjectionLocation AddressOf(string columnName) => Strip.AddressOf(Resolve(columnName));

    // The typed reads — the compute-legal binder's accessors. A cell is named two ways, coexisting
    // in one row projection: by CAPTION (resolved through the header, robust to a reordered export)
    // and by INDEX (hard-coded, for the headerless or structurally-fixed column a caption cannot
    // name). Each reads the cell through the one canonical accessor for its kind, the same one the
    // matching leaf and the Table<T> binder use, so a Decimal() column and row.Decimal("Amount")
    // describe a bad cell identically. The caption form names the column exactly as the binder does
    // (`column 'Amount': …`); the index form has no caption to name, so it speaks the bare leaf
    // sentence, the A1 alone pinning the column. An unknown caption, an ambiguous one, a name
    // against a headerless table, or an out-of-range index fails as the indexers already do; a kind
    // mismatch or an unrepresentable number throws the shared reading diagnostic, carrying the path.

    /// <summary>The <c>Text</c> in the column named <paramref name="caption"/>; a non-text cell throws the reading diagnostic.</summary>
    public string Text(string caption) => Read(Resolve(caption), CellKind.Text, CellReading.AsString, caption);

    /// <summary>The <c>Decimal</c> in <paramref name="caption"/>; a non-number, or a number no decimal holds, throws.</summary>
    public decimal Decimal(string caption) => Read(Resolve(caption), CellKind.Number, CellReading.AsDecimal, caption);

    /// <summary>The whole-number <c>Integer</c> in <paramref name="caption"/>; a non-number, or a number that is not a whole 32-bit one, throws.</summary>
    public int Integer(string caption) => Read(Resolve(caption), CellKind.Number, CellReading.AsInteger, caption);

    /// <summary>The <c>Double</c> in <paramref name="caption"/>; a non-number cell throws.</summary>
    public double Double(string caption) => Read(Resolve(caption), CellKind.Number, CellReading.AsDouble, caption);

    /// <summary>The <c>Date</c> in <paramref name="caption"/>; a non-temporal cell throws.</summary>
    public DateTime Date(string caption) => Read(Resolve(caption), CellKind.Temporal, CellReading.AsDateTime, caption);

    /// <summary>The <c>Boolean</c> in <paramref name="caption"/>; a non-boolean cell throws.</summary>
    public bool Boolean(string caption) => Read(Resolve(caption), CellKind.Boolean, CellReading.AsBoolean, caption);

    /// <summary>The <c>Text</c> in the column at <paramref name="column"/>; a non-text cell throws.</summary>
    public string Text(int column) => Read(Checked(column), CellKind.Text, CellReading.AsString, null);

    /// <summary>The <c>Decimal</c> in the column at <paramref name="column"/>; a non-number, or a number no decimal holds, throws.</summary>
    public decimal Decimal(int column) => Read(Checked(column), CellKind.Number, CellReading.AsDecimal, null);

    /// <summary>The whole-number <c>Integer</c> in the column at <paramref name="column"/>; a non-number, or a number that is not a whole 32-bit one, throws.</summary>
    public int Integer(int column) => Read(Checked(column), CellKind.Number, CellReading.AsInteger, null);

    /// <summary>The <c>Double</c> in the column at <paramref name="column"/>; a non-number cell throws.</summary>
    public double Double(int column) => Read(Checked(column), CellKind.Number, CellReading.AsDouble, null);

    /// <summary>The <c>Date</c> in the column at <paramref name="column"/>; a non-temporal cell throws.</summary>
    public DateTime Date(int column) => Read(Checked(column), CellKind.Temporal, CellReading.AsDateTime, null);

    /// <summary>The <c>Boolean</c> in the column at <paramref name="column"/>; a non-boolean cell throws.</summary>
    public bool Boolean(int column) => Read(Checked(column), CellKind.Boolean, CellReading.AsBoolean, null);

    // The blank-tolerant twins — the row's spelling of the leaves' OrBlank. A blank cell reads as
    // null with no complaint; a cell of the wrong kind still throws, because a blank says something
    // about the data and a wrong kind says something about the format. Separate methods rather than
    // an OrBlank() chained on, because a read returns a value, not a projection left to modify.

    /// <summary>The <c>Text</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public string? TextOrBlank(string caption) => ReadTextOrBlank(Resolve(caption), caption);

    /// <summary>The <c>Decimal</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public decimal? DecimalOrBlank(string caption) => ReadOrBlank(Resolve(caption), CellKind.Number, CellReading.AsDecimal, caption);

    /// <summary>The whole-number <c>Integer</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public int? IntegerOrBlank(string caption) => ReadOrBlank(Resolve(caption), CellKind.Number, CellReading.AsInteger, caption);

    /// <summary>The <c>Double</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public double? DoubleOrBlank(string caption) => ReadOrBlank(Resolve(caption), CellKind.Number, CellReading.AsDouble, caption);

    /// <summary>The <c>Date</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public DateTime? DateOrBlank(string caption) => ReadOrBlank(Resolve(caption), CellKind.Temporal, CellReading.AsDateTime, caption);

    /// <summary>The <c>Boolean</c> in <paramref name="caption"/>, or null when the cell is blank.</summary>
    public bool? BooleanOrBlank(string caption) => ReadOrBlank(Resolve(caption), CellKind.Boolean, CellReading.AsBoolean, caption);

    /// <summary>The <c>Text</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public string? TextOrBlank(int column) => ReadTextOrBlank(Checked(column), null);

    /// <summary>The <c>Decimal</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public decimal? DecimalOrBlank(int column) => ReadOrBlank(Checked(column), CellKind.Number, CellReading.AsDecimal, null);

    /// <summary>The whole-number <c>Integer</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public int? IntegerOrBlank(int column) => ReadOrBlank(Checked(column), CellKind.Number, CellReading.AsInteger, null);

    /// <summary>The <c>Double</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public double? DoubleOrBlank(int column) => ReadOrBlank(Checked(column), CellKind.Number, CellReading.AsDouble, null);

    /// <summary>The <c>Date</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public DateTime? DateOrBlank(int column) => ReadOrBlank(Checked(column), CellKind.Temporal, CellReading.AsDateTime, null);

    /// <summary>The <c>Boolean</c> in the column at <paramref name="column"/>, or null when the cell is blank.</summary>
    public bool? BooleanOrBlank(int column) => ReadOrBlank(Checked(column), CellKind.Boolean, CellReading.AsBoolean, null);

    private CellStrip Strip { get; }
    private ProjectionContext Context { get; }

    /// <summary>
    /// Reads an optional column: false when the table simply has no such column. A lookup that
    /// cannot mean anything — an ambiguous name, or a name against a table declared without a
    /// header row — still throws, because that is a broken declaration rather than a missing value.
    /// </summary>
    public bool TryGet(string columnName, out CellValue value)
    {
      var indices = Resolvable(columnName);

      if (indices.Count == 0)
      {
        value = CellValue.Blank;
        return false;
      }

      value = Strip[indices[0]];
      return true;
    }

    private int Resolve(string columnName)
    {
      var indices = Resolvable(columnName);

      if (indices.Count == 1)
        return indices[0];

      // Unreachable fallback: Resolvable throws the headerless message when there is no scope, so by
      // the time control reaches here a Column scope exists. Kept as an empty list rather than the
      // owning table's columns — byte-identical, and it is what lets a TableRow have no table.
      var labels = Context.NearestLabels(LabelAxis.Column)?.Source.Labels ?? Array.Empty<string>();
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
      var scope = Context.NearestLabels(LabelAxis.Column);

      if (scope is null)
        throw Failure($"column '{columnName}' cannot be resolved: the table was declared without a header row; use column indices.");

      var ordinals = scope.Source.IndicesOf(columnName);

      if (ordinals.Count > 1)
        throw Ambiguous(columnName, ordinals);

      if (ordinals.Count == 0)
        return ordinals;

      var local = ordinals[0] + scope.CaptureOrigin.Width - Context.Origin.Width;

      if (local < 0 || local >= Count)
        throw Failure($"column '{columnName}' is not in this region");

      return new[] { local };
    }

    private ProjectionException Ambiguous(string columnName, IReadOnlyList<int> indices)
      => Failure($"column '{columnName}' appears at indices {Join(indices)}; use the index.");

    private ProjectionException Failure(string problem) => Context.Failure(problem, Strip.Space);

    /// <summary>A column index the caller supplied by number, checked as the indexer checks it.</summary>
    private int Checked(int column)
      => column >= 0 && column < Count
        ? column
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    private T Read<T>(int column, CellKind kind, CellReader<T> read, string? caption)
      => Convert(Strip[column], column, kind, read, caption);

    private T? ReadOrBlank<T>(int column, CellKind kind, CellReader<T> read, string? caption) where T : struct
    {
      var cell = Strip[column];
      return cell.IsBlank ? (T?)null : Convert(cell, column, kind, read, caption);
    }

    private string? ReadTextOrBlank(int column, string? caption)
    {
      var cell = Strip[column];
      return cell.IsBlank ? null : Convert(cell, column, CellKind.Text, CellReading.AsString, caption);
    }

    /// <summary>
    /// The kind assertion and the conversion, both spoken by <see cref="CellReading"/>. The caption
    /// form names the column exactly as the <c>Table&lt;T&gt;</c> binder does; the index form has no
    /// caption and speaks the bare leaf sentence, the A1 alone pinning the column. The address is a
    /// thunk because it is only ever built on the failing path.
    /// </summary>
    private T Convert<T>(CellValue cell, int column, CellKind kind, CellReader<T> read, string? caption)
    {
      string At() => Strip.AddressOf(column).A1;
      var subject = caption is null ? string.Empty : $"column '{caption}': ";

      if (cell.Kind != kind)
        throw Failure(subject + CellReading.WrongKind(kind, cell, At()));

      if (!read(cell, At, out var value, out var conversion))
        throw Failure(subject + conversion);

      return value;
    }

    private static string Join(IReadOnlyList<int> indices)
      => indices.Count == 1
        ? indices[0].ToString()
        : string.Join(", ", indices.Take(indices.Count - 1)) + " and " + indices[indices.Count - 1];
  }
}
