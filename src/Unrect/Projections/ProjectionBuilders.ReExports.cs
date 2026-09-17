using System;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The strategy layer's vocabulary, re-exported so the one <c>using static</c> at the top of a
  /// file really is the only import a declaration needs. Each of these forwards and adds nothing;
  /// where a name reads better in a declaration than it does in the strategy layer, it is renamed
  /// here.
  /// <para>
  /// <b>A predicate written here reads a <see cref="Point{TSpace}"/> or a
  /// <see cref="Plane{TSpace}"/> over this file's space</b>, so it can ask a backend's own question —
  /// a cell's kind, its value — as well as the four questions every space answers, and what it hands
  /// back carries that demand in its type. Nothing is annotated to get it: the space is the one named
  /// in the import. A rule over a less demanding space flows in as it is, and the canonical spelling
  /// is subsumed rather than replaced, since a point over any space still answers
  /// <c>IsBlank</c>/<c>HasValue</c>/<c>IsText</c>/<c>AsText</c>.
  /// </para>
  /// <para>
  /// The erased factories in <c>Unrect.Strategies</c> remain the calculus — what a helper, a test or
  /// a composition writes when it has no space to name. Unwrap a rule from here to meet one with
  /// <c>.Strategy</c>, or <c>.Landmark</c> for a matcher.
  /// </para>
  /// </summary>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
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
    /// <c>.OffsetBy(...)</c>, the projection layer's one door onto the strategy calculus.
    /// </summary>
    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets) => OffsetStrategies.Then(offsets);

    // --- Anchoring to the far edge --------------------------------------------------------------

    /// <summary>
    /// The rightmost <paramref name="width"/> columns. Normally spelled with <c>.OffsetBy</c>,
    /// which replaces: an anchor measured from the far edge discards wherever a movement left off.
    /// </summary>
    public static IOffsetStrategy FromRight(int width) => OffsetStrategies.FromRight(width);

    /// <summary>The bottom <paramref name="height"/> rows; see <see cref="FromRight"/>.</summary>
    public static IOffsetStrategy FromBottom(int height) => OffsetStrategies.FromBottom(height);

    // --- Matchers -------------------------------------------------------------------------------
    //
    // One family, three shapes of question, both axes. A matcher only locates content and reports
    // absence; what absence means belongs to the modifier that takes it — .On lands a projection ON the
    // match so it owns that row, .Below (.RightOf) one beyond, and .Until bounds a projection by one.
    // Because a section can start at .On(RowContaining("A")) and end at .Until(RowContaining("B"))
    // through the same matcher, the two cannot disagree about what a caption is.
    //
    // The lifts those modifiers are built on — OffsetStrategies.To/Past — are deliberately NOT
    // re-exported. At projection level a landmark is placed by a modifier that names its own relation;
    // a raw offset strategy over a landmark is an escape hatch, and it is spelled like one:
    // .OffsetBy(OffsetStrategies.To(...)). One position the modifiers cannot reach: a repeat's
    // separatedBy: takes an IOffsetStrategy, not a projection, so a landmark-anchored separator is
    // spelled with the raw lift there too.

    /// <summary>The first row satisfying <paramref name="predicate"/>.</summary>
    public static IRowLandmark<TSpace> RowWhere(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Row<TSpace>(RowLandmarks.RowWhere(TypedPredicates.Lower(predicate)));

    /// <summary>The first row with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IRowLandmark<TSpace> RowWithCell(Func<Point<TSpace>, bool> anyCell)
      => Demanding.Row<TSpace>(RowLandmarks.RowWithCell(TypedPredicates.Lower(anyCell)));

    /// <summary>
    /// The first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IRowLandmark RowContaining(string text) => RowLandmarks.RowContaining(text);

    /// <summary>
    /// The first row in which some cell <em>says</em> <paramref name="text"/> — the same whole-cell
    /// comparison as <see cref="RowContaining"/>, against every cell's rendering rather than against
    /// text cells alone, so a numeric 42, a date, a boolean and an error are all reachable.
    /// </summary>
    public static IRowLandmark RowSaying(string text) => RowLandmarks.RowSaying(text);

    /// <summary>The first column satisfying <paramref name="predicate"/>.</summary>
    public static IColumnLandmark<TSpace> ColumnWhere(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Column<TSpace>(ColumnLandmarks.ColumnWhere(TypedPredicates.Lower(predicate)));

    /// <summary>The first column with any cell satisfying <paramref name="anyCell"/>.</summary>
    public static IColumnLandmark<TSpace> ColumnWithCell(Func<Point<TSpace>, bool> anyCell)
      => Demanding.Column<TSpace>(ColumnLandmarks.ColumnWithCell(TypedPredicates.Lower(anyCell)));

    /// <summary>
    /// The first column holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively.
    /// </summary>
    public static IColumnLandmark ColumnContaining(string text) => ColumnLandmarks.ColumnContaining(text);

    /// <summary>The column twin of <see cref="RowSaying"/>, with the same rule.</summary>
    public static IColumnLandmark ColumnSaying(string text) => ColumnLandmarks.ColumnSaying(text);

    // --- Extent vocabulary ----------------------------------------------------------------------
    //
    // What `.Sized` takes, re-exported here for the same reason the offset vocabulary above is: a
    // projection declaration should need one import. Everything here returns an extent the modifier
    // takes as it is — the fixed ones an IAreaStrategy, the predicate-driven ones the same wearing
    // this file's space — so nothing is lifted at the call site either way.

    /// <summary>The whole of the available space.</summary>
    public static IAreaStrategy WholeExtent() => AreaStrategies.MaxArea();

    /// <summary>Nothing — the identity extent, which a projection declares when it consumes no space.</summary>
    public static IAreaStrategy NoExtent() => AreaStrategies.MinArea();

    /// <summary>Exactly <paramref name="width"/> by <paramref name="height"/> cells.</summary>
    public static IAreaStrategy Extent(int width, int height) => AreaStrategies.ExplicitArea(width, height);

    /// <summary>Full available width, and the leading rows that carry values.</summary>
    public static IAreaStrategy RowsWhileAnyValue() => SizeStrategies.RowsWhileAnyValue().ToAreaStrategy();

    /// <summary>
    /// Full available width, and as many leading rows as have at least one cell satisfying
    /// <paramref name="anyCell"/>.
    /// </summary>
    public static IAreaStrategy<TSpace> RowsWhileAny(Func<Point<TSpace>, bool> anyCell)
      => Demanding.Area<TSpace>(SizeStrategies.RowsWhileAny(TypedPredicates.Lower(anyCell)).ToAreaStrategy());

    /// <summary>Full available height, and the leading columns that carry values.</summary>
    public static IAreaStrategy ColumnsWhileAnyValue() => SizeStrategies.ColumnsWhileAnyValue().ToAreaStrategy();

    /// <summary>
    /// Full available height, and as many leading columns as have at least one cell satisfying
    /// <paramref name="anyCell"/>.
    /// </summary>
    public static IAreaStrategy<TSpace> ColumnsWhileAny(Func<Point<TSpace>, bool> anyCell)
      => Demanding.Area<TSpace>(SizeStrategies.ColumnsWhileAny(TypedPredicates.Lower(anyCell)).ToAreaStrategy());

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

    // --- The predicate-driven selectors ---------------------------------------------------------
    //
    // A bare Where/While asks about a whole row or column; a cell predicate is always marked as one
    // — WithCell, WhileAll, WhileAny. Both halves read this file's space.

    /// <summary>Leading rows for which <paramref name="predicate"/> holds, stopping before the first it does not.</summary>
    public static IRowStrategy<TSpace> TakeRowsWhile(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Rows<TSpace>(RowStrategies.TakeRowsWhile(TypedPredicates.Lower(predicate)));

    /// <summary>
    /// Leading rows while <paramref name="predicate"/> holds of the cell in <paramref name="column"/>
    /// — a band read off one label column.
    /// </summary>
    public static IRowStrategy<TSpace> TakeRowsWhile(int column, Func<Point<TSpace>, int, bool> predicate)
      => Demanding.Rows<TSpace>(RowStrategies.TakeRowsWhile(column, TypedPredicates.Lower(predicate)));

    /// <summary>
    /// Rows up to and including the first for which <paramref name="predicate"/> holds — the match is
    /// kept, where <see cref="TakeRowsWhile(Func{Plane{TSpace}, int, bool})"/> stops before it.
    /// </summary>
    public static IRowStrategy<TSpace> TakeRowsTo(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Rows<TSpace>(RowStrategies.TakeRowsTo(TypedPredicates.Lower(predicate)));

    /// <summary>Leading rows in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IRowStrategy<TSpace> TakeRowsWhileAll(Func<Point<TSpace>, bool> predicate)
      => Demanding.Rows<TSpace>(RowStrategies.TakeRowsWhileAll(TypedPredicates.Lower(predicate)));

    /// <summary>Leading rows in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IRowStrategy<TSpace> TakeRowsWhileAny(Func<Point<TSpace>, bool> predicate)
      => Demanding.Rows<TSpace>(RowStrategies.TakeRowsWhileAny(TypedPredicates.Lower(predicate)));

    /// <summary>Leading columns for which <paramref name="predicate"/> holds, stopping before the first it does not.</summary>
    public static IColumnStrategy<TSpace> TakeColumnsWhile(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Columns<TSpace>(ColumnStrategies.TakeColumnsWhile(TypedPredicates.Lower(predicate)));

    /// <summary>
    /// Leading columns while <paramref name="predicate"/> holds of the cell in <paramref name="row"/>
    /// — a band read off one label row.
    /// </summary>
    public static IColumnStrategy<TSpace> TakeColumnsWhile(int row, Func<Point<TSpace>, int, bool> predicate)
      => Demanding.Columns<TSpace>(ColumnStrategies.TakeColumnsWhile(row, TypedPredicates.Lower(predicate)));

    /// <summary>
    /// Columns up to and including the first for which <paramref name="predicate"/> holds — the
    /// match is kept, where <see cref="TakeColumnsWhile(Func{Plane{TSpace}, int, bool})"/> stops
    /// before it.
    /// </summary>
    public static IColumnStrategy<TSpace> TakeColumnsTo(Func<Plane<TSpace>, int, bool> predicate)
      => Demanding.Columns<TSpace>(ColumnStrategies.TakeColumnsTo(TypedPredicates.Lower(predicate)));

    /// <summary>Leading columns in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IColumnStrategy<TSpace> TakeColumnsWhileAll(Func<Point<TSpace>, bool> predicate)
      => Demanding.Columns<TSpace>(ColumnStrategies.TakeColumnsWhileAll(TypedPredicates.Lower(predicate)));

    /// <summary>Leading columns in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IColumnStrategy<TSpace> TakeColumnsWhileAny(Func<Point<TSpace>, bool> predicate)
      => Demanding.Columns<TSpace>(ColumnStrategies.TakeColumnsWhileAny(TypedPredicates.Lower(predicate)));

    /// <summary>Past the leading rows in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipRowsWhileAll(Func<Point<TSpace>, bool> predicate)
      => Demanding.Offset<TSpace>(OffsetStrategies.SkipRowsWhileAll(TypedPredicates.Lower(predicate)));

    /// <summary>Past the leading rows in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipRowsWhileAny(Func<Point<TSpace>, bool> predicate)
      => Demanding.Offset<TSpace>(OffsetStrategies.SkipRowsWhileAny(TypedPredicates.Lower(predicate)));

    /// <summary>Past the leading columns in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipColumnsWhileAll(Func<Point<TSpace>, bool> predicate)
      => Demanding.Offset<TSpace>(OffsetStrategies.SkipColumnsWhileAll(TypedPredicates.Lower(predicate)));

    /// <summary>Past the leading columns in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipColumnsWhileAny(Func<Point<TSpace>, bool> predicate)
      => Demanding.Offset<TSpace>(OffsetStrategies.SkipColumnsWhileAny(TypedPredicates.Lower(predicate)));

    // --- Measuring by hand, and joining the two axes ----------------------------------------------

    /// <summary>The extent <paramref name="selector"/> measures for itself.</summary>
    public static ISizeStrategy<TSpace> SelectSize(Func<Plane<TSpace>, Size> selector)
      => Demanding.Size<TSpace>(SizeStrategies.SelectSize(TypedPredicates.Lower(selector)));

    /// <summary>The rectangle <paramref name="selector"/> measures for itself.</summary>
    public static IAreaStrategy<TSpace> SelectArea(Func<Plane<TSpace>, Size> selector)
      => Demanding.Area<TSpace>(AreaStrategies.SelectArea(TypedPredicates.Lower(selector)));

    /// <summary>The origin <paramref name="selector"/> measures for itself.</summary>
    public static IOffsetStrategy<TSpace> SelectOffset(Func<Plane<TSpace>, Size> selector)
      => Demanding.Offset<TSpace>(OffsetStrategies.SelectOffset(TypedPredicates.Lower(selector)));

    /// <summary>
    /// <paramref name="rows"/> measured first, then <paramref name="columns"/> within them — where
    /// the two axes meet.
    /// <para>
    /// Each axis is taken as it comes, demanding or not, so a rule from this file pairs with one
    /// from the calculus and neither has to be unwrapped. The result demands what its arguments do:
    /// a pair of erased rules is an erased extent, and one demanding rule makes the extent demand
    /// too.
    /// </para>
    /// </summary>
    public static IAreaStrategy RowsThenColumns(IRowStrategy rows, IColumnStrategy columns)
      => AreaStrategies.RowsThenColumns(rows, columns);

    /// <inheritdoc cref="RowsThenColumns(IRowStrategy, IColumnStrategy)"/>
    public static IAreaStrategy<TSpace> RowsThenColumns(IRowStrategy<TSpace> rows, IColumnStrategy<TSpace> columns)
      => Demanding.Area<TSpace>(AreaStrategies.RowsThenColumns(Required(rows).Strategy, Required(columns).Strategy));

    /// <inheritdoc cref="RowsThenColumns(IRowStrategy, IColumnStrategy)"/>
    public static IAreaStrategy<TSpace> RowsThenColumns(IRowStrategy<TSpace> rows, IColumnStrategy columns)
      => Demanding.Area<TSpace>(AreaStrategies.RowsThenColumns(Required(rows).Strategy, columns));

    /// <inheritdoc cref="RowsThenColumns(IRowStrategy, IColumnStrategy)"/>
    public static IAreaStrategy<TSpace> RowsThenColumns(IRowStrategy rows, IColumnStrategy<TSpace> columns)
      => Demanding.Area<TSpace>(AreaStrategies.RowsThenColumns(rows, Required(columns).Strategy));

    /// <summary>
    /// <paramref name="columns"/> measured first, then <paramref name="rows"/> within them — the
    /// other order, and the one a column-led region wants. It pairs the two axes exactly as
    /// <see cref="RowsThenColumns(IRowStrategy, IColumnStrategy)"/> does.
    /// </summary>
    public static IAreaStrategy ColumnsThenRows(IColumnStrategy columns, IRowStrategy rows)
      => AreaStrategies.ColumnsThenRows(columns, rows);

    /// <inheritdoc cref="ColumnsThenRows(IColumnStrategy, IRowStrategy)"/>
    public static IAreaStrategy<TSpace> ColumnsThenRows(IColumnStrategy<TSpace> columns, IRowStrategy<TSpace> rows)
      => Demanding.Area<TSpace>(AreaStrategies.ColumnsThenRows(Required(columns).Strategy, Required(rows).Strategy));

    /// <inheritdoc cref="ColumnsThenRows(IColumnStrategy, IRowStrategy)"/>
    public static IAreaStrategy<TSpace> ColumnsThenRows(IColumnStrategy<TSpace> columns, IRowStrategy rows)
      => Demanding.Area<TSpace>(AreaStrategies.ColumnsThenRows(Required(columns).Strategy, rows));

    /// <inheritdoc cref="ColumnsThenRows(IColumnStrategy, IRowStrategy)"/>
    public static IAreaStrategy<TSpace> ColumnsThenRows(IColumnStrategy columns, IRowStrategy<TSpace> rows)
      => Demanding.Area<TSpace>(AreaStrategies.ColumnsThenRows(columns, Required(rows).Strategy));
  }
}
