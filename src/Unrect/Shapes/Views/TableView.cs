using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A table's extent split into an optional header row and the body rows beneath it. Cells are
  /// reachable by index and, when a header row was declared, by column name.
  /// <para>
  /// Over an extent whose height is discovered while it is read, a table costs its header row up
  /// front and then whatever the projection asks for: <see cref="StreamRows"/> reads one row per step
  /// and never asks how many there are, while <see cref="Rows"/>, <see cref="RowCount"/> and
  /// <see cref="Location"/> are dimension queries and settle the bound. <see cref="ColumnCount"/> and
  /// the header itself are free — a width is settled before any row is read. That is why the three
  /// built-in row projections are written against <see cref="StreamRows"/>.
  /// </para>
  /// </summary>
  public sealed class TableView
  {
    // Views are built per projection and are not covered by the shape thread-safety guarantee; the
    // caches race benignly (reference assignment is atomic, so the worst case is duplicated work).
    private Dictionary<string, List<int>>? _columnsByName;
    private IReadOnlyList<TableRow>? _rows;

    internal TableView(ISpace space, int headerRows, ShapeContext context)
    {
      Space = space;
      HeaderRows = headerRows;
      Context = context;

      Header = new CellStrip(
        space.GetSubspace(new Offset(0, 0), new Area(HasHeader ? ColumnCount : 0, headerRows)),
        Orientation.Horizontal,
        context.Origin);

      ColumnNames = Header.Select(cell => cell.TryGetString()?.Trim() ?? string.Empty).ToList();
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
    /// still being discovered this reads the sheet through to wherever the declaration's rule stops;
    /// <see cref="StreamRows"/> is the reading that does not need the answer.
    /// </summary>
    public int RowCount => Space.Area.Height - HeaderRows;

    /// <summary>Whether a header row was declared. By-name lookups (<see cref="TableRow.this[string]"/>) need one.</summary>
    public bool HasHeader => HeaderRows > 0;

    /// <summary>The header row(s), when <see cref="HasHeader"/>; a zero-width strip when the table has none.</summary>
    public CellStrip Header { get; }

    /// <summary>Each column's header text, trimmed; the empty string for a column with no caption.</summary>
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    /// The address of the table's top-left cell, header included. It carries the extent the table was
    /// found in, so on one still being discovered this settles the bound.
    /// </summary>
    public ShapeLocation Location => ShapeLocation.At(Context.Origin, Space.Area.Size);

    /// <summary>
    /// The table's body rows, header row(s) excluded, built once per view. Materialising them is a
    /// dimension query, so on an extent still being discovered this settles the bound — use
    /// <see cref="StreamRows"/> to read a tall table a row at a time.
    /// </summary>
    public IReadOnlyList<TableRow> Rows => _rows ??= BuildRows();

    /// <summary>
    /// The table's body rows, header row(s) excluded, read one at a time as the enumeration advances:
    /// each step asks whether there is a row there and stops when there is not, so an extent whose
    /// height is still being discovered is consumed forward-only, in step with the reading, and is
    /// never measured up front.
    /// <para>
    /// This is what the built-in row projections — <c>TableRows&lt;T&gt;()</c>, <c>TableRows()</c> and
    /// <c>TableRows(row =&gt; …)</c> — are written against, and what a projection of your own should
    /// use where the sheet is tall. The rows it hands back are the same <see cref="TableRow"/> views
    /// <see cref="Rows"/> holds; unlike <see cref="Rows"/> they are not cached, so enumerating twice
    /// builds them twice — a second enumeration costs no extra rows of the sheet, the bound having
    /// been settled by the first.
    /// </para>
    /// </summary>
    public IEnumerable<TableRow> StreamRows()
    {
      for (var index = 0; BoundedSpace.HasRow(Space, HeaderRows + index); index++)
        yield return RowAt(index);
    }

    private int HeaderRows { get; }

    /// <summary>
    /// The context the table was projected in — how a projection built on this view reports a
    /// failure against the table itself.
    /// </summary>
    internal ShapeContext Context { get; }

    /// <summary>
    /// Reports a problem against the table itself — its origin, its extent. Citing the extent settles
    /// a bound still being discovered, which costs nothing worth saving on the way to a failure.
    /// </summary>
    internal ShapeException Failure(string problem) => Context.Failure(problem, Space);

    /// <summary>
    /// The columns carrying <paramref name="columnName"/>; empty when there is no such column.
    /// Header names are matched trimmed and case-insensitively, so the key is trimmed too.
    /// </summary>
    internal IReadOnlyList<int> IndicesOf(string columnName)
    {
      if (columnName is null)
        throw new ArgumentNullException(nameof(columnName));

      return (_columnsByName ??= BuildColumnsByName()).TryGetValue(columnName.Trim(), out var indices)
        ? indices
        : Array.Empty<int>();
    }

    /// <summary>
    /// Every body row in one list, sized exactly. A caller of <see cref="Rows"/> is already paying
    /// the dimension query <see cref="RowCount"/> is, so asking it first costs nothing and the list
    /// is allocated once at the right size instead of doubling its way there. The <c>TableRows</c>
    /// rungs deliberately do the opposite and grow their lists, because for them asking how many
    /// rows there are is the forcing question streaming exists to avoid.
    /// </summary>
    private List<TableRow> BuildRows()
    {
      var rows = new List<TableRow>(RowCount);

      foreach (var row in StreamRows())
        rows.Add(row);

      return rows;
    }

    private TableRow RowAt(int index)
    {
      var offset = new Offset(0, HeaderRows + index);
      var rowContext = Context.Advance(offset);
      var strip = new CellStrip(Space.GetSubspace(offset, new Area(ColumnCount, 1)), Orientation.Horizontal, rowContext.Origin);

      return new TableRow(this, index, strip, rowContext);
    }

    private Dictionary<string, List<int>> BuildColumnsByName()
    {
      var columns = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

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
    internal TableRow(TableView table, int index, CellStrip cells, ShapeContext context)
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
    /// whitespace-stripping <c>CaptionComparer</c> that binds <c>TableRows&lt;T&gt;</c>. An unknown,
    /// ambiguous, or headerless lookup is a declaration error.
    /// </summary>
    public CellValue this[string columnName] => Strip[Resolve(columnName)];

    /// <summary>The address of the row's first cell.</summary>
    public ShapeLocation Location => Strip.Location;

    /// <summary>
    /// The address of one cell of the row, for citing it in a message — a data-quality complaint
    /// can then read like a framework one.
    /// </summary>
    public ShapeLocation AddressOf(int column)
      => column >= 0 && column < Count
        ? Strip.AddressOf(column)
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    /// <summary>
    /// The address of one cell of the row by column name, resolved exactly as the indexer resolves
    /// it — unknown, ambiguous, and headerless lookups fail the same way.
    /// </summary>
    public ShapeLocation AddressOf(string columnName) => Strip.AddressOf(Resolve(columnName));

    private TableView Table { get; }
    private CellStrip Strip { get; }
    private ShapeContext Context { get; }

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

      var available = Table.ColumnNames.Where(name => name.Length > 0).Select(name => $"'{name}'").ToList();

      throw Failure(
        $"there is no column named '{columnName}'; available columns: {(available.Count == 0 ? "none" : string.Join(", ", available))}.");
    }

    /// <summary>
    /// The columns the name resolves to, having rejected the lookups that cannot mean anything.
    /// </summary>
    private IReadOnlyList<int> Resolvable(string columnName)
    {
      var indices = Table.IndicesOf(columnName);

      if (indices.Count > 1)
        throw Ambiguous(columnName, indices);

      if (indices.Count == 0 && !Table.HasHeader)
        throw Failure($"column '{columnName}' cannot be resolved: the table was declared without a header row; use column indices.");

      return indices;
    }

    private ShapeException Ambiguous(string columnName, IReadOnlyList<int> indices)
      => Failure($"column '{columnName}' appears at indices {Join(indices)}; use the index.");

    private ShapeException Failure(string problem) => Context.Failure(problem, Strip.Space);

    private static string Join(IReadOnlyList<int> indices)
      => indices.Count == 1
        ? indices[0].ToString()
        : string.Join(", ", indices.Take(indices.Count - 1)) + " and " + indices[indices.Count - 1];
  }
}
