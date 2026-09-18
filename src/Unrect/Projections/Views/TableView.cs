using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A table's extent split into an optional header row and the body rows beneath it. Cells are
  /// reachable by index and, when a header row was declared, by column name.
  /// </summary>
  /// <typeparam name="TSpace">The space the table's cells belong to.</typeparam>
  public sealed class TableView<TSpace>
    where TSpace : class, ISpace
  {
    // Views are built per projection and are not covered by the projection thread-safety guarantee;
    // the cache races benignly (reference assignment is atomic, so the worst case is duplicated
    // work).
    private IReadOnlyList<TableRow<TSpace>>? _rows;

    internal TableView(Plane<TSpace> space, int headerRows, ProjectorScope<TSpace> scope)
    {
      Space = space;
      HeaderRows = headerRows;

      Header = new CellStrip<TSpace>(
        space.Slice(new Offset(0, 0), new Area(HasHeader ? ColumnCount : 0, headerRows)),
        Orientation.Horizontal,
        scope);

      Labels = LabelMap.FromHeader(Header);

      // Publish the columns as the ambient Column labels for the body's subtree, but only when a
      // header was actually declared: a headerless table pushes nothing, so a by-name lookup still
      // finds no scope and reports the headerless message. The origin PushLabels captures is this
      // table's own, the frame the header's ordinals are read in and every body row translates from.
      Scope = HasHeader ? scope.PushLabels(Labels, space.Origin) : scope;
    }

    /// <summary>The table's own header parsed once: the labels the bind rung binds by, and their citations.</summary>
    internal LabelMap Labels { get; }

    /// <summary>The table's full extent, header row(s) included.</summary>
    public Plane<TSpace> Space { get; }

    /// <summary>How many columns wide the table is.</summary>
    public int ColumnCount => Space.Width;

    /// <summary>How many body rows the table has, header row(s) excluded.</summary>
    public int RowCount => Space.Area.Height - HeaderRows;

    /// <summary>Whether a header row was declared. By-name lookups (<see cref="TableRow{TSpace}.this[string]"/>) need one.</summary>
    public bool HasHeader => HeaderRows > 0;

    /// <summary>The header row(s), when <see cref="HasHeader"/>; a zero-width strip when the table has none.</summary>
    public CellStrip<TSpace> Header { get; }

    /// <summary>Each column's header text, trimmed; the empty string for a column with no caption.</summary>
    public IReadOnlyList<string> ColumnNames => Labels.Labels;

    /// <summary>The address of the table's top-left cell, header included, with the extent the table was found in.</summary>
    public ProjectionLocation Location => ProjectionLocation.At(Space);

    /// <summary>The table's body rows, header row(s) excluded, built once per view.</summary>
    public IReadOnlyList<TableRow<TSpace>> Rows => _rows ??= BuildRows();

    /// <summary>
    /// The table's body rows, header row(s) excluded, built as the enumeration advances rather than
    /// cached: the same <see cref="TableRow{TSpace}"/> views <see cref="Rows"/> holds, so
    /// enumerating twice builds them twice.
    /// </summary>
    public IEnumerable<TableRow<TSpace>> StreamRows()
    {
      for (var row = HeaderRows; row < Space.Area.Height; row++)
      {
        var band = Space.Slice(new Offset(0, row), new Area(ColumnCount, 1));

        yield return new TableRow<TSpace>(row - HeaderRows, new CellStrip<TSpace>(band, Orientation.Horizontal, Scope), Scope);
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
    internal IEnumerable<TableRow<TSpace>> StreamBodyRows(BlankRowStrategy onBlank)
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
          Scope.Report(severity, Failure($"the row at {row.Location.A1} is blank; it was skipped"));

        // Skip and Tolerate both omit the record and keep reading.
      }
    }

    /// <summary>
    /// Every body row to the enclosing edge, each tagged blank or not — the source the Project
    /// (<c>blankRecord</c>) rung maps: a blank row yields its blank record, a non-blank row its
    /// normal one.
    /// </summary>
    internal IEnumerable<(TableRow<TSpace> Row, bool IsBlank)> StreamClassifiedRows()
    {
      foreach (var row in StreamRows())
        yield return (row, IsBlankRow(row));
    }

    /// <summary>A fully-blank row: every cell blank — the complement of "any value".</summary>
    private static bool IsBlankRow(TableRow<TSpace> row)
    {
      for (var column = 0; column < row.Count; column++)
        if (row[column].HasValue)
          return false;

      return true;
    }

    private int HeaderRows { get; }

    /// <summary>
    /// The scope the table was projected in — how a projection built on this view reports a
    /// failure against the table itself.
    /// </summary>
    internal ProjectorScope<TSpace> Scope { get; }

    /// <summary>Reports a problem against the table itself — its origin, its extent.</summary>
    internal ProjectionException Failure(string problem) => Scope.Failure(problem, Space);

    /// <summary>
    /// The same, for something that broke rather than disagreed — a declaration that cannot mean
    /// anything, which no tolerance boundary may report as a section that was not there.
    /// </summary>
    internal ProjectionException Fault(string problem) => Scope.Failure(problem, Space, null, isFault: true);

    /// <summary>Every body row in one list, allocated once at its size.</summary>
    private List<TableRow<TSpace>> BuildRows()
    {
      var rows = new List<TableRow<TSpace>>(RowCount);

      foreach (var row in StreamRows())
        rows.Add(row);

      return rows;
    }
  }
}
