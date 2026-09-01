using System;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>The maximal leading block of rows and columns that carry values.</summary>
    public static IShape<T> Cells<T>(Func<CellBlock, T> project)
      => new BlockShape<T>(project, Placement.Of(DiscoveredBlock()), "Cells");

    /// <summary>A block of exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IShape<T> Cells<T>(int width, int height, Func<CellBlock, T> project)
      => new BlockShape<T>(project, Placement.Of(ExplicitArea(width, height)), $"Cells({width}, {height})");

    /// <summary>A block extending as far as <paramref name="area"/> declares.</summary>
    public static IShape<T> Cells<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => new BlockShape<T>(
        project,
        Placement.Of(area ?? throw new ArgumentNullException(nameof(area))),
        "Cells");

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
    /// </summary>
    public static IShape<IReadOnlyList<T>> Repeat<T>(IShape<T> item, IOffsetStrategy? separatedBy = null, int atLeast = 0)
      => Repeat(Orientation.Vertical, item, separatedBy, atLeast);

    /// <summary>
    /// One item stacked rightwards as many times as the space supports; see <c>Repeat</c> for
    /// <paramref name="separatedBy"/> and <paramref name="atLeast"/>.
    /// </summary>
    public static IShape<IReadOnlyList<T>> RepeatHorizontal<T>(IShape<T> item, IOffsetStrategy? separatedBy = null, int atLeast = 0)
      => Repeat(Orientation.Horizontal, item, separatedBy, atLeast);

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

    // --- Shared construction ------------------------------------------------------------------

    private static IShape<T> Strip<T>(Orientation orientation, Func<CellStrip, T> project, IAreaStrategy area, string description)
      => new StripShape<T>(orientation, project, Placement.Of(area), description);

    private static IShape<T> Stack<T>(Orientation orientation, IShape[] children, Func<object?[], T> combine)
      => new StackShape<T>(orientation, children, combine, Placement.Default);

    private static IShape<T> Overlay<T>(IShape[] children, Func<object?[], T> combine)
      => new OverlayShape<T>(children, combine, Placement.Default);

    private static IShape<IReadOnlyList<T>> Repeat<T>(Orientation orientation, IShape<T> item, IOffsetStrategy? separatedBy, int atLeast)
    {
      if (atLeast < 0)
        throw new ArgumentOutOfRangeException(nameof(atLeast), atLeast, "A repeat cannot require a negative number of occurrences.");

      return new RepeatShape<T>(item, separatedBy, orientation, atLeast, Placement.Default);
    }

    /// <summary>Validates a stack child where the caller's parameter name is what the user typed.</summary>
    private static IShape NotNull(IShape child, string parameter) => child ?? throw new ArgumentNullException(parameter);

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
