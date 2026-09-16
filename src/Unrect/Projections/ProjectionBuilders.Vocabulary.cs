using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

using static Unrect.Strategies.AreaStrategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The leaves, the tables, the labels, the repeats and the alternation — what a declaration says
  /// about the document itself. Every leaf comes in a discovered, an explicit-count and a strategy
  /// form.
  /// </summary>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    // --- Leaves -------------------------------------------------------------------------------
    //
    // Two, and between them every reading a space can promise: the cell as a place, and the cell as
    // what it says. A kind — a number, a date, a formula — is a backend's claim about its own cells,
    // so a leaf that asserts one ships beside the backend that can answer it.

    /// <summary>
    /// One cell, as the address of itself: the leaf for a reading this vocabulary does not name.
    /// <para>
    /// A point answers the canonical questions — blank, text, what it says — and a backend's own
    /// extension answers the rest, so <c>Record(r =&gt; r["Amount"].Decimal())</c> and
    /// <c>Range(b =&gt; b[0, 0].Value())</c> both go through here. It is also what to hand to code
    /// that wants to cite a cell in a complaint of its own.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, Point<TSpace>> Point()
      => new PointProjection<TSpace>(Placement.Of(ExplicitArea(1, 1)));

    /// <summary>
    /// One cell, read as what it says — the total canonical leaf. Every space renders every cell, so
    /// the only thing that can go wrong is that there is nothing there; <c>OrBlank</c> turns that
    /// from a failure into a null.
    /// <para>
    /// It says what the cell says, which for a cell holding words is the word itself and for
    /// anything else is the backend's own rendering. A reading that asserts the cell <em>is</em>
    /// words, or is a number, is a kind assertion and belongs to a backend's vocabulary.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, string> AsText()
      => new TextProjection<TSpace, string>(Placement.Of(ExplicitArea(1, 1)), blankIsNull: false, text => text);

    /// <summary>One row, as wide as the leading columns that carry values.</summary>
    public static IProjection<TSpace, T> Row<T>(Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Horizontal, project, RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue(), "Row");

    /// <summary>One row exactly <paramref name="width"/> columns wide.</summary>
    public static IProjection<TSpace, T> Row<T>(int width, Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Horizontal, project, ExplicitArea(width, 1), $"Row({width})");

    /// <summary>One row, as wide as <paramref name="columns"/> selects.</summary>
    public static IProjection<TSpace, T> Row<T>(IColumnStrategy columns, Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Horizontal, project, RowsThenColumns(RowStrategies.TakeRows(1), columns), "Row");

    /// <summary>One column, as tall as the leading rows that carry values.</summary>
    public static IProjection<TSpace, T> Column<T>(Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Vertical, project, ColumnStrategies.TakeColumns(1).TakeRowsWhileAnyValue(), "Column");

    /// <summary>One column exactly <paramref name="height"/> rows tall.</summary>
    public static IProjection<TSpace, T> Column<T>(int height, Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Vertical, project, ExplicitArea(1, height), $"Column({height})");

    /// <summary>One column, as tall as <paramref name="rows"/> selects.</summary>
    public static IProjection<TSpace, T> Column<T>(IRowStrategy rows, Func<CellStrip<TSpace>, T> project)
      => Strip(Orientation.Vertical, project, ColumnsThenRows(ColumnStrategies.TakeColumns(1), rows), "Column");

    /// <summary>
    /// A rectangular region, read through a <see cref="CellBlock{TSpace}"/>: the maximal leading block of
    /// rows and columns that carry values.
    /// </summary>
    public static IProjection<TSpace, T> Range<T>(Func<CellBlock<TSpace>, T> project)
      => new BlockProjection<TSpace, T>(project, Placement.Of(DiscoveredBlock()), "Range");

    /// <summary>A region of exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IProjection<TSpace, T> Range<T>(int width, int height, Func<CellBlock<TSpace>, T> project)
      => new BlockProjection<TSpace, T>(project, Placement.Of(ExplicitArea(width, height)), $"Range({width}, {height})");

    /// <summary>A region extending as far as <paramref name="area"/> declares.</summary>
    public static IProjection<TSpace, T> Range<T>(IAreaStrategy area, Func<CellBlock<TSpace>, T> project)
      => new BlockProjection<TSpace, T>(
        project,
        Placement.Of(area ?? throw new ArgumentNullException(nameof(area))),
        "Range");

    /// <summary>
    /// The row that holds <paramref name="text"/>, as declared content: the projection finds that row,
    /// asserts the text is there, consumes the row at the full available width, and yields what the
    /// cell actually says — the file's spelling, untrimmed, not the argument's.
    /// <para>
    /// A caption is a node rather than a property of the section under it, so it is described,
    /// consumed once, and rendered into failure paths like anything else. Put a section under one
    /// with <c>Under</c>:
    /// <code>
    /// var lines   = Range(RowsWhileAnyValue(), b =&gt; b.Rows);
    /// var section = lines.Under(Caption("K-1 Lines 1-21"))
    ///                    .Until(RowContaining("Portfolio Income"), orEnd: true);
    /// </code>
    /// </para>
    /// <para>
    /// Matching is whole-cell, trimmed and case-insensitive — the same rule
    /// <see cref="RowContaining"/> uses, so a caption and a bound written from the same literal
    /// cannot disagree. Share the literal with a <c>const</c> when both are needed.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, string> Caption(string text)
      => new CaptionProjection<TSpace>(
        NotEmpty(text, nameof(text)),
        new Placement(OffsetStrategies.To(RowLandmarks.RowContaining(text)), FullRow()));

    // --- Tables — one mechanism at several degrees of declaredness ------------------------------
    //
    // Every rung below is the same table: a header consumed, then body bands, each read as one
    // record. What changes is how much of the reading is declared and how much is written out — you
    // write the row against this file's captions, or against fixed positions, or you take the cells
    // and do it yourself. A rung that fills a typed record by reflection asserts a kind per member,
    // so it ships with the backend that can answer one.

    /// <summary>
    /// A table whose record projection is written against <em>this file's</em> captions:
    /// <paramref name="headerRows"/> rows are read as the header, the captions they carry are
    /// handed to <paramref name="eachRow"/>, and the projection it returns is applied to every body
    /// row.
    /// <code>
    /// Table(headerRows: 1, eachRow: labels =&gt; Overlay(o =&gt; new Allocation(
    ///   Account: o.Next(Text().Right(labels["Account"])),
    ///   Weight:  o.Next(Decimal().OrBlank().Right(labels["Weight"])))))
    /// </code>
    /// <para>
    /// Two arrows, two moments. The bind runs <em>once per application of the table</em> — a table
    /// inside a repeat binds once per occurrence, each with its own header — after the header is
    /// read and before any body row — and builds a description; that description is then applied to
    /// each row by the engine, exactly as a row handed to
    /// <see cref="Table{T}(int, IProjection{TSpace, T}, string)"/> is. Captions locate columns once and
    /// rows then read positionally, which is how the machine has always worked; the bind is what
    /// makes those two phases visible.
    /// </para>
    /// <para>
    /// An <c>Overlay</c> is the layout this rung is written with, because a caption's position is
    /// absolute: an overlay hands every child the whole row and each one says which column it is,
    /// so a file that reorders its columns changes nothing but the numbers the map hands back.
    /// </para>
    /// <para>
    /// A caption the file does not carry, or carries twice, fails through the map and names the
    /// header cells involved (see <see cref="LabelMap"/>). A bind that reaches a value to decide
    /// what to declare — <c>labels.Has("Fee") ? a : b</c> — is expressible because the API cannot
    /// prevent it, discouraged, and nothing here is added to encourage it.
    /// </para>
    /// <para>
    /// A hoisted bind is a factory rather than a value —
    /// <c>static IProjection&lt;TSpace, T&gt; AllocationRow(LabelMap labels) =&gt; …</c> — with the
    /// dependence stated in its signature, and passing the method group is also what gives the
    /// record a name: <c>Table(1, AllocationRow)</c> labels every record <c>'AllocationRow'</c>,
    /// while a lambda has no identifier to borrow and the row renders as whatever it is
    /// (<c>Overlay</c>), or as its <c>.Named(…)</c>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">
    /// How many rows to read as the header. A bind needs captions, so this must be 1; a table with
    /// no header row has none to hand it, and declaring one is an error where it is written.
    /// </param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="eachRow"/> argument, so a bind
    /// passed as a method group labels every record with its name.
    /// </param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Table(headerRows, eachRow, BlankRowStrategy.Stop, declared);

    /// <summary>
    /// <inheritdoc cref="Table{T}(int, Func{LabelMap, IProjection{TSpace, T}}, string)"/> The
    /// <paramref name="onBlank"/> strategy says how a fully-blank body row is treated: <c>Stop</c>
    /// (the default, self-bounding), <c>Skip</c>, <c>Fault</c>, or <c>Tolerate</c>. Every
    /// non-<c>Stop</c> policy is not self-bounding, so the table runs to the enclosing edge (declare
    /// <c>Until</c> or a count to bound it sooner).
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs captions, so this must be 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjection<TSpace, T>> eachRow,
      BlankRowStrategy onBlank,
      [CallerArgumentExpression("eachRow")] string? declared = null)
    {
      if (eachRow is null)
        throw new ArgumentNullException(nameof(eachRow));

      var site = UseSite.From(declared, null);
      var rows = ValidateBindHeaderRows(headerRows);

      // The bind runs inside the table's own Project, so a bind that throws is wrapped exactly as
      // any other user code the engine calls: ProjectionEngine classifies it, and a broken read —
      // an IO failure, a null bug — is a fault no tolerance boundary can absorb.
      return new TableProjection<TSpace, IReadOnlyList<T>>(
        rows,
        table => ProjectRecords(table, BoundRow(table, eachRow), site, onBlank),
        TablePlacement(onBlank),
        "Table");
    }

    /// <summary>
    /// A table whose records are read by a <em>projection</em> rather than by a lambda:
    /// <paramref name="headerRows"/> rows are consumed as the header, and every body row is handed
    /// to <paramref name="eachRow"/> as its own one-row extent.
    /// <para>
    /// The row is a declaration like any other, so it is inspectable, reusable, and says in its own
    /// type what it demands of the space. Two shapes of row cover almost everything, and which one
    /// you write says how the columns are found:
    /// <code>
    /// // Adjacent columns, no coordinates anywhere: silence is adjacency, as in any flow.
    /// Table(headerRows: 1, eachRow: HorizontalFlow(h =&gt; new Allocation(
    ///   Account: h.Next(Text()),
    ///   Symbol:  h.Next(Text()),
    ///   Weight:  h.Next(Decimal()))))
    ///
    /// // Sparse, structurally-fixed columns: an overlay hands every child the whole row.
    /// Table(headerRows: 0, eachRow: Overlay(o =&gt; new Allocation(
    ///   Fund:    o.Next(Text().Right(1)),
    ///   Primary: o.Next(Decimal().OrBlank().Right(6)))))
    ///   .Below(RowContaining("ACCOUNT"))
    ///   .Sized(RowsWhileAnyValue())
    /// </code>
    /// A flow full of <c>Right(n)</c>, or an overlay with none, is worth a second look: flows are
    /// relative and overlays are grid-absolute, and reading both value and formula out of one cell
    /// is an overlay's job because a flow would step past it.
    /// </para>
    /// <para>
    /// <b>A leaf that measures itself is measuring the wrong thing here.</b> The band is as wide as
    /// the table; a projection that discovers its own extent measures <em>itself</em> inside that
    /// band rather than reporting it — <c>Row(cells =&gt; cells.Count)</c> is "as wide as the
    /// leading columns that carry values", which over a sparse export whose first column is empty
    /// is zero. To read the band as it was handed over, say so: <c>Range(WholeExtent(), …)</c>, or
    /// an <c>Overlay</c> whose children place themselves in it.
    /// </para>
    /// <para>
    /// The extent is the table's, not the row's: placement, the discovered block and the blank gap
    /// in front of it are as they are for every other table, and the row projection is applied
    /// inside the band it is handed and consumes as much or as little of it as it likes. A failure
    /// inside a row names the record it happened in — <c>Table[3] -&gt; 'eachRow'</c>, counting body
    /// records from zero, the same way a repeat indexes its occurrences.
    /// </para>
    /// <para>
    /// A header is consumed here rather than read: a row that reads <em>this file's</em> captions is
    /// the rung above, <see cref="Table{T}(int, Func{LabelMap, IProjection{TSpace, T}}, string)"/>, and
    /// here a headered table means "skip that row".
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="eachRow"/> argument, so a row
    /// hoisted into a local labels every record — <c>Table(0, allocation)</c> reads as
    /// <c>Table[3] -&gt; 'allocation'</c>. Pass <c>.Named(…)</c> to choose a name instead.
    /// </param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
    {
      if (eachRow is null)
        throw new ArgumentNullException(nameof(eachRow));

      // One row per record, which is what a table is until it is told otherwise, and no blank-band
      // policy: what the body runs over is the extent the placement discovered, exactly as far as
      // that reaches.
      var body = VerticalBands(1, eachRow, onBlank: null, declared).AsScaffolding();

      IProjection<TSpace, IReadOnlyList<T>> composed = ValidateHeaderRows(headerRows) == 0
        ? body
        : UnderColumnLabels(ColumnLabels(1).AsScaffolding(), body).AsScaffolding();

      return new UnitProjection<TSpace, IReadOnlyList<T>>(composed, new IProjection[] { eachRow }, "Table", TablePlacement());
    }

    /// <summary>
    /// The header read once, then the body beneath it resolving columns through what the header
    /// named. The unit above this owns the placement, so the flow sits where it is handed and the
    /// rows it takes are the only thing it decides.
    /// </summary>
    private static IProjection<TSpace, IReadOnlyList<T>> UnderColumnLabels<T>(IProjection<TSpace, LabelMap> header, IProjection<TSpace, IReadOnlyList<T>> body)
      => new FlowProjection<TSpace, IReadOnlyList<T>>(
        Orientation.Vertical,
        flow =>
        {
          // declared: null at both sites, and it is mandatory. Left to the compiler, the naming
          // ladder would label the children with this method's own locals, identifiers the user
          // never wrote.
          var columns = flow.Next(header, declared: null);

          return flow.Next(WithColumnLabels(columns, body), declared: null);
        },
        Placement.Default,
        null);


    /// <summary>
    /// Every body row as a dictionary keyed by the column captions, with the cells themselves for
    /// values — a point, so nothing about a cell is decided here and a reading is whatever the
    /// caller asks the point for. Keys are matched by <see cref="CaptionComparer"/>, so
    /// <c>row["contribution itd"]</c> and <c>row["ContributionITD"]</c> both find
    /// <c>"Contribution ITD"</c>.
    /// <para>
    /// The idiom: open an unfamiliar sheet with this, look at the captions and kinds, then graduate
    /// to <c>Table&lt;T&gt;()</c> once the columns are known.
    /// </para>
    /// <para>
    /// It promises one entry per column, so it is strict about the things that would break that
    /// promise: a column with no caption, and two captions that collide under the comparer, are
    /// both loud failures naming the cells involved.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, IReadOnlyList<IReadOnlyDictionary<string, Point<TSpace>>>> Table()
      => new TableProjection<TSpace, IReadOnlyList<IReadOnlyDictionary<string, Point<TSpace>>>>(
        1,
        DictionaryRows,
        TablePlacement(),
        "Table");

    /// <summary>
    /// A table with one header row, projected row by row — the bottom rung, where the cells are
    /// handed over and the reading is yours. What a lambda does is invisible to the type system, to
    /// tooling and to any analysis of the declaration; that is the standing trade, and this is
    /// where it is paid.
    /// </summary>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableRow<TSpace>, T> project) => Table(1, project);

    /// <inheritdoc cref="Table{T}(Func{TableRow{TSpace}, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow<TSpace>, T> project)
    {
      if (project is null)
        throw new ArgumentNullException(nameof(project));

      return new TableProjection<TSpace, IReadOnlyList<T>>(
        ValidateHeaderRows(headerRows),
        table => (IReadOnlyList<T>)table.StreamRows().Select(project).ToList(),
        TablePlacement(),
        "Table");
    }

    /// <summary>
    /// <inheritdoc cref="Table{T}(Func{TableRow{TSpace}, T})"/> The <paramref name="onBlank"/> strategy says
    /// how a fully-blank body row is treated: <c>Stop</c> (the default, self-bounding), <c>Skip</c>,
    /// <c>Fault</c>, or <c>Tolerate</c>. Every non-<c>Stop</c> policy is not self-bounding, so the
    /// table runs to the enclosing edge (declare <c>Until</c> or a count to bound it sooner).
    /// </summary>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each body row.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableRow<TSpace>, T> project, BlankRowStrategy onBlank)
      => Table(1, project, onBlank);

    /// <inheritdoc cref="Table{T}(Func{TableRow{TSpace}, T}, BlankRowStrategy)"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow<TSpace>, T> project, BlankRowStrategy onBlank)
    {
      if (project is null)
        throw new ArgumentNullException(nameof(project));

      if (onBlank.IsStop)
        return Table(headerRows, project);

      return new TableProjection<TSpace, IReadOnlyList<T>>(
        ValidateHeaderRows(headerRows),
        table => (IReadOnlyList<T>)table.StreamBodyRows(onBlank).Select(project).ToList(),
        TablePlacement(onBlank),
        "Table");
    }

    /// <summary>
    /// <inheritdoc cref="Table{T}(Func{TableRow{TSpace}, T})"/> A fully-blank body row is projected to a
    /// record by <paramref name="blankRecord"/> rather than skipped — where <c>Skip</c> omits an
    /// entry, this includes one, usually built from the row's <see cref="TableRow{TSpace}.Index"/>. The
    /// table runs to the enclosing edge (declare <c>Until</c> or a count to bound it sooner).
    /// </summary>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each non-blank body row.</param>
    /// <param name="blankRecord">The record produced for a fully-blank body row.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableRow<TSpace>, T> project, Func<TableRow<TSpace>, T> blankRecord)
      => Table(1, project, blankRecord);

    /// <inheritdoc cref="Table{T}(Func{TableRow{TSpace}, T}, Func{TableRow{TSpace}, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each non-blank body row.</param>
    /// <param name="blankRecord">The record produced for a fully-blank body row.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow<TSpace>, T> project, Func<TableRow<TSpace>, T> blankRecord)
    {
      if (project is null)
        throw new ArgumentNullException(nameof(project));

      if (blankRecord is null)
        throw new ArgumentNullException(nameof(blankRecord));

      // Project continues past blanks, so it needs the run-to-edge extent; any non-Stop strategy
      // selects ToEdgeBlock().
      return new TableProjection<TSpace, IReadOnlyList<T>>(
        ValidateHeaderRows(headerRows),
        table => (IReadOnlyList<T>)table.StreamClassifiedRows()
          .Select(r => r.IsBlank ? blankRecord(r.Row) : project(r.Row)).ToList(),
        TablePlacement(BlankRowStrategy.Skip),
        "Table");
    }

    /// <summary>
    /// A table with one header row, read as a whole: past any blank rows, then rows and columns
    /// while they carry values. Column names come from the header, so rows can be read by name as
    /// well as by index. For the table that does not decompose row by row.
    /// </summary>
    public static IProjection<TSpace, T> Table<T>(Func<TableView<TSpace>, T> project) => Table(1, project);

    /// <summary>
    /// A table with <paramref name="headerRows"/> header rows, which must be 0 or 1 — multi-row
    /// headers are not supported in this release. With 0, every row is a body row and columns can
    /// only be read by index.
    /// </summary>
    public static IProjection<TSpace, T> Table<T>(int headerRows, Func<TableView<TSpace>, T> project)
      => new TableProjection<TSpace, T>(ValidateHeaderRows(headerRows), project, TablePlacement(), "Table");

    // --- Labels — the scope-introducer primitives -----------------------------------------------
    //
    // The three verbs a labelled axis decomposes into, from which the built-in Table is
    // reimplementable: MANUFACTURE a map (ColumnLabels, or the literal LabelMap.Of), PROVIDE it to a
    // subtree (WithColumnLabels), and READ it (Record). The row twins — RowLabels/WithRowLabels — are
    // deferred: a left/right header spans the height, so collecting it forces the full height before
    // anything can be addressed, which is a different cost model, not merely untested.

    /// <summary>
    /// Reads and consumes a table's header row as a <see cref="LabelMap"/>, without projecting the
    /// body — the map a <see cref="WithColumnLabels{T}(LabelMap, IProjection{TSpace, T})"/> then provides to
    /// the rows beneath it. The header parse is the one a built-in <c>Table</c> runs, so the labels,
    /// their ordinals and the matching rule are identical.
    /// <para>
    /// It takes the width of the band it is handed rather than discovering one of its own, so the
    /// labels describe the same columns the body beneath them reads. An extent with no room for the
    /// header row — no rows, or no columns — is an absorbable failure, so a table over an empty
    /// region answers to <c>.Optional()</c> like any other absent section.
    /// </para>
    /// </summary>
    /// <param name="headerRows">How many rows to read as the header. Only 1 is supported in this release.</param>
    public static IProjection<TSpace, LabelMap> ColumnLabels(int headerRows = 1)
    {
      if (headerRows != 1)
        throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "ColumnLabels reads exactly one header row in this release.");

      return new ColumnLabelsProjection<TSpace>(headerRows, Placement.Default);
    }

    /// <summary>
    /// Pushes <paramref name="map"/> as the ambient column labels for <paramref name="body"/>'s whole
    /// declaration subtree, then reads <paramref name="body"/> where it stands. A record inside it
    /// resolves a column by name through this map — <c>row.Decimal("Amount")</c> — with the ordinal
    /// translated from the frame the labels were read in to the reading frame and bounds-checked, so a
    /// label whose column has narrowed out of a slice is a clean, absorbable failure rather than a
    /// silent read of the neighbour. Transparent: it adds no path segment and forces nothing of its
    /// own, so the body reads exactly as it would unwrapped.
    /// </summary>
    /// <typeparam name="T">What the body reads.</typeparam>
    /// <param name="map">The columns to make resolvable by name for the body.</param>
    /// <param name="body">The projection read under the pushed labels.</param>
    public static IProjection<TSpace, T> WithColumnLabels<T>(LabelMap map, IProjection<TSpace, T> body)
      => new WithLabelsProjection<TSpace, T>(
        LabelAxis.Column,
        map ?? throw new ArgumentNullException(nameof(map)),
        body ?? throw new ArgumentNullException(nameof(body)),
        Placement.Default);

    /// <summary>
    /// One body row, read by <paramref name="record"/> — the compute-legal binder decoupled from
    /// <c>Table</c>. Its extent is a one-row band at the full width, so under a
    /// <see cref="VerticalRepeat{T}"/> each occurrence reads one row and the repeat stops past the
    /// last. Columns are resolved by name through whatever <see cref="WithColumnLabels{T}(LabelMap,
    /// IProjection{TSpace, T})"/> pushed; used with no labels in scope, a by-name read reports the headerless
    /// message, exactly as a headerless table's row does.
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="record">The reading applied to one body row.</param>
    public static IProjection<TSpace, T> Record<T>(Func<TableRow<TSpace>, T> record)
      => new RecordProjection<TSpace, T>(record ?? throw new ArgumentNullException(nameof(record)), Placement.Of(FullRow()));

    // --- Labelled pairs -------------------------------------------------------------------------

    /// <summary>
    /// One labelled pair for a <see cref="Fields"/> block: the cell reading <paramref
    /// name="label"/>, and the value cell immediately to its right.
    /// <para>
    /// A label is matched whole-cell, trimmed, case-insensitively, and <em>with a trailing colon
    /// ignored on both sides</em> — a colon is presentation of a label, not part of it, and an
    /// export that drops it next year should not break the declaration. That rule applies here and
    /// nowhere else.
    /// </para>
    /// </summary>
    public static Field Field(string label) => new Field(NotEmptyLabel(label));

    /// <summary>
    /// A block of labelled pairs — the card of name/value rows that heads so many reports. Two
    /// columns wide and as many rows as there are fields, keyed by the labels the declaration wrote:
    /// <code>
    /// var entity = Fields(Field("EIN"), Field("Entity Type"), Field("Deal Type"));
    /// </code>
    /// <para>
    /// The extent comes from the child count, so there is no width and height to get wrong and
    /// adding a field is one line. The block finds itself: it anchors on the first field's label,
    /// column first and then row, so the labels are the declaration <em>and</em> the anchor rather
    /// than the same literal written twice. <c>.On(…)</c> replaces that anchor when a sheet holds
    /// two blocks with the same first label.
    /// </para>
    /// <para>
    /// Values are the cells themselves, and a blank one is a blank cell rather than a failure: the
    /// labels are the structure, the values are data. Like <c>Caption</c>, a block searches from the cursor
    /// and can jump, so inside a repeat the anchor wants hoisting onto the item as well.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, IReadOnlyDictionary<string, Point<TSpace>>> Fields(params Field[] fields)
    {
      if (fields is null)
        throw new ArgumentNullException(nameof(fields));

      if (fields.Length == 0)
        throw new ArgumentException("A Fields block must declare at least one field.", nameof(fields));

      for (var index = 0; index < fields.Length; index++)
        if (fields[index] is null)
          throw new ArgumentException($"Field {index + 1} is null.", nameof(fields));

      // Cloned before validating, so what is checked is what the projection will hold.
      var declared = (Field[])fields.Clone();

      // Two labels must be distinct under BOTH relations, because they answer different questions
      // and neither contains the other. Matching decides whether two fields would accept the same
      // cell; the key comparer decides whether they would collide as entries in the result. A pair
      // that passed only the first would silently produce one entry for two fields.
      for (var index = 0; index < declared.Length; index++)
        for (var earlier = 0; earlier < index; earlier++)
        {
          if (CellMatching.LabelsMatch(declared[earlier].Label, declared[index].Label))
            throw new ArgumentException(
              $"Two fields carry the label '{declared[index].Label}'; "
              + "labels are matched ignoring case, surrounding whitespace and a trailing colon.",
              nameof(fields));

          if (CaptionComparer.Default.Equals(declared[earlier].Label, declared[index].Label))
            throw new ArgumentException(
              $"The labels '{declared[earlier].Label}' and '{declared[index].Label}' would be the same key; "
              + "a block's keys ignore case and all whitespace, so these two fields would collide into one entry.",
              nameof(fields));
        }

      // Built once: the children are a property of the declaration, not of any application of it.
      var pairs = new IProjection<TSpace, Point<TSpace>>[declared.Length];

      for (var index = 0; index < declared.Length; index++)
        pairs[index] = new FieldProjection<TSpace>(declared[index].Label, Placement.Of(ExplicitArea(2, 1)));

      return new FlowProjection<TSpace, IReadOnlyDictionary<string, Point<TSpace>>>(
        Orientation.Vertical,
        cursor =>
        {
          var values = new Dictionary<string, Point<TSpace>>(declared.Length, CaptionComparer.Default);

          // declared: null — without it the naming ladder would label every child with this
          // helper's own loop variable, an identifier the user never wrote.
          for (var index = 0; index < declared.Length; index++)
            values[declared[index].Label] = cursor.Next(pairs[index], declared: null);

          return values;
        },
        FieldsPlacement(declared[0].Label),
        description: "Fields");
    }

    // --- Repetition and tiling ------------------------------------------------------------------
    //
    // Two ways for one declaration to be read many times, and the difference is where the boundary
    // between occurrences comes from.
    //
    // A REPEAT repeats a PATTERN. Each occurrence is as big as the item's own placement makes it, so
    // occurrences may differ in size, and the run ends where the pattern stops matching.
    //
    // A TILER repeats a FIXED-DIMENSION SPACE. The extent is cut into bands of a declared stride and
    // each band is projected; nothing is searched for, no occurrence can be a different size, and the
    // run ends when a whole band is no longer left.

    /// <summary>
    /// One item stacked downwards as many times as the space supports.
    /// <para>
    /// The axis is in the name because the concept has one, and the two forms are spelled
    /// symmetrically for the same reason the flows are: no substrate's dominant axis is treated as
    /// the normal case that needs no marking.
    /// </para>
    /// <para>
    /// <paramref name="separatedBy"/> is the offset <em>between</em> items and is never applied
    /// before the first — a leading gap belongs to the repeat itself
    /// (<c>VerticalRepeat(...).AfterBlankRows()</c>). It is also load-bearing for termination: when
    /// content follows the last item, the separator is what carries the cursor over the gap so the
    /// repetition can recognise that the next item is not there. Without it, an item whose own
    /// placement still fits will be applied to that content and fail loudly.
    /// </para>
    /// <para>
    /// <paramref name="atLeast"/> turns "found nothing" into a good error instead of a silently
    /// empty list.
    /// </para>
    /// <para>
    /// A run ends where the item stops placing, or where <c>.Until(landmark)</c> bounds it —
    /// including <c>.Until(RowWhere(...), orEnd: true)</c> for "stop at a blank row", where
    /// <c>orEnd</c> lets a run that reaches the sheet's edge without meeting one end there rather
    /// than fail for want of the landmark. A landmark is located before the walk begins, so such a
    /// bound reads ahead to it and the walk then reads behind that point. A blank band between
    /// occurrences is a separator (<c>separatedBy: BlankRows()</c>), never a terminator; where the
    /// occurrences really are one row each and a blank row is a policy question — or where the
    /// reading must stay forward-only —
    /// <see cref="VerticalBands{T}(int, IProjection{TSpace, T}, BlankRowStrategy?, string)"/> is the
    /// spelling that says so.
    /// </para>
    /// <para>
    /// One malformed section among a hundred good ones is recovered by re-anchoring rather than by
    /// a parameter: give the item a fallback that swallows up to the next anchor and yields a
    /// marker, then drop the markers. The <c>Warning</c> from <c>Else</c> says which section failed,
    /// where, and why, so nothing is lost by carrying on.
    /// </para>
    /// <example>
    /// The anchor belongs to the item, outside the boundary: finding no further anchor is how the
    /// repetition knows to stop, so that one failure must not be tolerated. Everything after the
    /// anchor is inside the boundary, where a malformed section is swallowed and reported.
    /// <code>
    /// var item =
    ///   section.Select(s => (Section?)s)          // the section as it should be
    ///     .Else(Row(_ => (Section?)null))         // ... or just its label row, and a warning
    ///     .On(RowContaining("Section"));          // ... starting at the next section label
    ///
    /// var sections = VerticalRepeat(item).Select(all => all.Where(s => s is not null).ToList());
    ///
    /// var result = sections.MapWithDiagnostics(sheet);   // result.Diagnostics names the bad one
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="item"/> argument, so an item
    /// hoisted into a local is called that in every path — <c>VerticalRepeat(investorDetail)</c>
    /// reads as <c>VerticalRepeat[2] -&gt; 'investorDetail'</c>. It is not a naming API; pass
    /// <c>.Named(…)</c> to choose a name, and note that an item written inline keeps its description
    /// instead.
    /// </param>
    public static IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Repeat(Orientation.Vertical, item, separatedBy, atLeast, declared);

    /// <summary>
    /// One item stacked rightwards as many times as the space supports; see
    /// <see cref="VerticalRepeat{T}"/> for <paramref name="separatedBy"/>,
    /// <paramref name="atLeast"/>, and how the item is named.
    /// </summary>
    public static IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Repeat(Orientation.Horizontal, item, separatedBy, atLeast, declared);

    /// <summary>
    /// The extent cut into bands <paramref name="rows"/> rows tall, top to bottom, each projected by
    /// <paramref name="each"/>. A band's boundaries come from the stride alone — nothing is searched
    /// for and no band can be a different size — and the tiling ends when fewer than
    /// <paramref name="rows"/> rows are left, so a trailing part-band is not a band.
    /// <para>
    /// It declares no extent of its own: how far it runs is whatever places it — a <c>.Sized</c>, a
    /// discovered block, or simply the space it is handed. That is the difference from
    /// <see cref="VerticalRepeat{T}"/>, which discovers each occurrence's size from the item and
    /// stops where the item stops fitting.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one band reads.</typeparam>
    /// <param name="rows">How many rows one band is; at least 1.</param>
    /// <param name="each">The projection applied to each band.</param>
    /// <param name="onBlank">
    /// How a fully-blank band is treated: <c>Stop</c> ends the tiling there, <c>Skip</c> and
    /// <c>Tolerate</c> omit the band and carry on (<c>Tolerate</c> recording an <c>Info</c>), and
    /// <c>Fault</c> fails. Null (the default) projects every band the extent holds.
    /// </param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="each"/> argument, so a band
    /// projection hoisted into a local labels every band — <c>VerticalBands(1, allocation)</c> reads
    /// as <c>VerticalBands[3] -&gt; 'allocation'</c>. Pass <c>.Named(…)</c> to choose a name instead.
    /// </param>
    public static IProjection<TSpace, IReadOnlyList<T>> VerticalBands<T>(
      int rows,
      IProjection<TSpace, T> each,
      BlankRowStrategy? onBlank = null,
      [CallerArgumentExpression("each")] string? declared = null)
      => Bands(Orientation.Vertical, AtLeastOneBand(rows, nameof(rows)), each, onBlank, declared);

    /// <summary>
    /// The extent cut into bands <paramref name="columns"/> columns wide, left to right; see
    /// <see cref="VerticalBands{T}"/> for how a band is cut and how the tiling ends.
    /// <paramref name="onBlank"/> is a vertical blank-row policy and is rejected here; the parameter
    /// exists for call-site symmetry.
    /// <para>
    /// A band spans the full height, so over an extent whose height is still being discovered this
    /// settles it before the first band — which is what a column-wise reading of a vertically
    /// discovered region costs.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one band reads.</typeparam>
    /// <param name="columns">How many columns one band is; at least 1.</param>
    /// <param name="each">The projection applied to each band.</param>
    /// <param name="onBlank">Rejected; see the summary.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="each"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> HorizontalBands<T>(
      int columns,
      IProjection<TSpace, T> each,
      BlankRowStrategy? onBlank = null,
      [CallerArgumentExpression("each")] string? declared = null)
      => Bands(Orientation.Horizontal, AtLeastOneBand(columns, nameof(columns)), each, onBlank, declared);

    // --- Alternatives -------------------------------------------------------------------------

    /// <summary>
    /// The first of <paramref name="alternatives"/> that matches, tried in declaration order
    /// against the same extent — one report, several vendor layouts. Every alternative that does
    /// not match leaves an <c>Info</c> diagnostic saying why, readable through
    /// <c>MapWithDiagnostics</c>; if none matches, the failure lists all of them side by side.
    /// <para>
    /// Alternatives share a result type: <c>Select</c> each variant into whatever shape of result
    /// the caller wants before handing them over.
    /// </para>
    /// <para>
    /// An alternative that cannot fail makes everything after it unreachable, so a boundary such as
    /// <c>Optional</c> belongs around the choice rather than inside one of its arms. A failed
    /// attempt's diagnostics are rolled back, but nothing else about it is: alternatives are tried
    /// for real, and must not have side effects worth undoing. A projection that broke rather than
    /// disagreed — a null reference, a bad index — is a bug in the reading code and stops the
    /// choice instead of moving it on to the next arm.
    /// </para>
    /// </summary>
    public static IProjection<TSpace, T> Choice<T>(params IProjection<TSpace, T>[] alternatives)
    {
      if (alternatives is null)
        throw new ArgumentNullException(nameof(alternatives));

      if (alternatives.Length < 2)
        throw new ArgumentException("A choice needs at least two alternatives.", nameof(alternatives));

      for (var index = 0; index < alternatives.Length; index++)
        if (alternatives[index] is null)
          throw new ArgumentException($"Alternative {index + 1} is null.", nameof(alternatives));

      return new ChoiceProjection<TSpace, T>(alternatives, Placement.Default);
    }

    // --- Shared construction ------------------------------------------------------------------

    /// <summary>
    /// Applies a record projection to every body row of a table, through the engine — so a record's
    /// own placement is resolved exactly once, where every other placement is, and a failure inside
    /// one carries the path and the cell. Which rows become records is
    /// <paramref name="onBlank"/>'s: under <c>Stop</c> the extent already excludes blank rows and
    /// this walks every row there is.
    /// </summary>
    private static IReadOnlyList<T> ProjectRecords<T>(TableView<TSpace> table, IProjection<TSpace, T> eachRow, UseSite site, BlankRowStrategy onBlank)
    {
      // Grown rather than pre-sized, as the other row rungs are: asking how many records there are
      // is the forcing question streaming exists to avoid.
      var records = new List<T>();

      foreach (var row in table.StreamBodyRows(onBlank))
      {
        // The index belongs to the table's own segment and the label to the record's, exactly as a
        // repeat labels its occurrences. It counts body rows rather than records, so a record still
        // cites the row it was read from where a blank one was passed over.
        var scope = row.Context.WithIndex(row.Index).WithUseSite(site);

        records.Add(ProjectionEngine.Apply(eachRow, row.Space, scope).Value);
      }

      records.TrimExcess();

      return records;
    }

    /// <summary>
    /// Runs a row bind: once per application of the table, after the header has been read and before any body
    /// row, which is the moment the caption map exists and the only moment the description is
    /// built. A bind that throws is not caught here — the engine wraps every foreign exception a
    /// projection raises and classifies it, so a broken read stays a fault and a disagreement with
    /// the data stays absorbable.
    /// </summary>
    private static IProjection<TSpace, T> BoundRow<T>(TableView<TSpace> table, Func<LabelMap, IProjection<TSpace, T>> eachRow)
      => eachRow(table.Labels)
        ?? throw table.Fault("the row bind returned null; it must return the projection that reads one record");

    private static IProjection<TSpace, T> Strip<T>(Orientation orientation, Func<CellStrip<TSpace>, T> project, IAreaStrategy area, string description)
      => new StripProjection<TSpace, T>(orientation, project, Placement.Of(area), description);

    private static IProjection<TSpace, IReadOnlyList<T>> Repeat<T>(
      Orientation orientation,
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy,
      int atLeast,
      string? declared)
    {
      if (atLeast < 0)
        throw new ArgumentOutOfRangeException(nameof(atLeast), atLeast, "A repeat cannot require a negative number of occurrences.");

      // A repeat has one item rather than an nth child, so there is no ordinal to fall back on: an
      // item that is not a plain identifier keeps its description, exactly as before.
      return new RepeatProjection<TSpace, T>(item, separatedBy, orientation, atLeast, UseSite.From(declared, null), Placement.Default);
    }

    private static IProjection<TSpace, IReadOnlyList<T>> Bands<T>(
      Orientation orientation,
      int stride,
      IProjection<TSpace, T> each,
      BlankRowStrategy? onBlank,
      string? declared)
    {
      if (each is null)
        throw new ArgumentNullException(nameof(each));

      if (onBlank is not null && orientation == Orientation.Horizontal)
        throw new ArgumentException("onBlank is a vertical blank-row policy; HorizontalBands does not support it.", nameof(onBlank));

      // A tiler has one band projection rather than an nth child, so there is no ordinal to fall
      // back on: one written inline keeps its description, as a repeat's item does.
      return new BandsProjection<TSpace, T>(each, orientation, stride, UseSite.From(declared, null), onBlank, Placement.Default);
    }

    private static int AtLeastOneBand(int stride, string parameter)
      => stride >= 1
        ? stride
        : throw new ArgumentOutOfRangeException(parameter, stride, "A band is at least one row or column across.");

    /// <summary>Validates a layout lambda where the caller's parameter name is what the user typed.</summary>
    private static Layout<TSpace, T> NotNull<T>(Layout<TSpace, T> build, string parameter) => build ?? throw new ArgumentNullException(parameter);

    /// <summary>
    /// A caption that could never match anything is a declaration error, not a per-file one: a
    /// blank cell is <c>Blank</c> and never <c>Text("")</c>, so an empty caption is unsatisfiable.
    /// </summary>
    private static string NotEmpty(string text, string parameter)
    {
      if (text is null)
        throw new ArgumentNullException(parameter);

      if (text.Trim().Length == 0)
        throw new ArgumentException("A caption cannot be empty or whitespace.", parameter);

      return text;
    }

    /// <summary>One row, at the full available width — a caption row spans the sheet.</summary>
    private static IAreaStrategy FullRow()
      => RowsThenColumns(RowStrategies.TakeRows(1), ColumnStrategies.AllColumns());

    private static int NotNegative(int count, string parameter)
      => count >= 0 ? count : throw new ArgumentOutOfRangeException(parameter, count, "An offset cannot be negative.");

    private static Placement TablePlacement() => TablePlacement(BlankRowStrategy.Stop);

    /// <summary>
    /// The table body's placement: the offset skips to the first non-blank cell; the area is
    /// <see cref="DiscoveredBlock"/> for <c>Stop</c>, otherwise the run-to-edge <see cref="ToEdgeBlock"/>.
    /// </summary>
    private static Placement TablePlacement(BlankRowStrategy onBlank)
      => new Placement(OffsetStrategies.SkipToFirstNonBlankCell(), onBlank.IsStop ? DiscoveredBlock() : ToEdgeBlock());

    private static IAreaStrategy DiscoveredBlock() => RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue();

    /// <summary>
    /// The same width rule as <see cref="DiscoveredBlock"/>, but the height runs to the enclosing
    /// edge instead of stopping at the first blank row — the extent a non-self-bounding
    /// <see cref="BlankRowStrategy"/> needs so the walker can see and act on interior blank rows.
    /// </summary>
    private static IAreaStrategy ToEdgeBlock() => RowStrategies.AllRows().TakeColumnsWhileAnyValue();

    private static int ValidateHeaderRows(int headerRows)
      => headerRows == 0 || headerRows == 1
        ? headerRows
        : throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A table has either 0 or 1 header rows; multi-row headers are not supported in this release.");

    /// <summary>
    /// A bind is handed the captions, so there have to be some. A table declared with no header row
    /// has none, which is a declaration that cannot mean anything rather than a file that
    /// disagrees — so it fails where it is written, not per file.
    /// </summary>
    private static int ValidateBindHeaderRows(int headerRows)
      => headerRows == 0
        ? throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A table whose rows are declared from its captions needs a header row to read them from; headerRows must be 1.")
        : ValidateHeaderRows(headerRows);
  }
}