using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A placement being declared, one stage at a time, with the projection still to come:
  /// <code>
  /// Below(mark)                 // the entry — anchors exist ONLY here
  ///   .Down(1)                  // movements compose onto the offset
  ///   .Sized(RowsWhileAnyValue())
  ///   .Heading("Transactions")  // what announces the section
  ///   .Table&lt;Transaction&gt;();    // the terminal: the subject closes the pipeline
  /// </code>
  /// <para>
  /// <b>It runs in the engine's order, which is also the sheet's.</b> Where the section starts, how
  /// big it is, what announces it, then what it reads — so reading the declaration left to right is
  /// reading the document top to bottom, and every stage is optional. The bare terminal is exactly
  /// the plain factory: silence is adjacency, and a projection with no declared extent sizes to its
  /// children or to its content as it always did.
  /// </para>
  /// <para>
  /// <b>What it refuses, it refuses at compile time.</b> An anchor is an entry rather than a stage,
  /// so a second one cannot be written; extents do not stack and a projection has one end, so neither
  /// can be declared twice; and nothing chains past a terminal, so a pipeline describes one subject.
  /// Where a refusal has a reason worth saying, the member is present as an
  /// <c>[Obsolete(error: true)]</c> stub that says it.
  /// </para>
  /// <para>
  /// <b>Why a class rather than an interface with extension terminals.</b> A terminal whose result
  /// type must be <em>stated</em> — <c>Table&lt;Transaction&gt;()</c>, the commonest of them all —
  /// cannot be an extension method on a stage generic in the space: C# infers a method's type
  /// arguments all or none, so the space would have to be written too. Instance members take the
  /// space from the receiver's type and leave the method generic only in what it reads, which is the
  /// same reason <see cref="ProjectionBuilders{TSpace}"/> is a generic class.
  /// </para>
  /// <para>
  /// A stage is a value: it holds what has been declared and nothing else, so one may be held in a
  /// local and closed twice.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the pipeline's declaration is written over.</typeparam>
  public abstract class PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected PlacementStage(Steps steps) => Steps = steps;

    internal Steps Steps { get; }

    /// <summary>What this pipeline has declared so far, for reading in a tooltip or a report.</summary>
    public override string ToString() => $"{GetType().Name} {Steps}";

    // --- Layouts ---------------------------------------------------------------------------------
    //
    // The three terminals that exist for INFERENCE: a lambda body cannot drive inference, so a flow
    // whose result type lives inside its own lambda has to be told what space it is over, and the
    // receiver's type is the telling.

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.VerticalFlow{T}(LayoutDeclaration{TSpace, T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="declare">The layout: its children declared with <c>Next</c>, closed with <c>Build</c>.</param>
    public IProjectionDefinition<TSpace, T> VerticalFlow<T>(LayoutDeclaration<TSpace, T> declare)
      => Close(ProjectionBuilders<TSpace>.VerticalFlow(declare));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.HorizontalFlow{T}(LayoutDeclaration{TSpace, T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="declare">The layout: its children declared with <c>Next</c>, closed with <c>Build</c>.</param>
    public IProjectionDefinition<TSpace, T> HorizontalFlow<T>(LayoutDeclaration<TSpace, T> declare)
      => Close(ProjectionBuilders<TSpace>.HorizontalFlow(declare));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Overlay{T}(LayoutDeclaration{TSpace, T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="declare">The layout: its children declared with <c>Next</c>, closed with <c>Build</c>.</param>
    public IProjectionDefinition<TSpace, T> Overlay<T>(LayoutDeclaration<TSpace, T> declare)
      => Close(ProjectionBuilders<TSpace>.Overlay(declare));

    // --- Repetition and alternation ----------------------------------------------------------------

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.VerticalRepeat{T}(IProjectionDefinition{TSpace, T}, IOffsetStrategy, int, string)"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.VerticalRepeat(item, separatedBy, atLeast, declared: declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.VerticalRepeat{T}(IProjectionDefinition{TSpace, T}, IOffsetStrategy{TSpace}, int, string)"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy<TSpace> separatedBy,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.VerticalRepeat(item, separatedBy, atLeast, declared: declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.HorizontalRepeat{T}(IProjectionDefinition{TSpace, T}, IOffsetStrategy, int, string)"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.HorizontalRepeat(item, separatedBy, atLeast, declared: declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.HorizontalRepeat{T}(IProjectionDefinition{TSpace, T}, IOffsetStrategy{TSpace}, int, string)"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy<TSpace> separatedBy,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.HorizontalRepeat(item, separatedBy, atLeast, declared: declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Choice{T}"/>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public IProjectionDefinition<TSpace, T> Choice<T>(params IProjectionDefinition<TSpace, T>[] alternatives)
      => Close(ProjectionBuilders<TSpace>.Choice(alternatives));

    // --- Tables ------------------------------------------------------------------------------------

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(int, Func{LabelMap, IProjectionDefinition{TSpace, T}}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjectionDefinition<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(int, IProjectionDefinition{TSpace, T}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjectionDefinition<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(ProjectionBuilders<TSpace>.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table()"/>
    public IProjectionDefinition<TSpace, IReadOnlyList<IReadOnlyDictionary<string, Point<TSpace>>>> Table()
      => Close<IReadOnlyList<IReadOnlyDictionary<string, Point<TSpace>>>>(ProjectionBuilders<TSpace>.Table());

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(Func{TableRow{TSpace}, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(Func<TableRow<TSpace>, T> project)
      => Close<IReadOnlyList<T>>(ProjectionBuilders<TSpace>.Table(project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(int, Func{TableRow{TSpace}, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow<TSpace>, T> project)
      => Close<IReadOnlyList<T>>(ProjectionBuilders<TSpace>.Table(headerRows, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(Func{TableView{TSpace}, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjectionDefinition<TSpace, T> Table<T>(Func<TableView<TSpace>, T> project) => Close<T>(ProjectionBuilders<TSpace>.Table(project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Table{T}(int, Func{TableView{TSpace}, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjectionDefinition<TSpace, T> Table<T>(int headerRows, Func<TableView<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Table(headerRows, project));

    // --- Leaves ------------------------------------------------------------------------------------

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Point()"/>
    public IProjectionDefinition<TSpace, Point<TSpace>> Point() => Close(ProjectionBuilders<TSpace>.Point());

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.AsText()"/>
    public IProjectionDefinition<TSpace, string> AsText() => Close(ProjectionBuilders<TSpace>.AsText());

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Text"/>
    public IProjectionDefinition<TSpace, string> Text() => Close(ProjectionBuilders<TSpace>.Text());

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Caption(string)"/>
    /// <param name="text">What the row must say.</param>
    public IProjectionDefinition<TSpace, string> Caption(string text) => Close<string>(ProjectionBuilders<TSpace>.Caption(text));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Row{T}(Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjectionDefinition<TSpace, T> Row<T>(Func<CellStrip<TSpace>, T> project) => Close<T>(ProjectionBuilders<TSpace>.Row(project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Row{T}(int, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="width">How many columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjectionDefinition<TSpace, T> Row<T>(int width, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Row(width, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Row{T}(IColumnStrategy, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="columns">The columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjectionDefinition<TSpace, T> Row<T>(IColumnStrategy columns, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Row(columns, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Row{T}(IColumnStrategy{TSpace}, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="columns">The columns the row spans. A rule demanding less is accepted as it is.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjectionDefinition<TSpace, T> Row<T>(IColumnStrategy<TSpace> columns, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Row(columns, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Column{T}(Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjectionDefinition<TSpace, T> Column<T>(Func<CellStrip<TSpace>, T> project) => Close<T>(ProjectionBuilders<TSpace>.Column(project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Column{T}(int, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="height">How many rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjectionDefinition<TSpace, T> Column<T>(int height, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Column(height, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Column{T}(IRowStrategy, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="rows">The rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjectionDefinition<TSpace, T> Column<T>(IRowStrategy rows, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Column(rows, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Column{T}(IRowStrategy{TSpace}, Func{CellStrip{TSpace}, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="rows">The rows the column spans. A rule demanding less is accepted as it is.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjectionDefinition<TSpace, T> Column<T>(IRowStrategy<TSpace> rows, Func<CellStrip<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Column(rows, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Range{T}(Func{CellBlock{TSpace}, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjectionDefinition<TSpace, T> Range<T>(Func<CellBlock<TSpace>, T> project) => Close<T>(ProjectionBuilders<TSpace>.Range(project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Range{T}(int, int, Func{CellBlock{TSpace}, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="width">How many columns the region spans.</param>
    /// <param name="height">How many rows the region spans.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjectionDefinition<TSpace, T> Range<T>(int width, int height, Func<CellBlock<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Range(width, height, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Range{T}(IAreaStrategy, Func{CellBlock{TSpace}, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="area">How far the region extends.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjectionDefinition<TSpace, T> Range<T>(IAreaStrategy area, Func<CellBlock<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Range(area, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Range{T}(IAreaStrategy{TSpace}, Func{CellBlock{TSpace}, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="area">How far the region extends. A rule demanding less is accepted as it is.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjectionDefinition<TSpace, T> Range<T>(IAreaStrategy<TSpace> area, Func<CellBlock<TSpace>, T> project)
      => Close<T>(ProjectionBuilders<TSpace>.Range(area, project));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Fields(Field[])"/>
    /// <param name="fields">The labelled pairs, in the order they sit on the sheet.</param>
    public IProjectionDefinition<TSpace, IReadOnlyDictionary<string, Point<TSpace>>> Fields(params Field[] fields)
      => Close(ProjectionBuilders<TSpace>.Fields(fields));

    // --- The hoisted-reuse terminal ------------------------------------------------------------------

    /// <summary>
    /// Places a projection declared elsewhere — <c>Below(mark).Of(transactions)</c>, the pipeline's
    /// spelling of "this section, there".
    /// <para>
    /// It is also the door for everything the terminals above do not spell: a backend's own leaf,
    /// anything a helper built. Declare it, then place it.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration to place.</param>
    public IProjectionDefinition<TSpace, T> Of<T>(IProjectionDefinition<TSpace, T> projection)
      => Close(projection ?? throw new ArgumentNullException(nameof(projection)));

    private IProjectionDefinition<TSpace, T> Close<T>(IProjectionDefinition<TSpace, T> projection) => Steps.ApplyTo(projection);
  }

  /// <summary>A pipeline whose extent is still open: a bound or an extent may still be declared, and
  /// the headings and the subject are still to come.</summary>
  /// <typeparam name="TSpace">The space the pipeline's declaration is written over.</typeparam>
  public abstract class UnboundedStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected UnboundedStage(Steps steps) : base(steps)
    {
    }

    /// <summary>
    /// What announces the section — <c>On(mark).Heading("Portfolio Income").Of(lines)</c>.
    /// <para>
    /// Chained headings read in document order and are one statement rather than nested sections;
    /// see <see cref="HeadingStage{TSpace}"/> for what a heading is and what it is not.
    /// </para>
    /// </summary>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));

    /// <summary>
    /// Ends the section just before the row <paramref name="landmark"/> matches, which it therefore
    /// never reads — the bound declared with the rest of the geometry, ahead of the subject.
    /// <para>
    /// The same declaration can be written after the subject instead
    /// (<c>Heading("…").Of(section).Until(next)</c>), which is the modifier this replays; they are
    /// one declaration spelled two ways, and which reads better depends on whether the landmark
    /// below the section is the point or an afterthought.
    /// </para>
    /// </summary>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(
        (landmark ?? throw new ArgumentNullException(nameof(landmark))).Landmark,
        orEnd)));

    /// <summary>
    /// Ends the section just before the column <paramref name="landmark"/> matches — the column twin
    /// of <see cref="Until(IRowLandmark, bool)"/>, spelled distinctly so the common row form never
    /// has to be disambiguated by the reader.
    /// </summary>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(
        (landmark ?? throw new ArgumentNullException(nameof(landmark))).Landmark,
        orEnd)));

    /// <summary>Refused: a pipeline's anchor is its entry, so a second one is not a declaration.</summary>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IRowLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IColumnLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> Below(IRowLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy<TSpace> offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }

  /// <summary>
  /// Where the section starts is declared; how big it is, what announces it and what it reads are
  /// still open. Movements compose onto the offset; anchors are refused, because an anchor is a root.
  /// </summary>
  /// <typeparam name="TSpace">The space the pipeline's declaration is written over.</typeparam>
  public sealed class OffsetStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    internal OffsetStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Down(int)"/>
    /// <param name="rows">How far down.</param>
    public OffsetStage<TSpace> Down(int rows) => new OffsetStage<TSpace>(Steps.Then(Step.Down(rows)));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.Right(int)"/>
    /// <param name="columns">How far right.</param>
    public OffsetStage<TSpace> Right(int columns) => new OffsetStage<TSpace>(Steps.Then(Step.Right(columns)));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.AfterBlankRows()"/>
    public OffsetStage<TSpace> AfterBlankRows() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankRows()));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.AfterBlankColumns()"/>
    public OffsetStage<TSpace> AfterBlankColumns() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankColumns()));

    /// <inheritdoc cref="ProjectionBuilders{TSpace}.SkipToFirstNonBlankCell()"/>
    public OffsetStage<TSpace> SkipToFirstNonBlankCell() => new OffsetStage<TSpace>(Steps.Then(Step.SkipToFirstNonBlankCell()));

    /// <summary>
    /// Declares the section's extent, replacing the derived one — after which the extent is consumed
    /// in full whether the section reads all of it or not. Extents do not stack, so a second
    /// <c>Sized</c> is refused: the pipeline goes on to <see cref="OffsetAndSizeStage{TSpace}"/>,
    /// where <c>Sized</c> is an <c>[Obsolete(error)]</c> stub.
    /// </summary>
    /// <param name="area">The extent.</param>
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => new OffsetAndSizeStage<TSpace>(Steps.Then(Step.Sized(area)));

    /// <inheritdoc cref="Sized(IAreaStrategy)"/>
    /// <param name="area">The extent. A rule demanding less is accepted as it is.</param>
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy<TSpace> area)
      => new OffsetAndSizeStage<TSpace>(Steps.Then(Step.Sized(
        (area ?? throw new ArgumentNullException(nameof(area))).Strategy)));

    /// <summary>
    /// The extent stated and left as it is — optional explicitness, never ceremony: the terminal
    /// sizes to its children, or to its content, whether this is written or not.
    /// </summary>
    public OffsetAndSizeStage<TSpace> SizedToChildren() => new OffsetAndSizeStage<TSpace>(Steps);
  }

  /// <summary>
  /// Where the section starts and how big it is are both declared: only a bound, the headings and
  /// the subject remain.
  /// </summary>
  /// <typeparam name="TSpace">The space the pipeline's declaration is written over.</typeparam>
  public sealed class OffsetAndSizeStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    internal OffsetAndSizeStage(Steps steps) : base(steps)
    {
    }

    /// <summary>Refused: extents do not stack, so a second one erases the first rather than narrowing it.</summary>
    /// <param name="area">The extent that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.ExtentsDoNotStack, error: true)]
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.ExtentsDoNotStack);

    /// <inheritdoc cref="Sized(IAreaStrategy)"/>
    /// <param name="area">The extent that would replace the pipeline's, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.ExtentsDoNotStack, error: true)]
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy<TSpace> area)
      => throw new NotSupportedException(PipelineRefusals.ExtentsDoNotStack);

    /// <summary>Refused: a movement belongs with the offset, ahead of the extent.</summary>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> AfterBlankRows() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> AfterBlankColumns() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> SkipToFirstNonBlankCell() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);
  }

  /// <summary>
  /// Bounded: the section's end is declared, so only its headings and its subject can follow. An
  /// extent after a bound would become the frame the landmark is sought in rather than a narrowing
  /// of it, and is refused for saying so.
  /// </summary>
  /// <typeparam name="TSpace">The space the pipeline's declaration is written over.</typeparam>
  public sealed class BoundStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    internal BoundStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage{TSpace}.Heading(string)"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));

    /// <summary>Refused: a projection has one end, and the axis comes with the landmark.</summary>
    /// <param name="landmark">The row that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row that would be the second end, demanding a space of its own.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    /// <remarks>
    /// The refusal is doubled exactly as the declaration is, so a demanding landmark is turned away
    /// by the reason rather than by the argument type: without this the compiler would say only that
    /// an <c>IRowLandmark&lt;TSpace&gt;</c> is not an <c>IRowLandmark</c>, which is true and useless.
    /// </remarks>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The column that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="Until(IRowLandmark{TSpace}, bool)"/>
    /// <param name="landmark">The column that would be the second end, demanding a space of its own.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <summary>Refused: an extent declared after a bound frames the search rather than narrowing it.</summary>
    /// <param name="area">The extent that would frame the search.</param>
    [Obsolete(PipelineRefusals.BoundFramesTheExtent, error: true)]
    public BoundStage<TSpace> Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.BoundFramesTheExtent);

    /// <inheritdoc cref="Sized(IAreaStrategy)"/>
    /// <param name="area">The extent that would frame the search, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.BoundFramesTheExtent, error: true)]
    public BoundStage<TSpace> Sized(IAreaStrategy<TSpace> area)
      => throw new NotSupportedException(PipelineRefusals.BoundFramesTheExtent);

    /// <summary>Refused: a movement belongs with the offset, ahead of the bound.</summary>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage<TSpace> Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage<TSpace> Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IRowLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IColumnLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> Below(IRowLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage{TSpace}.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's, demanding a space of its own.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> OffsetBy(IOffsetStrategy<TSpace> offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }
}
