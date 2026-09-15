using System.Collections.Generic;

using Unrect.Core;

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
  public sealed class TableView
  {
    // Views are built per projection and are not covered by the projection thread-safety guarantee;
    // the cache races benignly (reference assignment is atomic, so the worst case is duplicated
    // work).
    private IReadOnlyList<TableRow>? _rows;

    internal TableView(Plane<ICellValues> space, int headerRows, ProjectionContext context)
    {
      Space = space;
      HeaderRows = headerRows;

      Header = new CellStrip(
        space.Cut(new Offset(0, 0), new Area(HasHeader ? ColumnCount : 0, headerRows)),
        Orientation.Horizontal,
        context);

      Labels = LabelMap.FromHeader(Header, context);

      // Publish the columns as the ambient Column labels for the body's subtree, but only when a
      // header was actually declared: a headerless table pushes nothing, so a by-name lookup still
      // finds no scope and reports the headerless message. The origin PushLabels captures is this
      // table's own, the frame the header's ordinals are read in and every body row translates from.
      Context = HasHeader ? context.PushLabels(LabelAxis.Column, Labels) : context;
    }

    /// <summary>The table's own header parsed once: the labels the bind rung binds by, and their citations.</summary>
    internal LabelMap Labels { get; }

    /// <summary>The table's full extent, header row(s) included.</summary>
    public Plane<ICellValues> Space { get; }

    /// <summary>
    /// How many columns wide the table is. Free on an extent still being discovered: a width is
    /// settled before the first row is read.
    /// </summary>
    public int ColumnCount => Space.Width;

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
    public IReadOnlyList<string> ColumnNames => Labels.Labels;

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
        yield return new TableRow(index++, new CellStrip(band.Space, Orientation.Horizontal, band.Context), band.Context);
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
    internal IEnumerable<(Plane<ICellValues> Space, ProjectionContext Context)> StreamBands(int bandHeight)
    {
      for (var row = HeaderRows; Space.HasRow(row + bandHeight - 1); row += bandHeight)
      {
        var offset = new Offset(0, row);

        yield return (Space.Cut(offset, new Area(ColumnCount, bandHeight)), Context.Advance(offset));
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
    internal IReadOnlyList<int> IndicesOf(string columnName) => ((ILabelSource)Labels).IndicesOf(columnName);

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
  }
}
