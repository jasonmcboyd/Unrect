using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// The strategy layer's vocabulary, re-exported so <c>using static Unrect.Shapes.Shape;</c> really
  /// is the only import a declaration needs. Each of these forwards and adds nothing; where a name
  /// reads better in a declaration than it does in the strategy layer, it is renamed here.
  /// </summary>
  public static partial class Shape
  {
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
    /// offsets, since the modifiers replace rather than accumulate. Reached through
    /// <c>.OffsetBy(...)</c>, the shape layer's one door onto the strategy calculus.
    /// </summary>
    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets) => OffsetStrategies.Then(offsets);

    // --- Anchoring to the far edge --------------------------------------------------------------

    /// <summary>
    /// The rightmost <paramref name="width"/> columns. Normally spelled with <c>.OffsetBy</c>, which
    /// replaces: an anchor measured from the far edge discards wherever a movement left off.
    /// </summary>
    public static IOffsetStrategy FromRight(int width) => OffsetStrategies.FromRight(width);

    /// <summary>The bottom <paramref name="height"/> rows; see <see cref="FromRight"/>.</summary>
    public static IOffsetStrategy FromBottom(int height) => OffsetStrategies.FromBottom(height);

    // --- Matchers -------------------------------------------------------------------------------
    //
    // One family, three shapes of question, both axes. A matcher only locates content and reports
    // absence; what absence means belongs to the modifier that takes it — .On lands a shape ON the
    // match so it owns that row, .Below (.RightOf) one beyond, and .Until bounds a shape by one.
    // Because a section can start at .On(RowContaining("A")) and end at .Until(RowContaining("B"))
    // through the same matcher, the two cannot disagree about what a caption is.
    //
    // The lifts those modifiers are built on — OffsetStrategies.To/Past — are deliberately NOT
    // re-exported. At shape level a landmark is placed by a modifier that names its own relation;
    // a raw offset strategy over a landmark is an escape hatch, and it is spelled like one:
    // .OffsetBy(OffsetStrategies.To(...)). One position the modifiers cannot reach: a repeat's
    // separatedBy: takes an IOffsetStrategy, not a shape, so a landmark-anchored separator is
    // spelled with the raw lift there too.

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
  }
}
