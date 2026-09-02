using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A table's extent split into an optional header row and the body rows beneath it. Cells are
  /// reachable by index and, when a header row was declared, by column name.
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

    public ISpace Space { get; }

    public int ColumnCount => Space.Area.Size.Width;
    public int RowCount => Space.Area.Size.Height - HeaderRows;
    public bool HasHeader => HeaderRows > 0;

    public CellStrip Header { get; }
    public IReadOnlyList<string> ColumnNames { get; }

    /// <summary>The address of the table's top-left cell, header included.</summary>
    public ShapeLocation Location => ShapeLocation.At(Context.Origin, Space.Area.Size);

    public IReadOnlyList<TableRow> Rows => _rows ??= BuildRows();

    private int HeaderRows { get; }

    /// <summary>
    /// The context the table was projected in — how a projection built on this view reports a
    /// failure against the table itself.
    /// </summary>
    internal ShapeContext Context { get; }

    /// <summary>Reports a problem against the table itself — its origin, its extent.</summary>
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
        : System.Array.Empty<int>();
    }

    private IReadOnlyList<TableRow> BuildRows()
    {
      var rows = new TableRow[RowCount];

      for (var index = 0; index < rows.Length; index++)
      {
        var offset = new Offset(0, HeaderRows + index);
        var rowContext = Context.Advance(offset);
        var strip = new CellStrip(Space.GetSubspace(offset, new Area(ColumnCount, 1)), Orientation.Horizontal, rowContext.Origin);

        rows[index] = new TableRow(this, index, strip, rowContext);
      }

      return rows;
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

    public int Index { get; }
    public int Count => Strip.Count;
    public IReadOnlyList<CellValue> Cells => Strip;

    /// <summary>
    /// The cell in <paramref name="column"/>; an index outside the table is a declaration error.
    /// </summary>
    public CellValue this[int column]
      => column >= 0 && column < Count
        ? Strip[column]
        : throw Failure($"column index {column} is out of range; the table has {Count} columns.");

    /// <summary>
    /// The cell in the column named <paramref name="columnName"/>, matched trimmed and
    /// case-insensitively. An unknown, ambiguous, or headerless lookup is a declaration error.
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
