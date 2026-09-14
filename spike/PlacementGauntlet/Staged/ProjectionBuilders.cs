using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Projections;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — <b>Entry C, the file-scoped vocabulary</b>. A
  /// closed generic static class imported once, which answers <typeparamref name="TSpace"/> at the
  /// top of the file, where C# already puts file-level bindings:
  /// <code>
  /// using static PlacementGauntlet.Staged.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var report = VerticalFlow(v =&gt; new Report(
  ///     Summary: v.Next(Table(headerRows: 1, eachRow: captions =&gt; Overlay(o =&gt; …))),
  ///     Details: v.Next(Under(Caption("IRR Details")).Of(details).Until(RowContaining(End)))));
  /// </code>
  /// Not one scope prefix, and the space is named once. Entry C is <b>Entry B with the receiver
  /// moved into the using block</b> — every composing member below forwards to
  /// <c>Projection.Over&lt;TSpace&gt;()</c>, and every placement entry to
  /// <c>Place.Over&lt;TSpace&gt;()</c>, so nothing here has semantics of its own to get wrong.
  /// <para>
  /// <b>Finding 1 — the split-type trick is not needed, because Entry C dissolves the problem it
  /// was invented for.</b> The all-or-none inference wall is about a <em>method's</em> type
  /// arguments: <c>Projection.Table&lt;ISpreadsheetSpace, Person&gt;()</c> makes you write the
  /// result because you wrote the space. Here the space is a <em>class</em> parameter, already
  /// closed by the using directive, so <c>Table&lt;Person&gt;()</c> states exactly one type argument
  /// and binds — measured, not assumed (<c>ScenarioC.SplitTypeTrick</c>). The nested spelling
  /// <c>Table.Of&lt;Person&gt;()</c> works too and is built beside this class as
  /// <see cref="SplitRungs{TSpace}"/>; what it costs is recorded there. The two cannot coexist under
  /// one name — CS0102, ledgered as (r) — so this class keeps the corpus's spelling and the trick
  /// stays a measured alternative.
  /// </para>
  /// <para>
  /// <b>Finding 2 — the ceiling: no extension method can live here (CS1106), so Entry C carries the
  /// PREFIX half of the vocabulary only.</b> Every postfix operator — <c>.Until</c>, <c>.Named</c>,
  /// <c>.Optional</c>, <c>.OrBlank</c>, <c>.Select</c>, <c>.Padded</c> — is an extension on
  /// <c>ProjectionExtensions</c> and is reached by the ordinary namespace import
  /// <c>using Unrect.Projections;</c>. That import is benign (a namespace import publishes no
  /// simple names, so it cannot collide with this class) but it is real: <b>"zero prefixes" is not
  /// "one import"</b>. The floor for an Entry C file is two usings plus this one, and under the
  /// geography law that is a clean split rather than a leak — what spells before the subject is
  /// imported here, what spells after it comes from the extensions.
  /// </para>
  /// <para>
  /// <b>Finding 3 — the completeness obligation is a wall of forwarders, and it is finite.</b>
  /// Because the file cannot also <c>using static Projection</c> (CS0121 on every shared name,
  /// ledgered as (q)), everything a declaration says must be reachable from here. That is the
  /// whole of <c>Projection</c> — the count is in the section headers below — re-exported at one
  /// line each. Space-indifferent members are re-exported at their PLAIN type on purpose:
  /// <c>Text()</c> stays an <c>IProjection&lt;string&gt;</c> so that a piece hoisted out of a scoped
  /// file keeps the weakest demand it can, and variance raises it at the use site with nothing said.
  /// Only the members that <em>take</em> a projection — the layouts, the repeats, <c>Choice</c>, and
  /// the two composing <c>Table</c> rungs — are scoped, which is the same eight-member rule the
  /// scope was built on.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    // ============================================================================================
    // The scoped half — the six members that take projections, forwarded to Entry B (7 with the
    // second Table rung). Everything below this section is space-indifferent and plain.
    // ============================================================================================

    private static ProjectionScope<TSpace> Scope => Projection.Over<TSpace>();

    public static IProjection<TSpace, T> VerticalFlow<T>(Layout<TSpace, T> build) => Scope.VerticalFlow(build);

    public static IProjection<TSpace, T> HorizontalFlow<T>(Layout<TSpace, T> build) => Scope.HorizontalFlow(build);

    public static IProjection<TSpace, T> Overlay<T>(Layout<TSpace, T> build) => Scope.Overlay(build);

    public static IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Scope.VerticalRepeat(item, separatedBy, atLeast, declared: declared);

    public static IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Scope.HorizontalRepeat(item, separatedBy, atLeast, declared: declared);

    public static IProjection<TSpace, T> Choice<T>(params IProjection<TSpace, T>[] alternatives)
      => Scope.Choice(alternatives);

    // ============================================================================================
    // Tables — the whole ladder. Rungs 1, 2 and 5 demand nothing, so they stay plain; the two slot
    // rungs take a projection and are scoped. `Table<Person>()` here is the split-type finding:
    // one type argument stated, the space already answered by the using directive.
    // ============================================================================================

    public static IProjection<IReadOnlyList<T>> Table<T>() => Projection.Table<T>();

    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Projection.Table(bind);

    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Scope.Table(headerRows, eachRow, declared);

    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Scope.Table(headerRows, eachRow, declared);

    public static IProjection<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Table() => Projection.Table();

    public static IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project) => Projection.Table(project);

    public static IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project)
      => Projection.Table(headerRows, project);

    public static IProjection<T> Table<T>(Func<TableView, T> project) => Projection.Table(project);

    public static IProjection<T> Table<T>(int headerRows, Func<TableView, T> project)
      => Projection.Table(headerRows, project);

    // ============================================================================================
    // Leaves — 20 members, plain, so a hoisted piece keeps the weakest demand.
    // ============================================================================================

    public static IProjection<T> Cell<T>(Func<CellValue, T> project) => Projection.Cell(project);

    public static IProjection<string> Text() => Projection.Text();

    public static IProjection<decimal> Decimal() => Projection.Decimal();

    public static IProjection<int> Integer() => Projection.Integer();

    public static IProjection<double> Double() => Projection.Double();

    public static IProjection<DateTime> Date() => Projection.Date();

    public static IProjection<bool> Boolean() => Projection.Boolean();

    public static IProjection<string> Caption(string text) => Projection.Caption(text);

    public static IProjection<T> Row<T>(Func<CellStrip, T> project) => Projection.Row(project);

    public static IProjection<T> Row<T>(int width, Func<CellStrip, T> project) => Projection.Row(width, project);

    public static IProjection<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project) => Projection.Row(columns, project);

    public static IProjection<T> Column<T>(Func<CellStrip, T> project) => Projection.Column(project);

    public static IProjection<T> Column<T>(int height, Func<CellStrip, T> project) => Projection.Column(height, project);

    public static IProjection<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project) => Projection.Column(rows, project);

    public static IProjection<T> Range<T>(Func<CellBlock, T> project) => Projection.Range(project);

    public static IProjection<T> Range<T>(int width, int height, Func<CellBlock, T> project) => Projection.Range(width, height, project);

    public static IProjection<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project) => Projection.Range(area, project);

    public static Field Field(string label) => Projection.Field(label);

    public static IProjection<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields) => Projection.Fields(fields);

    // ============================================================================================
    // Matchers — 6 members. A matcher only locates; the modifier or the entry decides what absence
    // means, which is why these are plain values rather than stages.
    // ============================================================================================

    public static IRowLandmark RowWhere(Func<ISpace, int, bool> predicate) => Projection.RowWhere(predicate);

    public static IRowLandmark RowWithCell(Func<CellValue, bool> anyCell) => Projection.RowWithCell(anyCell);

    public static IRowLandmark RowContaining(string text) => Projection.RowContaining(text);

    public static IColumnLandmark ColumnWhere(Func<ISpace, int, bool> predicate) => Projection.ColumnWhere(predicate);

    public static IColumnLandmark ColumnWithCell(Func<CellValue, bool> anyCell) => Projection.ColumnWithCell(anyCell);

    public static IColumnLandmark ColumnContaining(string text) => Projection.ColumnContaining(text);

    // ============================================================================================
    // Offsets, extents and the row/column strategies — 18 members, verbatim re-exports.
    // ============================================================================================

    public static IOffsetStrategy BlankRows() => Projection.BlankRows();

    public static IOffsetStrategy BlankColumns() => Projection.BlankColumns();

    public static IOffsetStrategy SkipRows(int count) => Projection.SkipRows(count);

    public static IOffsetStrategy SkipColumns(int count) => Projection.SkipColumns(count);

    public static IOffsetStrategy Then(params IOffsetStrategy[] offsets) => Projection.Then(offsets);

    public static IOffsetStrategy FromRight(int width) => Projection.FromRight(width);

    public static IOffsetStrategy FromBottom(int height) => Projection.FromBottom(height);

    public static IAreaStrategy WholeExtent() => Projection.WholeExtent();

    public static IAreaStrategy NoExtent() => Projection.NoExtent();

    public static IAreaStrategy Extent(int width, int height) => Projection.Extent(width, height);

    public static IAreaStrategy RowsWhileAnyValue() => Projection.RowsWhileAnyValue();

    public static IAreaStrategy RowsWhileAny(Func<CellValue, bool> anyCell) => Projection.RowsWhileAny(anyCell);

    public static IAreaStrategy ColumnsWhileAnyValue() => Projection.ColumnsWhileAnyValue();

    public static IAreaStrategy ColumnsWhileAny(Func<CellValue, bool> anyCell) => Projection.ColumnsWhileAny(anyCell);

    public static IRowStrategy TakeRows(int count) => Projection.TakeRows(count);

    public static IColumnStrategy TakeColumns(int count) => Projection.TakeColumns(count);

    public static IRowStrategy AllRows() => Projection.AllRows();

    public static IColumnStrategy AllColumns() => Projection.AllColumns();

    // ============================================================================================
    // The placement entries — 16 members, forwarded to Place.Over<TSpace>(). These are why an
    // Entry C file needs no `Place.Over<…>()` local either: the inverted pipeline's entries are
    // file-level names like everything else.
    //
    // The demanding-matcher overloads are here because a plain IRowLandmark and an
    // IRowLandmark<TSpace> are deliberately unrelated types — a demanding matcher would otherwise
    // be unreachable from a scoped file, which is the one place it is most at home.
    // ============================================================================================

    private static PlacementScope<TSpace> Placement => Place.Over<TSpace>();

    public static OffsetStage<TSpace> On(IRowLandmark landmark) => Placement.On(landmark);

    public static OffsetStage<TSpace> On(IColumnLandmark landmark) => Placement.On(landmark);

    public static OffsetStage<TSpace> On(IRowLandmark<TSpace> landmark) => Place.On(landmark);

    public static OffsetStage<TSpace> On(IColumnLandmark<TSpace> landmark) => Place.On(landmark);

    public static OffsetStage<TSpace> Below(IRowLandmark landmark) => Placement.Below(landmark);

    public static OffsetStage<TSpace> Below(IRowLandmark<TSpace> landmark) => Place.Below(landmark);

    public static OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => Placement.RightOf(landmark);

    public static OffsetStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark) => Place.RightOf(landmark);

    public static OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => Placement.OffsetBy(offset);

    public static OffsetStage<TSpace> Down(int rows) => Placement.Down(rows);

    public static OffsetStage<TSpace> Right(int columns) => Placement.Right(columns);

    public static OffsetStage<TSpace> AfterBlankRows() => Placement.AfterBlankRows();

    public static OffsetStage<TSpace> AfterBlankColumns() => Placement.AfterBlankColumns();

    public static OffsetStage<TSpace> SkipEmptyRowsAndColumns() => Placement.SkipEmptyRowsAndColumns();

    public static OffsetStage<TSpace> Offset() => Placement.Offset();

    public static OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area) => Placement.Sized(area);

    public static OffsetAndSizeStage<TSpace> SizedToChildren() => Placement.SizedToChildren();

    /// <inheritdoc cref="UnboundedStage.Under(IProjection{string}[])"/>
    public static UnderStage<TSpace> Under(params IProjection<string>[] captions) => Placement.Under(captions);

    /// <inheritdoc cref="Place.Heading(string)"/>
    public static HeadingStage<TSpace> Heading(string text) => Placement.Heading(text);

    /// <summary>
    /// The bound as a leading stage, per ruling 1's canonical order.
    /// <para>
    /// <b>Added by the Heading trial, and the reason is a finding.</b> It was left out when this
    /// class was written, deliberately: the geography law had superseded the bound stage, so
    /// postfix <c>.Until</c> was the spelling and postfix operators arrive as extensions. Ruling 1's
    /// canonical order — bounds before headings — put it back in the grammar, and a zero-prefix file
    /// then could not spell it (CS0103). <b>The completeness obligation is not a one-time wall: the
    /// closed class inherits every unsettled position in the vocabulary, and an operator that moves
    /// moves here too.</b>
    /// </para>
    /// </summary>
    public static BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false) => Placement.Until(landmark, orEnd);
  }

  /// <summary>
  /// SPIKE — the owner's split-type trick, built and measured. A nested static class splits the two
  /// type parameters across the type dot: the space is captured by the closed enclosing class, and
  /// only the result is stated — <c>Table.Of&lt;Person&gt;()</c>.
  /// <para>
  /// <b>It works, and it is unnecessary.</b> Nested types DO come through a
  /// <c>using static</c> of a closed generic (measured — <c>ScenarioC.SplitTypeTrick</c> reads a
  /// document through it), but so does the plain method rung, because the wall the trick was
  /// invented to climb is about a method's type arguments and Entry C's space is a class's.
  /// </para>
  /// <para>
  /// <b>What it would cost to adopt it anyway,</b> measured rather than guessed:
  /// <list type="number">
  /// <item>
  /// <b>The whole family, not the one rung.</b> A class cannot hold both a nested type and a method
  /// of the same name (CS0102, ledgered as (r)), so adopting <c>Table.Of&lt;T&gt;()</c> forces
  /// <c>Table(headerRows:, eachRow:)</c>, <c>Table()</c> and the two lambda rungs to be renamed
  /// into the nested class too — five rungs re-spelled to split a type argument that did not need
  /// splitting.
  /// </item>
  /// <item>
  /// <b>It changes what a collision says.</b> Two closed imports collide on a method as CS0121
  /// at the call site and on a nested TYPE as CS0104 at the type name — a different diagnostic for
  /// the same mistake, and only one of them can be read (see the ledger).
  /// </item>
  /// </list>
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class SplitRungs<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The reflection rung, spelled through the type dot.</summary>
    public static class Table
    {
      public static IProjection<IReadOnlyList<T>> Of<T>() => Projection.Table<T>();

      /// <summary>A slot rung, to show that the whole family has to follow the one that split.</summary>
      public static IProjection<TSpace, IReadOnlyList<T>> Rows<T>(
        int headerRows,
        Func<LabelMap, IProjection<TSpace, T>> eachRow,
        [CallerArgumentExpression("eachRow")] string? declared = null)
        => Projection.Over<TSpace>().Table(headerRows, eachRow, declared);
    }
  }
}
