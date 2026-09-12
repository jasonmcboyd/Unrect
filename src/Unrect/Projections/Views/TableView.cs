using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// A table's extent split into an optional header row and the body rows beneath it. Cells are
  /// reachable by index and, when a header row was declared, by column name.
  /// <para>
  /// Over an extent whose height is discovered while it is read, a table costs its header row up
  /// front and then whatever the projection asks for: <see cref="StreamRows"/> reads one row per
  /// step and never asks how many there are, while <see cref="Rows"/>, <see cref="RowCount"/> and
  /// <see cref="Location"/> are dimension queries and settle the bound. <see cref="ColumnCount"/>
  /// and the header itself are free — a width is settled before any row is read. That is why the
  /// three built-in row projections are written against <see cref="StreamRows"/>.
  /// </para>
  /// </summary>
  public sealed class TableView : ILabelSource, IHeaderCitations
  {
    // Views are built per projection and are not covered by the projection thread-safety guarantee;
    // the caches race benignly (reference assignment is atomic, so the worst case is duplicated
    // work).
    private Dictionary<string, List<int>>? _columnsByName;
    private IReadOnlyList<TableRow>? _rows;

    internal TableView(ISpace space, int headerRows, ProjectionContext context)
    {
      Space = space;
      HeaderRows = headerRows;

      Header = new CellStrip(
        space.GetSubspace(new Offset(0, 0), new Area(HasHeader ? ColumnCount : 0, headerRows)),
        Orientation.Horizontal,
        context);

      ColumnNames = Header.Select(cell => cell.TryGetString()?.Trim() ?? string.Empty).ToList();

      // Publish the columns as the ambient Column labels for the body's subtree, but only when a
      // header was actually declared: a headerless table pushes nothing, so a by-name lookup still
      // finds no scope and reports the headerless message. The origin PushLabels captures is this
      // table's own, the frame the header's ordinals are read in and every body row translates from.
      Context = HasHeader ? context.PushLabels(LabelAxis.Column, this) : context;
    }

    /// <summary>The table's full extent, header row(s) included.</summary>
    public ISpace Space { get; }

    /// <summary>
    /// How many columns wide the table is. Free on an extent still being discovered: a width is
    /// settled before the first row is read.
    /// </summary>
    public int ColumnCount => BoundedSpace.WidthOf(Space);

    /// <summary>
    /// How many body rows the table has, header row(s) excluded. A dimension query, so on an extent
    /// still being discovered this reads the sheet through to wherever the declaration's rule
    /// stops; <see cref="StreamRows"/> is the reading that does not need the answer.
    /// </summary>
    public int RowCount => Space.Area.Height - HeaderRows;

    /// <summary>Whether a header row was declared. By-name lookups (<see cref="TableRow.this[string]"/>) need one.</summary>
    public bool HasHeader => HeaderRows > 0;

    /// <summary>The header row(s), when <see cref="HasHeader"/>; a zero-width strip when the table has none.</summary>
    public CellStrip Header { get; }

    /// <summary>Each column's header text, trimmed; the empty string for a column with no caption.</summary>
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>The columns as an ambient label source: its names and the content-rule lookup already here.</summary>
    IReadOnlyList<string> ILabelSource.Labels => ColumnNames;

    IReadOnlyList<int> ILabelSource.IndicesOf(string label) => IndicesOf(label);

    /// <summary>The header cells the bind rung cites: the table's own failure and header addresses.</summary>
    ProjectionException IHeaderCitations.Failure(string problem) => Failure(problem);

    ProjectionLocation IHeaderCitations.AddressOf(int column) => Header.AddressOf(column);

    /// <summary>
    /// The address of the table's top-left cell, header included. It carries the extent the table
    /// was found in, so on one still being discovered this settles the bound.
    /// </summary>
    public ProjectionLocation Location => ProjectionLocation.At(Context.Origin, Space.Area.Size);

    /// <summary>
    /// The table's body rows, header row(s) excluded, built once per view. Materialising them is a
    /// dimension query, so on an extent still being discovered this settles the bound — use
    /// <see cref="StreamRows"/> to read a tall table a row at a time.
    /// </summary>
    public IReadOnlyList<TableRow> Rows => _rows ??= BuildRows();

    /// <summary>
    /// The table's body rows, header row(s) excluded, read one at a time as the enumeration
    /// advances: each step asks whether there is a row there and stops when there is not, so an
    /// extent whose height is still being discovered is consumed forward-only, in step with the
    /// reading, and is never measured up front.
    /// <para>
    /// This is what the built-in row readings — <c>Table&lt;T&gt;()</c>, <c>Table()</c>
    /// and <c>Table(row =&gt; …)</c> — are written against, and what a projection of your own
    /// should use where the sheet is tall. The rows it hands back are the same <see
    /// cref="TableRow"/> views <see cref="Rows"/> holds; unlike <see cref="Rows"/> they are not
    /// cached, so enumerating twice builds them twice — a second enumeration costs no extra rows of
    /// the sheet, the bound having been settled by the first.
    /// </para>
    /// </summary>
    public IEnumerable<TableRow> StreamRows()
    {
      var index = 0;

      foreach (var band in StreamBands(1))
        yield return new TableRow(this, index++, new CellStrip(band.Space, Orientation.Horizontal, band.Context), band.Context);
    }

    /// <summary>
    /// The body as bands of <paramref name="bandHeight"/> rows, each with the context to project it
    /// in — what a row projection is applied to, and what <see cref="StreamRows"/> wraps in a
    /// <see cref="TableRow"/>. Forward-only in the same way: each step asks whether the band's last
    /// row is there and stops when it is not, so an extent still being discovered is consumed in
    /// step with the reading.
    /// <para>
    /// The height is a parameter because a record is not always one row tall. Everything above it
    /// counts in bands rather than rows, so a table that ever slices taller records needs a
    /// different argument here and nothing else; a trailing part-band is not a record and is left
    /// undescribed.
    /// </para>
    /// </summary>
    internal IEnumerable<(ISpace Space, ProjectionContext Context)> StreamBands(int bandHeight)
    {
      for (var row = HeaderRows; BoundedSpace.HasRow(Space, row + bandHeight - 1); row += bandHeight)
      {
        var offset = new Offset(0, row);

        yield return (Space.GetSubspace(offset, new Area(ColumnCount, bandHeight)), Context.Advance(offset));
      }
    }

    /// <summary>
    /// The body rows that become records under the four preset blank-row policies
    /// (Stop/Skip/Fault/Tolerate). Under <see cref="BlankRowStrategy.Stop"/> the extent already
    /// excludes blank rows, so this delegates verbatim to <see cref="StreamRows"/> — no blank test,
    /// no filter, byte-identical to the default path. The other policies walk to the enclosing edge
    /// and act on each fully-blank row: Fault throws terminally, Tolerate records an Info and omits
    /// the record, Skip simply omits it. Project is not here — it injects records and is handled by
    /// the rung through <see cref="StreamClassifiedRows"/>.
    /// </summary>
    internal IEnumerable<TableRow> StreamBodyRows(BlankRowStrategy onBlank)
    {
      if (onBlank.IsStop)
      {
        foreach (var row in StreamRows())
          yield return row;

        yield break;
      }

      foreach (var row in StreamRows())
      {
        if (!IsBlankRow(row))
        {
          yield return row;
          continue;
        }

        if (onBlank.IsFault)
          throw Fault($"the row at {row.Location.A1} is blank, which is not allowed here");

        if (onBlank.Diagnostic is DiagnosticSeverity severity)
          Context.Report(severity, Failure($"the row at {row.Location.A1} is blank; it was skipped"));

        // Skip and Tolerate both omit the record and keep reading.
      }
    }

    /// <summary>
    /// Every body row to the enclosing edge, each tagged blank or not — the source the Project
    /// (<c>blankRecord</c>) rung maps: a blank row yields its blank record, a non-blank row its
    /// normal one.
    /// </summary>
    internal IEnumerable<(TableRow Row, bool IsBlank)> StreamClassifiedRows()
    {
      foreach (var row in StreamRows())
        yield return (row, IsBlankRow(row));
    }

    /// <summary>A fully-blank row: every cell <see cref="CellValue.IsBlank"/> — the complement of "any value".</summary>
    private static bool IsBlankRow(TableRow row)
    {
      for (var column = 0; column < row.Count; column++)
        if (!row.Cells[column].IsBlank)
          return false;

      return true;
    }

    private int HeaderRows { get; }

    /// <summary>
    /// The context the table was projected in — how a projection built on this view reports a
    /// failure against the table itself.
    /// </summary>
    internal ProjectionContext Context { get; }

    /// <summary>
    /// Reports a problem against the table itself — its origin, its extent. Citing the extent
    /// settles a bound still being discovered, which costs nothing worth saving on the way to a
    /// failure.
    /// </summary>
    internal ProjectionException Failure(string problem) => Context.Failure(problem, Space);

    /// <summary>
    /// The same, for something that broke rather than disagreed — a declaration that cannot mean
    /// anything, which no tolerance boundary may report as a section that was not there.
    /// </summary>
    internal ProjectionException Fault(string problem) => Context.Failure(problem, Space, null, isFault: true);

    /// <summary>
    /// The columns carrying <paramref name="columnName"/>; empty when there is no such column.
    /// Header names are matched by the content rule, applied to the key as well as to the header.
    /// </summary>
    internal IReadOnlyList<int> IndicesOf(string columnName)
    {
      if (columnName is null)
        throw new ArgumentNullException(nameof(columnName));

      return (_columnsByName ??= BuildColumnsByName()).TryGetValue(columnName, out var indices)
        ? indices
        : Array.Empty<int>();
    }

    /// <summary>
    /// Every body row in one list, sized exactly. A caller of <see cref="Rows"/> is already paying
    /// the dimension query <see cref="RowCount"/> is, so asking it first costs nothing and the list
    /// is allocated once at the right size instead of doubling its way there. The row rungs
    /// deliberately do the opposite and grow their lists, because for them asking how many
    /// rows there are is the forcing question streaming exists to avoid.
    /// </summary>
    private List<TableRow> BuildRows()
    {
      var rows = new List<TableRow>(RowCount);

      foreach (var row in StreamRows())
        rows.Add(row);

      return rows;
    }

    /// <summary>
    /// Header text to the columns carrying it, keyed by the content rule itself rather than by a
    /// comparer that happens to agree with it — a lookup here and a <c>RowContaining</c> elsewhere
    /// find a caption on the same terms, and go on doing so if those terms change.
    /// </summary>
    private Dictionary<string, List<int>> BuildColumnsByName()
    {
      var columns = new Dictionary<string, List<int>>(CellMatching.TextComparer);

      for (var index = 0; index < ColumnNames.Count; index++)
      {
        var name = ColumnNames[index];

        if (name.Length == 0)
          continue;

        if (!columns.TryGetValue(name, out var indices))
          columns[name] = indices = new List<int>();

        indices.Add(index);
      }

      return columns;
    }
  }

  /// <summary>
  /// One body row of a <see cref="TableView"/>.
  /// </summary>
  public sealed class TableRow
  {
    internal TableRow(TableView? table, int index, CellStrip cells, ProjectionContext context)
    {
      Table = table;
      Strip = cells;
      Context = context;
      Index = index;
    }

    /// <summary>This row's 0-based position among the table's body rows.</summary>
    public int Index { get; }

    /// <summary>How many columns wide the row is — the same as the table's <see cref="TableView.ColumnCount"/>.</summary>
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
    public ISpace Space => Strip.Space;

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

    // Nullable, and read by nothing: a Record projection builds a TableRow with no owning view, and
    // caption resolution flows entirely through the ambient label scope rather than through a table.
    private TableView? Table { get; }
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
