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
  /// today's factory: silence is adjacency, and a projection with no declared extent sizes to its
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
  /// same reason <see cref="ProjectionScope{TSpace}"/> exists.
  /// </para>
  /// <para>
  /// A stage is a value: it holds what has been declared and nothing else, so one may be held in a
  /// local and closed twice.
  /// </para>
  /// </summary>
  public abstract class PlacementStage
  {
    private protected PlacementStage(Steps steps) => Steps = steps;

    internal Steps Steps { get; }

    /// <summary>What this pipeline has declared so far, for reading in a tooltip or a report.</summary>
    public override string ToString() => $"{GetType().Name} {Steps}";

    // --- Layouts ---------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.VerticalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<T> VerticalFlow<T>(Layout<T> build) => Close(Projection.VerticalFlow(build));

    /// <inheritdoc cref="Projection.HorizontalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<T> HorizontalFlow<T>(Layout<T> build) => Close(Projection.HorizontalFlow(build));

    /// <inheritdoc cref="Projection.Overlay{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<T> Overlay<T>(Layout<T> build) => Close(Projection.Overlay(build));

    // --- Repetition and alternation ----------------------------------------------------------------

    /// <inheritdoc cref="Projection.VerticalRepeat{T}"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjection<IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.VerticalRepeat(item, separatedBy, atLeast, declared));

    /// <inheritdoc cref="Projection.HorizontalRepeat{T}"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjection<IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.HorizontalRepeat(item, separatedBy, atLeast, declared));

    /// <inheritdoc cref="Projection.Choice{T}"/>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public IProjection<T> Choice<T>(params IProjection<T>[] alternatives) => Close(Projection.Choice(alternatives));

    // --- Tables ------------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.Table{T}()"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    public IProjection<IReadOnlyList<T>> Table<T>() => Close(Projection.Table<T>());

    /// <inheritdoc cref="Projection.Table{T}(Func{TableBinding{T}, TableBinding{T}})"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Close(Projection.Table(bind));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{LabelMap, IProjection{T}}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjection<T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="Projection.Table{T}(int, IProjection{T}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="Projection.Table()"/>
    public IProjection<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Table() => Close(Projection.Table());

    /// <inheritdoc cref="Projection.Table{T}(Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project) => Close(Projection.Table(project));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjection<IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project)
      => Close(Projection.Table(headerRows, project));

    /// <inheritdoc cref="Projection.Table{T}(Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjection<T> Table<T>(Func<TableView, T> project) => Close(Projection.Table(project));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjection<T> Table<T>(int headerRows, Func<TableView, T> project)
      => Close(Projection.Table(headerRows, project));

    // --- Leaves ------------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.Cell{T}(Func{CellValue, T})"/>
    /// <typeparam name="T">What the cell reads.</typeparam>
    /// <param name="project">The reading applied to the cell.</param>
    public IProjection<T> Cell<T>(Func<CellValue, T> project) => Close(Projection.Cell(project));

    /// <inheritdoc cref="Projection.Text()"/>
    public IProjection<string> Text() => Close(Projection.Text());

    /// <inheritdoc cref="Projection.Decimal()"/>
    public IProjection<decimal> Decimal() => Close(Projection.Decimal());

    /// <inheritdoc cref="Projection.Integer()"/>
    public IProjection<int> Integer() => Close(Projection.Integer());

    /// <inheritdoc cref="Projection.Double()"/>
    public IProjection<double> Double() => Close(Projection.Double());

    /// <inheritdoc cref="Projection.Date()"/>
    public IProjection<DateTime> Date() => Close(Projection.Date());

    /// <inheritdoc cref="Projection.Boolean()"/>
    public IProjection<bool> Boolean() => Close(Projection.Boolean());

    /// <inheritdoc cref="Projection.Caption(string)"/>
    /// <param name="text">What the row must say.</param>
    public IProjection<string> Caption(string text) => Close(Projection.Caption(text));

    /// <inheritdoc cref="Projection.Row{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<T> Row<T>(Func<CellStrip, T> project) => Close(Projection.Row(project));

    /// <inheritdoc cref="Projection.Row{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="width">How many columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<T> Row<T>(int width, Func<CellStrip, T> project) => Close(Projection.Row(width, project));

    /// <inheritdoc cref="Projection.Row{T}(IColumnStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="columns">The columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project)
      => Close(Projection.Row(columns, project));

    /// <inheritdoc cref="Projection.Column{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<T> Column<T>(Func<CellStrip, T> project) => Close(Projection.Column(project));

    /// <inheritdoc cref="Projection.Column{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="height">How many rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<T> Column<T>(int height, Func<CellStrip, T> project) => Close(Projection.Column(height, project));

    /// <inheritdoc cref="Projection.Column{T}(IRowStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="rows">The rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project)
      => Close(Projection.Column(rows, project));

    /// <inheritdoc cref="Projection.Range{T}(Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<T> Range<T>(Func<CellBlock, T> project) => Close(Projection.Range(project));

    /// <inheritdoc cref="Projection.Range{T}(int, int, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="width">How many columns the region spans.</param>
    /// <param name="height">How many rows the region spans.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<T> Range<T>(int width, int height, Func<CellBlock, T> project)
      => Close(Projection.Range(width, height, project));

    /// <inheritdoc cref="Projection.Range{T}(IAreaStrategy, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="area">How far the region extends.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => Close(Projection.Range(area, project));

    /// <inheritdoc cref="Projection.Fields(Field[])"/>
    /// <param name="fields">The labelled pairs, in the order they sit on the sheet.</param>
    public IProjection<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields)
      => Close(Projection.Fields(fields));

    // --- The hoisted-reuse terminal ------------------------------------------------------------------

    /// <summary>
    /// Places a projection declared elsewhere — <c>Below(mark).Of(transactions)</c>, the pipeline's
    /// spelling of "this section, there".
    /// <para>
    /// It is also the door for everything the terminals above do not spell: a projection that demands
    /// a capability, a backend's own leaf, anything a helper built. Declare it, then place it.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration to place.</param>
    public IProjection<T> Of<T>(IProjection<T> projection)
      => Close(projection ?? throw new ArgumentNullException(nameof(projection)));

    /// <inheritdoc cref="Of{T}(IProjection{T})"/>
    /// <typeparam name="TSpace">What the projection demands of the space, carried out to the result.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration to place.</param>
    public IProjection<TSpace, T> Of<TSpace, T>(IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => Steps.ApplyTo(ProjectionExtensions.Plain(
        projection ?? throw new ArgumentNullException(nameof(projection))));

    private IProjection<T> Close<T>(IProjection<T> projection) => Steps.ApplyTo(projection);
  }

  /// <summary>
  /// A pipeline whose extent is still open: a bound or an extent may still be declared, and the
  /// headings and the subject are still to come.
  /// </summary>
  public abstract class UnboundedStage : PlacementStage
  {
    private protected UnboundedStage(Steps steps) : base(steps)
    {
    }

    /// <summary>
    /// What announces the section — <c>On(mark).Heading("Portfolio Income").Of(lines)</c>.
    /// <para>
    /// Chained headings read in document order and are one statement rather than nested sections;
    /// see <see cref="HeadingStage"/> for what a heading is and what it is not.
    /// </para>
    /// </summary>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage Heading(string text) => new HeadingStage(Steps, Headings.One(text));

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
    public BoundStage Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand the matcher raises, carried on to the terminal.</typeparam>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until<TSpace>(IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
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
    public BoundStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="UntilColumn(IColumnLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand the matcher raises, carried on to the terminal.</typeparam>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn<TSpace>(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(
        (landmark ?? throw new ArgumentNullException(nameof(landmark))).Landmark,
        orEnd)));

    /// <summary>Refused: a pipeline's anchor is its entry, so a second one is not a declaration.</summary>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }

  /// <summary>
  /// Where the section starts is declared; how big it is, what announces it and what it reads are
  /// still open. Movements compose onto the offset; anchors are refused, because an anchor is a root.
  /// </summary>
  public sealed class OffsetStage : UnboundedStage
  {
    internal OffsetStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="Projection.Down(int)"/>
    /// <param name="rows">How far down.</param>
    public OffsetStage Down(int rows) => new OffsetStage(Steps.Then(Step.Down(rows)));

    /// <inheritdoc cref="Projection.Right(int)"/>
    /// <param name="columns">How far right.</param>
    public OffsetStage Right(int columns) => new OffsetStage(Steps.Then(Step.Right(columns)));

    /// <inheritdoc cref="Projection.AfterBlankRows()"/>
    public OffsetStage AfterBlankRows() => new OffsetStage(Steps.Then(Step.AfterBlankRows()));

    /// <inheritdoc cref="Projection.AfterBlankColumns()"/>
    public OffsetStage AfterBlankColumns() => new OffsetStage(Steps.Then(Step.AfterBlankColumns()));

    /// <inheritdoc cref="Projection.SkipToFirstNonBlankCell()"/>
    public OffsetStage SkipToFirstNonBlankCell() => new OffsetStage(Steps.Then(Step.SkipToFirstNonBlankCell()));

    /// <summary>
    /// Declares the section's extent, replacing the derived one — after which the extent is consumed
    /// in full whether the section reads all of it or not. Extents do not stack, so a second
    /// <c>Sized</c> is refused: the pipeline goes on to <see cref="OffsetAndSizeStage"/>, where
    /// <c>Sized</c> is an <c>[Obsolete(error)]</c> stub.
    /// </summary>
    /// <param name="area">The extent.</param>
    public OffsetAndSizeStage Sized(IAreaStrategy area) => new OffsetAndSizeStage(Steps.Then(Step.Sized(area)));

    /// <summary>
    /// The extent stated and left as it is — optional explicitness, never ceremony: the terminal
    /// sizes to its children, or to its content, whether this is written or not.
    /// </summary>
    public OffsetAndSizeStage SizedToChildren() => new OffsetAndSizeStage(Steps);
  }

  /// <summary>
  /// Where the section starts and how big it is are both declared: only a bound, the headings and
  /// the subject remain.
  /// </summary>
  public sealed class OffsetAndSizeStage : UnboundedStage
  {
    internal OffsetAndSizeStage(Steps steps) : base(steps)
    {
    }

    /// <summary>Refused: extents do not stack, so a second one erases the first rather than narrowing it.</summary>
    /// <param name="area">The extent that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.ExtentsDoNotStack, error: true)]
    public OffsetAndSizeStage Sized(IAreaStrategy area) => throw new NotSupportedException(PipelineRefusals.ExtentsDoNotStack);

    /// <summary>Refused: a movement belongs with the offset, ahead of the extent.</summary>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage AfterBlankRows() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage AfterBlankColumns() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage SkipToFirstNonBlankCell() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);
  }

  /// <summary>
  /// Bounded: the section's end is declared, so only its headings and its subject can follow. An
  /// extent after a bound would become the frame the landmark is sought in rather than a narrowing
  /// of it, and is refused for saying so.
  /// </summary>
  public sealed class BoundStage : PlacementStage
  {
    internal BoundStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage Heading(string text) => new HeadingStage(Steps, Headings.One(text));

    /// <summary>Refused: a projection has one end, and the axis comes with the landmark.</summary>
    /// <param name="landmark">The row that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage Until(IRowLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The column that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <summary>Refused: an extent declared after a bound frames the search rather than narrowing it.</summary>
    /// <param name="area">The extent that would frame the search.</param>
    [Obsolete(PipelineRefusals.BoundFramesTheExtent, error: true)]
    public BoundStage Sized(IAreaStrategy area) => throw new NotSupportedException(PipelineRefusals.BoundFramesTheExtent);

    /// <summary>Refused: a movement belongs with the offset, ahead of the bound.</summary>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <summary>Refused: a pipeline's anchor is its entry, so a second one is not a declaration.</summary>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }
}
