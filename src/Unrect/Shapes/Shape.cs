using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

using static Unrect.Strategies.AreaStrategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// The shape vocabulary. <c>using static Unrect.Shapes.Shape;</c> is the only import a shape
  /// declaration needs: every leaf comes in a discovered, an explicit-count, and a strategy form,
  /// and the common offsets are re-exported here so the strategy layer stays optional.
  /// </summary>
  public static partial class Shape
  {
    // --- Leaves -------------------------------------------------------------------------------

    /// <summary>A single cell.</summary>
    public static IShape<T> Cell<T>(Func<CellValue, T> project)
      => new CellShape<T>(project, Placement.Of(ExplicitArea(1, 1)));

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
    // Under `using static Shape`, Decimal/Double/Boolean sit beside the framework types of the same
    // name. Type positions and the keyword aliases are unaffected (decimal.Parse, double.IsNaN);
    // only a static member reached through the FRAMEWORK TYPE NAME — Decimal.ToDouble(x) — stops
    // resolving. Write decimal.ToDouble-style calls through the keyword, which is the usual spelling.

    /// <summary>One cell holding text.</summary>
    public static IShape<string> Text()
      => Typed<string>(CellKind.Text, "Text", CellReading.ReadString);

    /// <summary>
    /// One cell holding a number, read as a <see cref="decimal"/> — the accessor that keeps a
    /// spreadsheet's exact decimal where the file carried one.
    /// </summary>
    public static IShape<decimal> Decimal()
      => Typed<decimal>(CellKind.Number, "Decimal", CellReading.ReadDecimal);

    /// <summary>
    /// One cell holding a whole number. A number that is really there but is fractional or out of
    /// range fails as a conversion, not as a kind — the cell is a <c>Number</c> either way.
    /// </summary>
    public static IShape<int> Integer()
      => Typed<int>(CellKind.Number, "Integer", CellReading.ReadInteger);

    /// <summary>One cell holding a number, read as a <see cref="double"/>.</summary>
    public static IShape<double> Double()
      => Typed<double>(CellKind.Number, "Double", CellReading.ReadDouble);

    /// <summary>
    /// One cell holding a date or time, verbatim. The time of day is kept: truncating is
    /// consumer-side (<c>Date().Select(d =&gt; d.Date)</c>), because a leaf that silently handed
    /// back less than the cell holds would be the only one in the vocabulary that did.
    /// </summary>
    public static IShape<DateTime> Date()
      => Typed<DateTime>(CellKind.Temporal, "Date", CellReading.ReadDateTime);

    /// <summary>One cell holding a boolean.</summary>
    public static IShape<bool> Boolean()
      => Typed<bool>(CellKind.Boolean, "Boolean", CellReading.ReadBoolean);

    private static IShape<T> Typed<T>(CellKind kind, string description, CellReader<T> read)
      => new TypedCellShape<T>(kind, description, read, Placement.Of(ExplicitArea(1, 1)));

    /// <summary>One row, as wide as the leading columns that carry values.</summary>
    public static IShape<T> Row<T>(Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue(), "Row");

    /// <summary>One row exactly <paramref name="width"/> columns wide.</summary>
    public static IShape<T> Row<T>(int width, Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, ExplicitArea(width, 1), $"Row({width})");

    /// <summary>One row, as wide as <paramref name="columns"/> selects.</summary>
    public static IShape<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project)
      => Strip(Orientation.Horizontal, project, RowsThenColumns(RowStrategies.TakeRows(1), columns), "Row");

    /// <summary>One column, as tall as the leading rows that carry values.</summary>
    public static IShape<T> Column<T>(Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ColumnStrategies.TakeColumns(1).TakeRowsWhileAnyValue(), "Column");

    /// <summary>One column exactly <paramref name="height"/> rows tall.</summary>
    public static IShape<T> Column<T>(int height, Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ExplicitArea(1, height), $"Column({height})");

    /// <summary>One column, as tall as <paramref name="rows"/> selects.</summary>
    public static IShape<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project)
      => Strip(Orientation.Vertical, project, ColumnsThenRows(ColumnStrategies.TakeColumns(1), rows), "Column");

    /// <summary>
    /// A rectangular region, read through a <see cref="CellBlock"/>: the maximal leading block of
    /// rows and columns that carry values.
    /// </summary>
    public static IShape<T> Range<T>(Func<CellBlock, T> project)
      => new BlockShape<T>(project, Placement.Of(DiscoveredBlock()), "Range");

    /// <summary>A region of exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IShape<T> Range<T>(int width, int height, Func<CellBlock, T> project)
      => new BlockShape<T>(project, Placement.Of(ExplicitArea(width, height)), $"Range({width}, {height})");

    /// <summary>A region extending as far as <paramref name="area"/> declares.</summary>
    public static IShape<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => new BlockShape<T>(
        project,
        Placement.Of(area ?? throw new ArgumentNullException(nameof(area))),
        "Range");

    /// <summary>
    /// The row that holds <paramref name="text"/>, as declared content: the shape finds that row,
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
    public static IShape<string> Caption(string text)
      => new CaptionShape(
        NotEmpty(text, nameof(text)),
        new Placement(OffsetStrategies.To(RowLandmarks.RowContaining(text)), FullRow()));

    // --- Tables -------------------------------------------------------------------------------

    /// <summary>
    /// A table with one header row: past any blank rows, then rows and columns while they carry
    /// values. Column names come from the header, so rows can be read by name as well as by index.
    /// </summary>
    public static IShape<T> Table<T>(Func<TableView, T> project) => Table(1, project);

    /// <summary>
    /// A table with <paramref name="headerRows"/> header rows, which must be 0 or 1 — multi-row
    /// headers are not supported in this release. With 0, every row is a body row and columns can
    /// only be read by index.
    /// </summary>
    public static IShape<T> Table<T>(int headerRows, Func<TableView, T> project)
      => new TableShape<T>(ValidateHeaderRows(headerRows), project, TablePlacement(), "Table");

    /// <summary>A table with one header row, projected row by row.</summary>
    public static IShape<IReadOnlyList<T>> TableRows<T>(Func<TableRow, T> project) => TableRows(1, project);

    /// <summary>
    /// A table with <paramref name="headerRows"/> header rows (0 or 1), projected row by row.
    /// </summary>
    public static IShape<IReadOnlyList<T>> TableRows<T>(int headerRows, Func<TableRow, T> project)
    {
      if (project is null)
        throw new ArgumentNullException(nameof(project));

      return new TableShape<IReadOnlyList<T>>(
        ValidateHeaderRows(headerRows),
        table => (IReadOnlyList<T>)table.Rows.Select(project).ToList(),
        TablePlacement(),
        "TableRows");
    }

    /// <summary>
    /// Every body row as a <typeparamref name="T"/>, with each member filled from the column whose
    /// caption matches its name and read as the member's own type declares.
    /// <para>
    /// Captions bind to members by <see cref="CaptionComparer"/> — case and whitespace are ignored,
    /// so <c>"Contribution ITD"</c> fills <c>ContributionItd</c> with nothing declared. The member's
    /// type chooses the kind to assert and the accessor to use, from the same closed set the typed
    /// leaves cover: <c>string</c>, <c>decimal</c>, <c>double</c>, <c>int</c>, <c>DateTime</c>,
    /// <c>bool</c>, <c>CellValue</c>, and the nullable forms. A nullable member tolerates a
    /// <em>blank</em> cell and still fails on the wrong kind — tolerating a blank says something
    /// about the data, tolerating a kind would say something about the format, and no real format
    /// has that.
    /// </para>
    /// <para>
    /// <typeparamref name="T"/> is built through its single parameterized constructor when it has
    /// one and no parameterless constructor (the positional-record case), otherwise through a
    /// parameterless constructor and its settable properties. Everything reflective is resolved
    /// once, when the shape is built; a bad type is an error at that point, not per file.
    /// </para>
    /// <para>
    /// Binding is strict in one direction: every member must find a column, and one that does not is
    /// a loud failure listing the table's captions. A column no member claims is fine — real reports
    /// carry columns a consumer does not want.
    /// </para>
    /// </summary>
    public static IShape<IReadOnlyList<T>> TableRows<T>() => TypedRows<T>(null);

    /// <summary>
    /// <see cref="TableRows{T}()"/> with per-member declarations: <c>Column</c> for a caption the
    /// comparer would not have found, <c>Ignore</c> for a member this table does not carry.
    /// <code>
    /// TableRows&lt;Transaction&gt;(bind =&gt; bind
    ///   .Column(t =&gt; t.Date, "Transaction Date")
    ///   .Column(t =&gt; t.Type, "Transaction Type"))
    /// </code>
    /// </summary>
    public static IShape<IReadOnlyList<T>> TableRows<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => TypedRows((bind ?? throw new ArgumentNullException(nameof(bind)))(new TableBinding<T>())
        ?? throw new ArgumentException("The binding lambda returned null.", nameof(bind)));

    private static IShape<IReadOnlyList<T>> TypedRows<T>(TableBinding<T>? binding)
    {
      var plan = RowBinding<T>.Create(binding);

      return new TableShape<IReadOnlyList<T>>(
        1,
        table => BindRows(table, plan),
        TablePlacement(),
        $"TableRows<{typeof(T).Name}>");
    }

    /// <summary>
    /// Every body row as a dictionary keyed by the column captions, with <see cref="CellValue"/>s
    /// for values — kinds and blankness survive, because this is an exploratory reader and not a
    /// stringifier. Keys are matched by <see cref="CaptionComparer"/>, so
    /// <c>row["contribution itd"]</c> and <c>row["ContributionITD"]</c> both find
    /// <c>"Contribution ITD"</c>.
    /// <para>
    /// The idiom: open an unfamiliar sheet with this, look at the captions and kinds, then graduate
    /// to <c>TableRows&lt;T&gt;()</c> once the columns are known.
    /// </para>
    /// <para>
    /// It promises one entry per column, so it is strict about the things that would break that
    /// promise: a column with no caption, and two captions that collide under the comparer, are
    /// both loud failures naming the cells involved.
    /// </para>
    /// </summary>
    public static IShape<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> TableRows()
      => new TableShape<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>>(
        1,
        DictionaryRows,
        TablePlacement(),
        "TableRows");

    // --- Labelled pairs -------------------------------------------------------------------------

    /// <summary>
    /// One labelled pair for a <see cref="Fields"/> block: the cell reading <paramref name="label"/>,
    /// and the value cell immediately to its right.
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
    /// than the same literal written twice. <c>.After(…)</c> replaces that anchor when a sheet holds
    /// two blocks with the same first label.
    /// </para>
    /// <para>
    /// Values are <see cref="CellValue"/>s and blank ones are <c>Blank</c>, not failures: the labels
    /// are the structure, the values are data. Like <c>Caption</c>, a block searches from the cursor
    /// and can jump, so inside a <c>Repeat</c> the anchor wants hoisting onto the item as well.
    /// </para>
    /// </summary>
    public static IShape<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields)
    {
      if (fields is null)
        throw new ArgumentNullException(nameof(fields));

      if (fields.Length == 0)
        throw new ArgumentException("A Fields block must declare at least one field.", nameof(fields));

      for (var index = 0; index < fields.Length; index++)
        if (fields[index] is null)
          throw new ArgumentException($"Field {index + 1} is null.", nameof(fields));

      // Cloned before validating, so what is checked is what the shape will hold.
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
      var pairs = new IShape<CellValue>[declared.Length];

      for (var index = 0; index < declared.Length; index++)
        pairs[index] = new FieldShape(declared[index].Label, Placement.Of(ExplicitArea(2, 1)));

      return new FlowShape<IReadOnlyDictionary<string, CellValue>>(
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
    /// <paramref name="separatedBy"/> is the offset <em>between</em> items and is never applied
    /// before the first — a leading gap belongs to the repeat itself
    /// (<c>Repeat(...).AfterBlankRows()</c>). It is also load-bearing for termination: when
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
    ///     .After(To(RowContaining("Section")));   // ... starting at the next section label
    ///
    /// var sections = Repeat(item).Select(all => all.Where(s => s is not null).ToList());
    ///
    /// var result = sections.MapWithDiagnostics(sheet);   // result.Diagnostics names the bad one
    /// </code>
    /// </example>
    /// </summary>
    /// <param name="item">The shape to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="item"/> argument, so an item
    /// hoisted into a local is called that in every path — <c>Repeat(investorDetail)</c> reads as
    /// <c>Repeat[2] -&gt; 'investorDetail'</c>. It is not a naming API; pass <c>.Named(…)</c> to
    /// choose a name, and note that an item written inline keeps its description instead.
    /// </param>
    public static IShape<IReadOnlyList<T>> Repeat<T>(
      IShape<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Repeat(Orientation.Vertical, item, separatedBy, atLeast, declared);

    /// <summary>
    /// One item stacked rightwards as many times as the space supports; see <c>Repeat</c> for
    /// <paramref name="separatedBy"/>, <paramref name="atLeast"/>, and how the item is named.
    /// </summary>
    public static IShape<IReadOnlyList<T>> RepeatHorizontal<T>(
      IShape<T> item,
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
    public static IShape<T> Choice<T>(params IShape<T>[] alternatives)
    {
      if (alternatives is null)
        throw new ArgumentNullException(nameof(alternatives));

      if (alternatives.Length < 2)
        throw new ArgumentException("A choice needs at least two alternatives.", nameof(alternatives));

      for (var index = 0; index < alternatives.Length; index++)
        if (alternatives[index] is null)
          throw new ArgumentException($"Alternative {index + 1} is null.", nameof(alternatives));

      return new ChoiceShape<T>(alternatives, Placement.Default);
    }

    // --- Shared construction ------------------------------------------------------------------

    private static IShape<T> Strip<T>(Orientation orientation, Func<CellStrip, T> project, IAreaStrategy area, string description)
      => new StripShape<T>(orientation, project, Placement.Of(area), description);

    private static IShape<IReadOnlyList<T>> Repeat<T>(
      Orientation orientation,
      IShape<T> item,
      IOffsetStrategy? separatedBy,
      int atLeast,
      string? declared)
    {
      if (atLeast < 0)
        throw new ArgumentOutOfRangeException(nameof(atLeast), atLeast, "A repeat cannot require a negative number of occurrences.");

      // A repeat has one item rather than an nth child, so there is no ordinal to fall back on: an
      // item that is not a plain identifier keeps its description, exactly as before.
      return new RepeatShape<T>(item, separatedBy, orientation, atLeast, UseSite.From(declared, null), Placement.Default);
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
  }
}
