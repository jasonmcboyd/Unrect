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
    /// The seek belongs to the item, outside the boundary: finding no further anchor is how the
    /// repetition knows to stop, so that one failure must not be tolerated. Everything after the
    /// anchor is inside the boundary, where a malformed section is swallowed and reported.
    /// <code>
    /// var item =
    ///   section.Select(s => (Section?)s)          // the section as it should be
    ///     .Else(Row(_ => (Section?)null))         // ... or just its label row, and a warning
    ///     .After(SeekRowContaining("Section"));   // ... starting at the next section label
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

    // --- Offset vocabulary --------------------------------------------------------------------

    /// <summary>Past however many leading rows are entirely blank.</summary>
    public static IOffsetStrategy BlankRows() => OffsetStrategies.SkipBlankRows();

    /// <summary>Past however many leading columns are entirely blank.</summary>
    public static IOffsetStrategy BlankColumns() => OffsetStrategies.SkipBlankColumns();

    /// <summary>Down <paramref name="count"/> rows, blank or not.</summary>
    public static IOffsetStrategy SkipRows(int count)
      => OffsetStrategies.ExplicitOffset(0, NotNegative(count, nameof(count)));

    /// <summary>Right <paramref name="count"/> columns, blank or not.</summary>
    public static IOffsetStrategy SkipColumns(int count)
      => OffsetStrategies.ExplicitOffset(NotNegative(count, nameof(count)), 0);

    /// <summary>
    /// Each offset applied to the space the one before it left, and summed — the way to combine
    /// offsets, since the modifiers replace rather than accumulate.
    /// </summary>
    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets) => OffsetStrategies.Then(offsets);

    // --- Anchoring vocabulary -------------------------------------------------------------------
    //
    // Seeks land the region ON the row or column they find, so "the row after the label" reads
    // Then(SeekRowContaining("Total"), SkipRows(1)). Finding nothing is a placement failure: a
    // strict shape reports which anchor was missing, and a Repeat stops looking for more sections.

    /// <summary>Down to the first row satisfying <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy SeekRow(Func<ISpace, int, bool> predicate) => OffsetStrategies.SeekRow(predicate);

    /// <summary>Down to the first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IOffsetStrategy SeekRowWhere(Func<CellValue, bool> anyCell) => OffsetStrategies.SeekRowWhere(anyCell);

    /// <summary>
    /// Down to the first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IOffsetStrategy SeekRowContaining(string text) => OffsetStrategies.SeekRowContaining(text);

    /// <summary>Right to the first column satisfying <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy SeekColumn(Func<ISpace, int, bool> predicate) => OffsetStrategies.SeekColumn(predicate);

    /// <summary>Right to the first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IOffsetStrategy SeekColumnWhere(Func<CellValue, bool> anyCell) => OffsetStrategies.SeekColumnWhere(anyCell);

    /// <summary>
    /// Right to the first column holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IOffsetStrategy SeekColumnContaining(string text) => OffsetStrategies.SeekColumnContaining(text);

    /// <summary>
    /// The rightmost <paramref name="width"/> columns. Normally spelled with <c>After</c>, which
    /// replaces: an anchor measured from the far edge discards wherever a movement left off.
    /// </summary>
    public static IOffsetStrategy FromRight(int width) => OffsetStrategies.FromRight(width);

    /// <summary>The bottom <paramref name="height"/> rows; see <see cref="FromRight"/>.</summary>
    public static IOffsetStrategy FromBottom(int height) => OffsetStrategies.FromBottom(height);

    // --- Landmark vocabulary --------------------------------------------------------------------
    //
    // Where a seek says where a shape starts, a landmark says where it ends: shape.Until(landmark).
    // They match on the same rules as the seeks, so a section can start at SeekRowContaining("A")
    // and end at RowContaining("B") without the two disagreeing about what a caption is.

    /// <summary>The first row satisfying <paramref name="predicate"/>.</summary>
    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate) => RowLandmarks.RowWhere(predicate);

    /// <summary>The first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell) => RowLandmarks.RowWithCell(anyCell);

    /// <summary>
    /// The first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IRowLandmark RowContaining(string text) => RowLandmarks.RowContaining(text);

    /// <summary>The first column satisfying <paramref name="predicate"/>.</summary>
    public static IColumnLandmark ColumnWhere(Func<ISpace, int, bool> predicate) => ColumnLandmarks.ColumnWhere(predicate);

    /// <summary>The first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IColumnLandmark ColumnWithCell(Func<CellValue, bool> anyCell) => ColumnLandmarks.ColumnWithCell(anyCell);

    /// <summary>
    /// The first column holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IColumnLandmark ColumnContaining(string text) => ColumnLandmarks.ColumnContaining(text);

    // --- Extent vocabulary ----------------------------------------------------------------------
    //
    // What `.Sized` takes, re-exported here for the same reason the offset vocabulary above is: a
    // shape declaration should need one import. Everything here returns the IAreaStrategy the
    // modifier wants, so no lifting is needed at the call site.

    /// <summary>The whole of the available space.</summary>
    public static IAreaStrategy WholeExtent() => AreaStrategies.MaxArea();

    /// <summary>Nothing — the identity extent, which a shape declares when it consumes no space.</summary>
    public static IAreaStrategy NoExtent() => AreaStrategies.MinArea();

    /// <summary>Exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IAreaStrategy Extent(int width, int height) => AreaStrategies.ExplicitArea(width, height);

    /// <summary>Full available width, and the leading rows that carry values.</summary>
    public static IAreaStrategy RowsWhileAnyValue() => SizeStrategies.RowsWhileAnyValue().ToAreaStrategy();

    /// <summary>
    /// Full available width, and as many leading rows as have at least one cell satisfying
    /// <paramref name="anyCell"/>.
    /// </summary>
    public static IAreaStrategy RowsWhileAny(Func<CellValue, bool> anyCell)
      => SizeStrategies.RowsWhileAny(anyCell).ToAreaStrategy();

    /// <summary>Full available height, and the leading columns that carry values.</summary>
    public static IAreaStrategy ColumnsWhileAnyValue() => SizeStrategies.ColumnsWhileAnyValue().ToAreaStrategy();

    /// <summary>
    /// Full available height, and as many leading columns as have at least one cell satisfying
    /// <paramref name="anyCell"/>.
    /// </summary>
    public static IAreaStrategy ColumnsWhileAny(Func<CellValue, bool> anyCell)
      => SizeStrategies.ColumnsWhileAny(anyCell).ToAreaStrategy();

    // The row/column selectors, for composing an extent from its two axes and for the leaf
    // overloads that take one — Row(AllColumns(), ...) is a full-width row.

    /// <summary>Exactly <paramref name="count"/> rows.</summary>
    public static IRowStrategy TakeRows(int count) => RowStrategies.TakeRows(count);

    /// <summary>Exactly <paramref name="count"/> columns.</summary>
    public static IColumnStrategy TakeColumns(int count) => ColumnStrategies.TakeColumns(count);

    /// <summary>Every row of the available space — the declared spelling of "the full height".</summary>
    public static IRowStrategy AllRows() => RowStrategies.AllRows();

    /// <summary>Every column of the available space — the declared spelling of "the full width".</summary>
    public static IColumnStrategy AllColumns() => ColumnStrategies.AllColumns();

    // The area-composing forms (rows.AllColumns(), columns.AllRows()) are deliberately NOT
    // re-exported: they are extension methods, and a script with `using Unrect.Strategies;` in
    // scope would see both copies and fail to resolve. Row(AllColumns(), ...) covers the case
    // that motivated them, using the leaf overload that already takes a column strategy.

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

    private static int NotNegative(int count, string parameter)
      => count >= 0 ? count : throw new ArgumentOutOfRangeException(parameter, count, "An offset cannot be negative.");

    private static Placement TablePlacement() => new Placement(OffsetStrategies.SkipBlankRows(), DiscoveredBlock());

    private static IAreaStrategy DiscoveredBlock() => RowStrategies.TakeRowsWhileAnyValue().TakeColumnsWhileAnyValue();

    private static int ValidateHeaderRows(int headerRows)
      => headerRows == 0 || headerRows == 1
        ? headerRows
        : throw new ArgumentOutOfRangeException(nameof(headerRows), headerRows, "A table has either 0 or 1 header rows; multi-row headers are not supported in this release.");

    private static IAreaStrategy RowsThenColumns(IRowStrategy rows, IColumnStrategy columns)
      => SelectArea(space =>
      {
        var height = rows.SelectRows(space);
        var width = columns.SelectColumns(space.GetSubspace(new Area(space.Area.Size.Width, height)));
        return new Size(width, height);
      });

    private static IAreaStrategy ColumnsThenRows(IColumnStrategy columns, IRowStrategy rows)
      => SelectArea(space =>
      {
        var width = columns.SelectColumns(space);
        var height = rows.SelectRows(space.GetSubspace(new Area(width, space.Area.Size.Height)));
        return new Size(width, height);
      });
  }
}
