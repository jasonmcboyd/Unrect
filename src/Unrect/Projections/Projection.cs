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
  /// The projection vocabulary. <c>using static Unrect.Projections.Projection;</c> is the only
  /// import a projection declaration needs: every leaf comes in a discovered, an explicit-count,
  /// and a strategy form, and the common offsets are re-exported here so the strategy layer stays
  /// optional.
  /// </summary>
  public static partial class Projection
  {
    // --- Leaves -------------------------------------------------------------------------------

    /// <summary>A single cell.</summary>
    public static IProjection<T> Cell<T>(Func<CellValue, T> project)
      => new CellProjection<T>(project, Placement.Of(ExplicitArea(1, 1)));

    // --- Typed leaves ---------------------------------------------------------------------------
    //
    // A cell whose kind the declaration states. The family is closed over CellValue's canonical
    // accessor set and mirrors it 1:1 — six kinds of reading over six cell kinds, because Number is
    // read three ways and two kinds (Blank, Error) have no leaf at all. There is no Long(), Single(), Money() or Enum<T>(): a CLR
    // conversion beyond that set is Select territory (Integer().Select(i => (long)i)), one-way and
    // honest about it. Nothing is added to Core to serve a leaf, and adding an accessor to Core
    // does not add one here — GetDate is a transformation of GetDateTime, not a different reading
    // of the cell, so it has no leaf.
    //
    // Under `using static Projection`, Decimal/Double/Boolean sit beside the framework types of the same
    // name. Type positions and the keyword aliases are unaffected (decimal.Parse, double.IsNaN);
    // only a static member reached through the FRAMEWORK TYPE NAME — Decimal.ToDouble(x) — stops
    // resolving. Write decimal.ToDouble-style calls through the keyword, which is the usual spelling.

    /// <summary>One cell holding text.</summary>
    public static IProjection<string> Text()
      => Typed<string>(CellKind.Text, "Text", CellReading.ReadString);

    /// <summary>
    /// One cell holding a number, read as a <see cref="decimal"/> — the accessor that keeps a
    /// spreadsheet's exact decimal where the file carried one.
    /// </summary>
    public static IProjection<decimal> Decimal()
      => Typed<decimal>(CellKind.Number, "Decimal", CellReading.ReadDecimal);

    /// <summary>
    /// One cell holding a whole number. A number that is really there but is fractional or out of
    /// range fails as a conversion, not as a kind — the cell is a <c>Number</c> either way.
    /// </summary>
    public static IProjection<int> Integer()
      => Typed<int>(CellKind.Number, "Integer", CellReading.ReadInteger);

    /// <summary>One cell holding a number, read as a <see cref="double"/>.</summary>
    public static IProjection<double> Double()
      => Typed<double>(CellKind.Number, "Double", CellReading.ReadDouble);

    /// <summary>
    /// One cell holding a date or time, verbatim. The time of day is kept: truncating is
    /// consumer-side (<c>Date().Select(d =&gt; d.Date)</c>), because a leaf that silently handed
    /// back less than the cell holds would be the only one in the vocabulary that did.
    /// </summary>
    public static IProjection<DateTime> Date()
      => Typed<DateTime>(CellKind.Temporal, "Date", CellReading.ReadDateTime);

    /// <summary>One cell holding a boolean.</summary>
    public static IProjection<bool> Boolean()
      => Typed<bool>(CellKind.Boolean, "Boolean", CellReading.ReadBoolean);

    private static IProjection<T> Typed<T>(CellKind kind, string description, CellReader<T> read)
      => new TypedCellProjection<T>(kind, description, read, Placement.Of(ExplicitArea(1, 1)), blankIsNull: false);

    /// <summary>One row, as wide as the leading columns that carry values.</summary>
    public static IProjection<T> Row<T>(Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue(), "Row");

    /// <summary>One row exactly <paramref name="width"/> columns wide.</summary>
    public static IProjection<T> Row<T>(int width, Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, ExplicitArea(width, 1), $"Row({width})");

    /// <summary>One row, as wide as <paramref name="columns"/> selects.</summary>
    public static IProjection<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, RowsThenColumns(RowStrategies.TakeRows(1), columns), "Row");

    /// <summary>One column, as tall as the leading rows that carry values.</summary>
    public static IProjection<T> Column<T>(Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ColumnStrategies.TakeColumns(1).TakeRowsWhileAnyValue(), "Column");

    /// <summary>One column exactly <paramref name="height"/> rows tall.</summary>
    public static IProjection<T> Column<T>(int height, Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ExplicitArea(1, height), $"Column({height})");

    /// <summary>One column, as tall as <paramref name="rows"/> selects.</summary>
    public static IProjection<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ColumnsThenRows(ColumnStrategies.TakeColumns(1), rows), "Column");

    /// <summary>
    /// A rectangular region, read through a <see cref="CellBlock"/>: the maximal leading block of
    /// rows and columns that carry values.
    /// </summary>
    public static IProjection<T> Range<T>(Func<CellBlock, T> project)
      => new BlockProjection<T>(project, Placement.Of(DiscoveredBlock()), "Range");

    /// <summary>A region of exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IProjection<T> Range<T>(int width, int height, Func<CellBlock, T> project)
      => new BlockProjection<T>(project, Placement.Of(ExplicitArea(width, height)), $"Range({width}, {height})");

    /// <summary>A region extending as far as <paramref name="area"/> declares.</summary>
    public static IProjection<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => new BlockProjection<T>(
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
    public static IProjection<string> Caption(string text)
      => new CaptionProjection(
        NotEmpty(text, nameof(text)),
        new Placement(OffsetStrategies.To(RowLandmarks.RowContaining(text)), FullRow()));

    // --- Tables — one mechanism at five degrees of declaredness ---------------------------------
    //
    // Every rung below is the same table: a header consumed, then body bands, each read as one
    // record. What changes is how much of the reading is declared and how much is written out —
    // reflection writes the row (1), you adjust what it writes (2), you write the row against this
    // file's captions (3) or against fixed positions (4), or you take the cells and do it yourself
    // (5). Graduate up as a table's shape firms, and reach for rung 1 first.

    /// <summary>
    /// Every body row as a <typeparamref name="T"/>, with each member filled from the column whose
    /// caption matches its name and read as the member's own type declares.
    /// <para>
    /// Captions bind to members by <see cref="CaptionComparer"/> — case and whitespace are ignored,
    /// so <c>"Contribution ITD"</c> fills <c>ContributionItd</c> with nothing declared. The
    /// member's type chooses the kind to assert and the accessor to use, from the same closed set
    /// the typed leaves cover: <c>string</c>, <c>decimal</c>, <c>double</c>, <c>int</c>,
    /// <c>DateTime</c>, <c>bool</c>, <c>CellValue</c>, and the nullable forms. A nullable member
    /// tolerates a <em>blank</em> cell and still fails on the wrong kind — tolerating a blank says
    /// something about the data, tolerating a kind would say something about the format, and no
    /// real format has that.
    /// </para>
    /// <para>
    /// <typeparamref name="T"/> is built through its single parameterized constructor when it has
    /// one and no parameterless constructor (the positional-record case), otherwise through a
    /// parameterless constructor and its settable properties. Everything reflective is resolved
    /// once, when the projection is built; a bad type is an error at that point, not per file.
    /// </para>
    /// <para>
    /// Binding is strict in one direction: every member must find a column, and one that does not
    /// is a loud failure listing the table's captions. A column no member claims is fine — real
    /// reports carry columns a consumer does not want.
    /// </para>
    /// </summary>
    public static IProjection<IReadOnlyList<T>> Table<T>() => TypedRows<T>(null);

    /// <summary>
    /// <see cref="Table{T}()"/> with per-member declarations: <c>Column</c> for a caption the
    /// comparer would not have found, <c>Ignore</c> for a member this table does not carry.
    /// <code>
    /// Table&lt;Transaction&gt;(bind =&gt; bind
    ///   .Column(t =&gt; t.Date, "Transaction Date")
    ///   .Column(t =&gt; t.Type, "Transaction Type"))
    /// </code>
    /// </summary>
    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => TypedRows((bind ?? throw new ArgumentNullException(nameof(bind)))(new TableBinding<T>())
        ?? throw new ArgumentException("The binding lambda returned null.", nameof(bind)));

    /// <summary>
    /// A table whose record projection is written against <em>this file's</em> captions:
    /// <paramref name="headerRows"/> rows are read as the header, the captions they carry are
    /// handed to <paramref name="eachRow"/>, and the projection it returns is applied to every body
    /// row.
    /// <code>
    /// Table(headerRows: 1, eachRow: captions =&gt; Overlay(o =&gt; new Allocation(
    ///   Account: o.Next(Text().Right(captions["Account"])),
    ///   Weight:  o.Next(Decimal().OrBlank().Right(captions["Weight"])))))
    /// </code>
    /// <para>
    /// Two arrows, two moments. The bind runs <em>once per application of the table</em> — a table
    /// inside a repeat binds once per occurrence, each with its own header — after the header is
    /// read and before any body row — and builds a description; that description is then applied to
    /// each row by the engine, exactly as a row handed to
    /// <see cref="Table{T}(int, IProjection{T}, string)"/> is. Captions locate columns once and
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
    /// header cells involved (see <see cref="CaptionMap"/>). A bind that reaches a value to decide
    /// what to declare — <c>captions.Has("Fee") ? a : b</c> — is expressible because the API cannot
    /// prevent it, discouraged, and nothing here is added to encourage it.
    /// </para>
    /// <para>
    /// A hoisted bind is a factory rather than a value —
    /// <c>static IProjection&lt;T&gt; AllocationRow(CaptionMap captions) =&gt; …</c> — with the
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
    public static IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<CaptionMap, IProjection<T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
    {
      if (eachRow is null)
        throw new ArgumentNullException(nameof(eachRow));

      var site = UseSite.From(declared, null);
      var rows = ValidateBindHeaderRows(headerRows);

      // The bind runs inside the table's own Project, so a bind that throws is wrapped exactly as
      // any other user code the engine calls: ProjectionEngine classifies it, and a broken read —
      // an IO failure, a null bug — is a fault no tolerance boundary can absorb.
      return new TableProjection<IReadOnlyList<T>>(
        rows,
        table => ProjectBands(table, BoundRow(table, eachRow), site, bandHeight: 1),
        TablePlacement(),
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
    /// the rung above, <see cref="Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>, and
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
    public static IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
    {
      if (eachRow is null)
        throw new ArgumentNullException(nameof(eachRow));

      var site = UseSite.From(declared, null);
      var rows = ValidateHeaderRows(headerRows);

      // bandHeight: one row per record, which is what a table is until it is told otherwise. The
      // walk beneath this counts bands rather than rows, so slicing taller records is this argument
      // and nothing else.
      return new TableProjection<IReadOnlyList<T>>(
        rows,
        table => ProjectBands(table, eachRow, site, bandHeight: 1),
        TablePlacement(),
        "Table",
        eachRow);
    }

    private static IProjection<IReadOnlyList<T>> TypedRows<T>(TableBinding<T>? binding)
    {
      var plan = RowBinding<T>.Create(binding);

      return new TableProjection<IReadOnlyList<T>>(
        1,
        table => BindRows(table, plan),
        TablePlacement(),
        $"Table<{typeof(T).Name}>");
    }

    /// <summary>
    /// Every body row as a dictionary keyed by the column captions, with <see cref="CellValue"/>s
    /// for values — kinds and blankness survive, because this is an exploratory reader and not a
    /// stringifier. Keys are matched by <see cref="CaptionComparer"/>, so
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
    public static IProjection<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Table()
      => new TableProjection<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>>(
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
    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project) => Table(1, project);

    /// <inheritdoc cref="Table{T}(Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project)
    {
      if (project is null)
        throw new ArgumentNullException(nameof(project));

      return new TableProjection<IReadOnlyList<T>>(
        ValidateHeaderRows(headerRows),
        table => (IReadOnlyList<T>)table.StreamRows().Select(project).ToList(),
        TablePlacement(),
        "Table");
    }

    /// <summary>
    /// A table with one header row, read as a whole: past any blank rows, then rows and columns
    /// while they carry values. Column names come from the header, so rows can be read by name as
    /// well as by index. For the table that does not decompose row by row.
    /// </summary>
    public static IProjection<T> Table<T>(Func<TableView, T> project) => Table(1, project);

    /// <summary>
    /// A table with <paramref name="headerRows"/> header rows, which must be 0 or 1 — multi-row
    /// headers are not supported in this release. With 0, every row is a body row and columns can
    /// only be read by index.
    /// </summary>
    public static IProjection<T> Table<T>(int headerRows, Func<TableView, T> project)
      => new TableProjection<T>(ValidateHeaderRows(headerRows), project, TablePlacement(), "Table");

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
    /// Values are <see cref="CellValue"/>s and blank ones are <c>Blank</c>, not failures: the labels
    /// are the structure, the values are data. Like <c>Caption</c>, a block searches from the cursor
    /// and can jump, so inside a repeat the anchor wants hoisting onto the item as well.
    /// </para>
    /// </summary>
    public static IProjection<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields)
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
          if (CellMatching.LabelEquals(declared[earlier].Label)(CellValue.Of(declared[index].Label)))
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
      var pairs = new IProjection<CellValue>[declared.Length];

      for (var index = 0; index < declared.Length; index++)
        pairs[index] = new FieldProjection(declared[index].Label, Placement.Of(ExplicitArea(2, 1)));

      return new FlowProjection<IReadOnlyDictionary<string, CellValue>>(
        Orientation.Vertical,
        cursor =>
        {
          var values = new Dictionary<string, CellValue>(declared.Length, CaptionComparer.Default);

          // declared: null — without it the naming ladder would label every child with this
          // helper's own loop variable, an identifier the user never wrote.
          for (var index = 0; index < declared.Length; index++)
            values[declared[index].Label] = cursor.Next(pairs[index], declared: null);

          return values;
        },
        FieldsPlacement(declared[0].Label),
        description: "Fields");
    }

    // --- Repetition ---------------------------------------------------------------------------

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
    public static IProjection<IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Repeat(Orientation.Vertical, item, separatedBy, atLeast, declared);

    /// <summary>
    /// One item stacked rightwards as many times as the space supports; see
    /// <see cref="VerticalRepeat{T}"/> for <paramref name="separatedBy"/>,
    /// <paramref name="atLeast"/>, and how the item is named.
    /// </summary>
    public static IProjection<IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Repeat(Orientation.Horizontal, item, separatedBy, atLeast, declared);

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
    public static IProjection<T> Choice<T>(params IProjection<T>[] alternatives)
    {
      if (alternatives is null)
        throw new ArgumentNullException(nameof(alternatives));

      if (alternatives.Length < 2)
        throw new ArgumentException("A choice needs at least two alternatives.", nameof(alternatives));

      for (var index = 0; index < alternatives.Length; index++)
        if (alternatives[index] is null)
          throw new ArgumentException($"Alternative {index + 1} is null.", nameof(alternatives));

      return new ChoiceProjection<T>(alternatives, Placement.Default);
    }

    // --- Shared construction ------------------------------------------------------------------

    /// <summary>
    /// Applies a record projection to every band of a table's body, through the engine — so a
    /// record's own placement is resolved exactly once, where every other placement is, and a
    /// failure inside one carries the path and the cell.
    /// </summary>
    private static IReadOnlyList<T> ProjectBands<T>(TableView table, IProjection<T> eachRow, UseSite site, int bandHeight)
    {
      // Grown rather than pre-sized, as the other row rungs are: asking how many records there are
      // is the forcing question streaming exists to avoid.
      var records = new List<T>();

      foreach (var band in table.StreamBands(bandHeight))
      {
        // The index belongs to the table's own segment and the label to the record's, exactly as a
        // repeat labels its occurrences.
        var scope = band.Context.WithIndex(records.Count).WithUseSite(site);

        records.Add(ProjectionEngine.Apply(eachRow, band.Space, scope).Value);
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
    private static IProjection<T> BoundRow<T>(TableView table, Func<CaptionMap, IProjection<T>> eachRow)
      => eachRow(new CaptionMap(table))
        ?? throw table.Fault("the row bind returned null; it must return the projection that reads one record");

    private static IProjection<T> Strip<T>(Orientation orientation, Func<CellStrip, T> project, IAreaStrategy area, string description)
      => new StripProjection<T>(orientation, project, Placement.Of(area), description);

    private static IProjection<IReadOnlyList<T>> Repeat<T>(
      Orientation orientation,
      IProjection<T> item,
      IOffsetStrategy? separatedBy,
      int atLeast,
      string? declared)
    {
      if (atLeast < 0)
        throw new ArgumentOutOfRangeException(nameof(atLeast), atLeast, "A repeat cannot require a negative number of occurrences.");

      // A repeat has one item rather than an nth child, so there is no ordinal to fall back on: an
      // item that is not a plain identifier keeps its description, exactly as before.
      return new RepeatProjection<T>(item, separatedBy, orientation, atLeast, UseSite.From(declared, null), Placement.Default);
    }

    /// <summary>Validates a layout lambda where the caller's parameter name is what the user typed.</summary>
    private static Layout<T> NotNull<T>(Layout<T> build, string parameter) => build ?? throw new ArgumentNullException(parameter);

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

    private static Placement TablePlacement() => new Placement(OffsetStrategies.SkipBlankRows(), DiscoveredBlock());

    private static IAreaStrategy DiscoveredBlock() => RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue();

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
