using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The vocabulary with <typeparamref name="TSpace"/> answered once, at the top of a file — and
  /// every declaration below written with no prefix, no scope local and no type argument naming the
  /// space. <b>The file is the scope</b>, which is where C# already puts file-level bindings. Two
  /// closed imports in one file collide on every shared name, and that is the model rather than a
  /// limitation: for any two space types, either their difference matters to a declaration — and
  /// then no declaration serves both, so the file wants splitting — or it does not, and both
  /// declarations target the shared base one scope already covers. <b>Scope a file to what its
  /// declarations READ, not to what the file parses</b>: a workbook opened for formulas whose
  /// projections never ask for one is an <c>ISpace</c> file.
  /// <code>
  /// using Unrect.Projections;                                                      // the postfix half
  /// using Unrect.Spreadsheets;
  ///
  /// using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  /// using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var report = VerticalFlow(v =&gt; new Report(
  ///     Title: v.Next(Text()),
  ///     Rows:  v.Next(Table(headerRows: 1, eachRow: row))));
  /// </code>
  /// <para>
  /// The space is spelled in full in the import because a <c>using</c> directive is resolved
  /// without the other <c>using</c>s around it — the one place in the file where a namespace does
  /// not help, and the price of naming the space exactly once.
  /// </para>
  /// <para>
  /// <b>The completeness covenant.</b> A file importing this class cannot also
  /// <c>using static Projection</c> — every shared name would be ambiguous — so everything a
  /// declaration says has to be reachable from here, and an operator that moves in the vocabulary
  /// moves here too. What cannot follow is the postfix half: C# forbids extension methods in a
  /// generic static class, so <c>.Named</c>, <c>.Optional</c>, <c>.OrBlank</c>, <c>.Select</c>,
  /// <c>.Until</c> and the placement modifiers arrive through the ordinary
  /// <c>using Unrect.Projections;</c>. A namespace import publishes no simple names, so the two
  /// cannot collide, and the seam falls where a reader would draw it anyway: what spells before the
  /// subject is imported here, what spells after it is an extension.
  /// </para>
  /// <para>
  /// <b>Only the members that TAKE a projection are closed over
  /// <typeparamref name="TSpace"/></b> — the three layouts, the two composing <c>Table</c> rungs,
  /// the two repeats and <c>Choice</c>, the same eight the scope was built on. Everything else is
  /// indifferent to the space and is re-exported at its plain type on purpose: a leaf demands
  /// nothing, so it composes here by variance and keeps the weakest demand it can when it is
  /// hoisted out. It also keeps the postfix half whole — <c>OrBlank</c> is written against
  /// <c>IProjection&lt;T&gt;</c> and a leaf raised to <typeparamref name="TSpace"/> would be out of
  /// its reach.
  /// </para>
  /// <para>
  /// Nothing here has semantics of its own: every member is one line to
  /// <see cref="Projection"/> or to the <see cref="ProjectionScope{TSpace}"/> that
  /// <see cref="Projection.Over{TSpace}"/> opens, use-site names forwarded along with the
  /// arguments.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    private static ProjectionScope<TSpace> Scope => Projection.Over<TSpace>();

    // --- Layouts -------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.VerticalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IProjection<TSpace, T> VerticalFlow<T>(Layout<TSpace, T> build) => Scope.VerticalFlow(build);

    /// <inheritdoc cref="Projection.HorizontalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IProjection<TSpace, T> HorizontalFlow<T>(Layout<TSpace, T> build) => Scope.HorizontalFlow(build);

    /// <inheritdoc cref="Projection.Overlay{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IProjection<TSpace, T> Overlay<T>(Layout<TSpace, T> build) => Scope.Overlay(build);

    // --- Repetition and alternation ------------------------------------------------------------

    /// <inheritdoc cref="Projection.VerticalRepeat{T}"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Scope.VerticalRepeat(item, separatedBy, atLeast, declared);

    /// <inheritdoc cref="Projection.HorizontalRepeat{T}"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Scope.HorizontalRepeat(item, separatedBy, atLeast, declared);

    /// <inheritdoc cref="Projection.Choice{T}"/>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public static IProjection<TSpace, T> Choice<T>(params IProjection<TSpace, T>[] alternatives)
      => Scope.Choice(alternatives);

    // --- Tables — the whole ladder -------------------------------------------------------------
    //
    // Two rungs take a projection and are scoped; the other seven demand nothing and stay plain.
    // Table<Person>() states one type argument and binds: the space is a parameter of this CLASS,
    // already answered by the using directive, so the all-or-none inference wall — which is about a
    // METHOD's type arguments — never arises.

    /// <inheritdoc cref="Projection.Table{T}()"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    public static IProjection<IReadOnlyList<T>> Table<T>() => Projection.Table<T>();

    /// <inheritdoc cref="Projection.Table{T}(Func{TableBinding{T}, TableBinding{T}})"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Projection.Table(bind);

    /// <inheritdoc cref="Projection.Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<CaptionMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Scope.Table(headerRows, eachRow, declared);

    /// <inheritdoc cref="Projection.Table{T}(int, IProjection{T}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Scope.Table(headerRows, eachRow, declared);

    /// <inheritdoc cref="Projection.Table()"/>
    public static IProjection<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Table() => Projection.Table();

    /// <inheritdoc cref="Projection.Table{T}(Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each body row.</param>
    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project) => Projection.Table(project);

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project)
      => Projection.Table(headerRows, project);

    /// <inheritdoc cref="Projection.Table{T}(Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public static IProjection<T> Table<T>(Func<TableView, T> project) => Projection.Table(project);

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public static IProjection<T> Table<T>(int headerRows, Func<TableView, T> project)
      => Projection.Table(headerRows, project);

    // --- Leaves --------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.Cell{T}(Func{CellValue, T})"/>
    /// <typeparam name="T">What the cell reads.</typeparam>
    /// <param name="project">The reading applied to the cell.</param>
    public static IProjection<T> Cell<T>(Func<CellValue, T> project) => Projection.Cell(project);

    /// <inheritdoc cref="Projection.Text()"/>
    public static IProjection<string> Text() => Projection.Text();

    /// <inheritdoc cref="Projection.Decimal()"/>
    public static IProjection<decimal> Decimal() => Projection.Decimal();

    /// <inheritdoc cref="Projection.Integer()"/>
    public static IProjection<int> Integer() => Projection.Integer();

    /// <inheritdoc cref="Projection.Double()"/>
    public static IProjection<double> Double() => Projection.Double();

    /// <inheritdoc cref="Projection.Date()"/>
    public static IProjection<DateTime> Date() => Projection.Date();

    /// <inheritdoc cref="Projection.Boolean()"/>
    public static IProjection<bool> Boolean() => Projection.Boolean();

    /// <inheritdoc cref="Projection.Caption(string)"/>
    /// <param name="text">What the row must say.</param>
    public static IProjection<string> Caption(string text) => Projection.Caption(text);

    /// <inheritdoc cref="Projection.Row{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="project">The reading applied to the row's cells.</param>
    public static IProjection<T> Row<T>(Func<CellStrip, T> project) => Projection.Row(project);

    /// <inheritdoc cref="Projection.Row{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="width">How many columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public static IProjection<T> Row<T>(int width, Func<CellStrip, T> project) => Projection.Row(width, project);

    /// <inheritdoc cref="Projection.Row{T}(IColumnStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="columns">The columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public static IProjection<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project)
      => Projection.Row(columns, project);

    /// <inheritdoc cref="Projection.Column{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="project">The reading applied to the column's cells.</param>
    public static IProjection<T> Column<T>(Func<CellStrip, T> project) => Projection.Column(project);

    /// <inheritdoc cref="Projection.Column{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="height">How many rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public static IProjection<T> Column<T>(int height, Func<CellStrip, T> project) => Projection.Column(height, project);

    /// <inheritdoc cref="Projection.Column{T}(IRowStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="rows">The rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public static IProjection<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project)
      => Projection.Column(rows, project);

    /// <inheritdoc cref="Projection.Range{T}(Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="project">The reading applied to the region's cells.</param>
    public static IProjection<T> Range<T>(Func<CellBlock, T> project) => Projection.Range(project);

    /// <inheritdoc cref="Projection.Range{T}(int, int, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="width">How many columns the region spans.</param>
    /// <param name="height">How many rows the region spans.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public static IProjection<T> Range<T>(int width, int height, Func<CellBlock, T> project)
      => Projection.Range(width, height, project);

    /// <inheritdoc cref="Projection.Range{T}(IAreaStrategy, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="area">How far the region extends.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public static IProjection<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => Projection.Range(area, project);

    /// <inheritdoc cref="Projection.Field(string)"/>
    /// <param name="label">The label cell's text.</param>
    public static Field Field(string label) => Projection.Field(label);

    /// <inheritdoc cref="Projection.Fields(Field[])"/>
    /// <param name="fields">The labelled pairs, in the order they sit on the sheet.</param>
    public static IProjection<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields)
      => Projection.Fields(fields);

    // --- Matchers ------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.RowWhere(Func{ISpace, int, bool})"/>
    /// <param name="predicate">What makes a row the one.</param>
    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate) => Projection.RowWhere(predicate);

    /// <inheritdoc cref="Projection.RowWithCell(Func{CellValue, bool})"/>
    /// <param name="anyCell">What makes a cell the one.</param>
    public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell) => Projection.RowWithCell(anyCell);

    /// <inheritdoc cref="Projection.RowContaining(string)"/>
    /// <param name="text">The whole cell value to look for.</param>
    public static IRowLandmark RowContaining(string text) => Projection.RowContaining(text);

    /// <inheritdoc cref="Projection.ColumnWhere(Func{ISpace, int, bool})"/>
    /// <param name="predicate">What makes a column the one.</param>
    public static IColumnLandmark ColumnWhere(Func<ISpace, int, bool> predicate) => Projection.ColumnWhere(predicate);

    /// <inheritdoc cref="Projection.ColumnWithCell(Func{CellValue, bool})"/>
    /// <param name="anyCell">What makes a cell the one.</param>
    public static IColumnLandmark ColumnWithCell(Func<CellValue, bool> anyCell) => Projection.ColumnWithCell(anyCell);

    /// <inheritdoc cref="Projection.ColumnContaining(string)"/>
    /// <param name="text">The whole cell value to look for.</param>
    public static IColumnLandmark ColumnContaining(string text) => Projection.ColumnContaining(text);

    // --- Offsets -------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.BlankRows()"/>
    public static IOffsetStrategy BlankRows() => Projection.BlankRows();

    /// <inheritdoc cref="Projection.BlankColumns()"/>
    public static IOffsetStrategy BlankColumns() => Projection.BlankColumns();

    /// <inheritdoc cref="Projection.SkipRows(int)"/>
    /// <param name="count">How many rows to step over.</param>
    public static IOffsetStrategy SkipRows(int count) => Projection.SkipRows(count);

    /// <inheritdoc cref="Projection.SkipColumns(int)"/>
    /// <param name="count">How many columns to step over.</param>
    public static IOffsetStrategy SkipColumns(int count) => Projection.SkipColumns(count);

    /// <inheritdoc cref="Projection.Then(IOffsetStrategy[])"/>
    /// <param name="offsets">The offsets, each applied to the space the one before it left.</param>
    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets) => Projection.Then(offsets);

    /// <inheritdoc cref="Projection.FromRight(int)"/>
    /// <param name="width">How many columns of the far edge to anchor on.</param>
    public static IOffsetStrategy FromRight(int width) => Projection.FromRight(width);

    /// <inheritdoc cref="Projection.FromBottom(int)"/>
    /// <param name="height">How many rows of the bottom edge to anchor on.</param>
    public static IOffsetStrategy FromBottom(int height) => Projection.FromBottom(height);

    // --- Extents -------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.WholeExtent()"/>
    public static IAreaStrategy WholeExtent() => Projection.WholeExtent();

    /// <inheritdoc cref="Projection.NoExtent()"/>
    public static IAreaStrategy NoExtent() => Projection.NoExtent();

    /// <inheritdoc cref="Projection.Extent(int, int)"/>
    /// <param name="width">How many columns.</param>
    /// <param name="height">How many rows.</param>
    public static IAreaStrategy Extent(int width, int height) => Projection.Extent(width, height);

    /// <inheritdoc cref="Projection.RowsWhileAnyValue()"/>
    public static IAreaStrategy RowsWhileAnyValue() => Projection.RowsWhileAnyValue();

    /// <inheritdoc cref="Projection.RowsWhileAny(Func{CellValue, bool})"/>
    /// <param name="anyCell">What one cell of a row must satisfy for the row to be taken.</param>
    public static IAreaStrategy RowsWhileAny(Func<CellValue, bool> anyCell) => Projection.RowsWhileAny(anyCell);

    /// <inheritdoc cref="Projection.ColumnsWhileAnyValue()"/>
    public static IAreaStrategy ColumnsWhileAnyValue() => Projection.ColumnsWhileAnyValue();

    /// <inheritdoc cref="Projection.ColumnsWhileAny(Func{CellValue, bool})"/>
    /// <param name="anyCell">What one cell of a column must satisfy for the column to be taken.</param>
    public static IAreaStrategy ColumnsWhileAny(Func<CellValue, bool> anyCell) => Projection.ColumnsWhileAny(anyCell);

    /// <inheritdoc cref="Projection.TakeRows(int)"/>
    /// <param name="count">How many rows.</param>
    public static IRowStrategy TakeRows(int count) => Projection.TakeRows(count);

    /// <inheritdoc cref="Projection.TakeColumns(int)"/>
    /// <param name="count">How many columns.</param>
    public static IColumnStrategy TakeColumns(int count) => Projection.TakeColumns(count);

    /// <inheritdoc cref="Projection.AllRows()"/>
    public static IRowStrategy AllRows() => Projection.AllRows();

    /// <inheritdoc cref="Projection.AllColumns()"/>
    public static IColumnStrategy AllColumns() => Projection.AllColumns();

    // --- The placement pipeline's entries -------------------------------------------------------
    //
    // Closed over TSpace like the members that take a projection, and for the same reason: a
    // pipeline's stages carry the space to whichever terminal closes them, so a scoped file's
    // Below(mark).VerticalFlow(v => …) hands its lambda a cursor over the space the file named.

    /// <inheritdoc cref="Projection.On(IRowLandmark)"/>
    /// <param name="landmark">The row to sit on.</param>
    public static OffsetStage<TSpace> On(IRowLandmark landmark) => Scope.On(landmark);

    /// <inheritdoc cref="Projection.On(IColumnLandmark)"/>
    /// <param name="landmark">The column to sit on.</param>
    public static OffsetStage<TSpace> On(IColumnLandmark landmark) => Scope.On(landmark);

    /// <inheritdoc cref="Projection.On{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The row to sit on. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> On(IRowLandmark<TSpace> landmark) => Scope.On(landmark);

    /// <inheritdoc cref="Projection.On{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The column to sit on. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> On(IColumnLandmark<TSpace> landmark) => Scope.On(landmark);

    /// <inheritdoc cref="Projection.Below(IRowLandmark)"/>
    /// <param name="landmark">The row to sit below.</param>
    public static OffsetStage<TSpace> Below(IRowLandmark landmark) => Scope.Below(landmark);

    /// <inheritdoc cref="Projection.Below{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The row to sit below. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> Below(IRowLandmark<TSpace> landmark) => Scope.Below(landmark);

    /// <inheritdoc cref="Projection.RightOf(IColumnLandmark)"/>
    /// <param name="landmark">The column to sit right of.</param>
    public static OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => Scope.RightOf(landmark);

    /// <inheritdoc cref="Projection.RightOf{T}(IColumnLandmark{T})"/>
    /// <param name="landmark">The column to sit right of. A matcher demanding less is accepted as it is.</param>
    public static OffsetStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark) => Scope.RightOf(landmark);

    /// <inheritdoc cref="Projection.OffsetBy(IOffsetStrategy)"/>
    /// <param name="offset">Where the section starts.</param>
    public static OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => Scope.OffsetBy(offset);

    /// <inheritdoc cref="Projection.AfterBlankRows()"/>
    public static OffsetStage<TSpace> AfterBlankRows() => Scope.AfterBlankRows();

    /// <inheritdoc cref="Projection.AfterBlankColumns()"/>
    public static OffsetStage<TSpace> AfterBlankColumns() => Scope.AfterBlankColumns();

    /// <inheritdoc cref="Projection.SkipEmptyRowsAndColumns()"/>
    public static OffsetStage<TSpace> SkipEmptyRowsAndColumns() => Scope.SkipEmptyRowsAndColumns();

    /// <inheritdoc cref="Projection.Down(int)"/>
    /// <param name="rows">How far down.</param>
    public static OffsetStage<TSpace> Down(int rows) => Scope.Down(rows);

    /// <inheritdoc cref="Projection.Right(int)"/>
    /// <param name="columns">How far right.</param>
    public static OffsetStage<TSpace> Right(int columns) => Scope.Right(columns);

    /// <inheritdoc cref="Projection.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false) => Scope.Until(landmark, orEnd);

    /// <inheritdoc cref="Projection.Until{T}(IRowLandmark{T}, bool)"/>
    /// <param name="landmark">The row the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => Scope.Until(landmark, orEnd);

    /// <inheritdoc cref="Projection.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => Scope.UntilColumn(landmark, orEnd);

    /// <inheritdoc cref="Projection.UntilColumn{T}(IColumnLandmark{T}, bool)"/>
    /// <param name="landmark">The column the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => Scope.UntilColumn(landmark, orEnd);

    /// <inheritdoc cref="HeadingStage"/>
    /// <param name="text">What the heading row says.</param>
    public static HeadingStage<TSpace> Heading(string text) => Scope.Heading(text);
  }
}
