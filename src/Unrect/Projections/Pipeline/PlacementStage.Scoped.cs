using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A placement being declared over a space that answers more than <see cref="ISpace"/> — the same
  /// pipeline as <see cref="PlacementStage"/>, carrying a demand.
  /// <para>
  /// Two things start one: a scope, when the whole declaration is written over one space
  /// (<c>Over&lt;ISpreadsheetSpace&gt;().Below(mark)</c> or the same entry re-exported by
  /// <see cref="ProjectionBuilders{TSpace}"/>), and a demanding matcher on its own —
  /// <c>On(RowWithFormula())</c> lands here with nothing annotated, because the demand is in the
  /// matcher's type and the pipeline carries it to the terminal.
  /// </para>
  /// <para>
  /// <b>Why every terminal is repeated here rather than inherited.</b> The stage itself carries the
  /// demand, so each terminal has to hand it out in its result type; a scoped pipeline that closed on
  /// a plain leaf would drop the very requirement the matcher raised. That is also why this half is
  /// the whole terminal spread and not a subset of it — unlike
  /// <see cref="ProjectionScope{TSpace}"/>, which needs only the members that <em>take</em> a
  /// projection because everything else composes in by variance.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public abstract class PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected PlacementStage(Steps steps) => Steps = steps;

    internal Steps Steps { get; }

    /// <inheritdoc cref="PlacementStage.ToString()"/>
    public override string ToString() => $"{GetType().Name} {Steps}";

    // --- Layouts ---------------------------------------------------------------------------------
    //
    // The three terminals that exist for INFERENCE as well as for the demand: a lambda body cannot
    // drive inference, so a flow whose demand lives inside its own lambda has to be told, and the
    // receiver's type is the telling.

    /// <inheritdoc cref="Projection.VerticalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, T> VerticalFlow<T>(Layout<TSpace, T> build)
      => Close(Projection.VerticalFlow(Demand<TSpace>.Instance, build));

    /// <inheritdoc cref="Projection.HorizontalFlow{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, T> HorizontalFlow<T>(Layout<TSpace, T> build)
      => Close(Projection.HorizontalFlow(Demand<TSpace>.Instance, build));

    /// <inheritdoc cref="Projection.Overlay{T}(Layout{T})"/>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, T> Overlay<T>(Layout<TSpace, T> build)
      => Close(Projection.Overlay(Demand<TSpace>.Instance, build));

    // --- Repetition and alternation ----------------------------------------------------------------

    /// <inheritdoc cref="Projection.VerticalRepeat{T}"/>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<TSpace, T> item,
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
    public IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.HorizontalRepeat(item, separatedBy, atLeast, declared));

    /// <inheritdoc cref="Projection.Choice{T}"/>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public IProjection<TSpace, T> Choice<T>(params IProjection<TSpace, T>[] alternatives)
      => Close(Projection.Choice(alternatives));

    // --- Tables ------------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.Table{T}()"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>() => Close<IReadOnlyList<T>>(Projection.Table<T>());

    /// <inheritdoc cref="Projection.Table{T}(Func{TableBinding{T}, TableBinding{T}})"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Close<IReadOnlyList<T>>(Projection.Table(bind));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<CaptionMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="Projection.Table{T}(int, IProjection{T}, string)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    /// <inheritdoc cref="Projection.Table()"/>
    public IProjection<TSpace, IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> Table()
      => Close<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>>(Projection.Table());

    /// <inheritdoc cref="Projection.Table{T}(Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableRow, T> project)
      => Close<IReadOnlyList<T>>(Projection.Table(project));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableRow, T})"/>
    /// <typeparam name="T">What one row reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to each body row.</param>
    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, Func<TableRow, T> project)
      => Close<IReadOnlyList<T>>(Projection.Table(headerRows, project));

    /// <inheritdoc cref="Projection.Table{T}(Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjection<TSpace, T> Table<T>(Func<TableView, T> project) => Close<T>(Projection.Table(project));

    /// <inheritdoc cref="Projection.Table{T}(int, Func{TableView, T})"/>
    /// <typeparam name="T">What the table reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="project">The reading applied to the table as a whole.</param>
    public IProjection<TSpace, T> Table<T>(int headerRows, Func<TableView, T> project)
      => Close<T>(Projection.Table(headerRows, project));

    // --- Leaves ------------------------------------------------------------------------------------

    /// <inheritdoc cref="Projection.Cell{T}(Func{CellValue, T})"/>
    /// <typeparam name="T">What the cell reads.</typeparam>
    /// <param name="project">The reading applied to the cell.</param>
    public IProjection<TSpace, T> Cell<T>(Func<CellValue, T> project) => Close<T>(Projection.Cell(project));

    /// <inheritdoc cref="Projection.Text()"/>
    public IProjection<TSpace, string> Text() => Close<string>(Projection.Text());

    /// <inheritdoc cref="Projection.Decimal()"/>
    public IProjection<TSpace, decimal> Decimal() => Close<decimal>(Projection.Decimal());

    /// <inheritdoc cref="Projection.Integer()"/>
    public IProjection<TSpace, int> Integer() => Close<int>(Projection.Integer());

    /// <inheritdoc cref="Projection.Double()"/>
    public IProjection<TSpace, double> Double() => Close<double>(Projection.Double());

    /// <inheritdoc cref="Projection.Date()"/>
    public IProjection<TSpace, DateTime> Date() => Close<DateTime>(Projection.Date());

    /// <inheritdoc cref="Projection.Boolean()"/>
    public IProjection<TSpace, bool> Boolean() => Close<bool>(Projection.Boolean());

    /// <inheritdoc cref="Projection.Caption(string)"/>
    /// <param name="text">What the row must say.</param>
    public IProjection<TSpace, string> Caption(string text) => Close<string>(Projection.Caption(text));

    /// <inheritdoc cref="Projection.Row{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<TSpace, T> Row<T>(Func<CellStrip, T> project) => Close<T>(Projection.Row(project));

    /// <inheritdoc cref="Projection.Row{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="width">How many columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<TSpace, T> Row<T>(int width, Func<CellStrip, T> project)
      => Close<T>(Projection.Row(width, project));

    /// <inheritdoc cref="Projection.Row{T}(IColumnStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the row reads.</typeparam>
    /// <param name="columns">The columns the row spans.</param>
    /// <param name="project">The reading applied to the row's cells.</param>
    public IProjection<TSpace, T> Row<T>(IColumnStrategy columns, Func<CellStrip, T> project)
      => Close<T>(Projection.Row(columns, project));

    /// <inheritdoc cref="Projection.Column{T}(Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<TSpace, T> Column<T>(Func<CellStrip, T> project) => Close<T>(Projection.Column(project));

    /// <inheritdoc cref="Projection.Column{T}(int, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="height">How many rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<TSpace, T> Column<T>(int height, Func<CellStrip, T> project)
      => Close<T>(Projection.Column(height, project));

    /// <inheritdoc cref="Projection.Column{T}(IRowStrategy, Func{CellStrip, T})"/>
    /// <typeparam name="T">What the column reads.</typeparam>
    /// <param name="rows">The rows the column spans.</param>
    /// <param name="project">The reading applied to the column's cells.</param>
    public IProjection<TSpace, T> Column<T>(IRowStrategy rows, Func<CellStrip, T> project)
      => Close<T>(Projection.Column(rows, project));

    /// <inheritdoc cref="Projection.Range{T}(Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<TSpace, T> Range<T>(Func<CellBlock, T> project) => Close<T>(Projection.Range(project));

    /// <inheritdoc cref="Projection.Range{T}(int, int, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="width">How many columns the region spans.</param>
    /// <param name="height">How many rows the region spans.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<TSpace, T> Range<T>(int width, int height, Func<CellBlock, T> project)
      => Close<T>(Projection.Range(width, height, project));

    /// <inheritdoc cref="Projection.Range{T}(IAreaStrategy, Func{CellBlock, T})"/>
    /// <typeparam name="T">What the region reads.</typeparam>
    /// <param name="area">How far the region extends.</param>
    /// <param name="project">The reading applied to the region's cells.</param>
    public IProjection<TSpace, T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project)
      => Close<T>(Projection.Range(area, project));

    /// <inheritdoc cref="Projection.Fields(Field[])"/>
    /// <param name="fields">The labelled pairs, in the order they sit on the sheet.</param>
    public IProjection<TSpace, IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields)
      => Close<IReadOnlyDictionary<string, CellValue>>(Projection.Fields(fields));

    // --- The hoisted-reuse terminal ------------------------------------------------------------------

    /// <inheritdoc cref="PlacementStage.Of{T}(IProjection{T})"/>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration to place. A projection demanding less is accepted as it is.</param>
    public IProjection<TSpace, T> Of<T>(IProjection<TSpace, T> projection) => Close(projection);

    private IProjection<TSpace, T> Close<T>(IProjection<T> projection) => Steps.ApplyTo(projection);

    private IProjection<TSpace, T> Close<T>(IProjection<TSpace, T> projection)
      => Steps.ApplyTo(ProjectionExtensions.Plain(projection));
  }

  /// <summary>The scoped twin of <see cref="UnboundedStage"/>.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public abstract class UnboundedStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected UnboundedStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));

    /// <inheritdoc cref="UnboundedStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before, matched by a demanding matcher.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(
        (landmark ?? throw new ArgumentNullException(nameof(landmark))).Landmark,
        orEnd)));

    /// <inheritdoc cref="UnboundedStage.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before, matched by a demanding matcher.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(
        (landmark ?? throw new ArgumentNullException(nameof(landmark))).Landmark,
        orEnd)));

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }

  /// <summary>The scoped twin of <see cref="OffsetStage"/>.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public sealed class OffsetStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    internal OffsetStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="OffsetStage.Down(int)"/>
    /// <param name="rows">How far down.</param>
    public OffsetStage<TSpace> Down(int rows) => new OffsetStage<TSpace>(Steps.Then(Step.Down(rows)));

    /// <inheritdoc cref="OffsetStage.Right(int)"/>
    /// <param name="columns">How far right.</param>
    public OffsetStage<TSpace> Right(int columns) => new OffsetStage<TSpace>(Steps.Then(Step.Right(columns)));

    /// <inheritdoc cref="OffsetStage.AfterBlankRows()"/>
    public OffsetStage<TSpace> AfterBlankRows() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankRows()));

    /// <inheritdoc cref="OffsetStage.AfterBlankColumns()"/>
    public OffsetStage<TSpace> AfterBlankColumns() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankColumns()));

    /// <inheritdoc cref="OffsetStage.Sized(IAreaStrategy)"/>
    /// <param name="area">The extent.</param>
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => new OffsetAndSizeStage<TSpace>(Steps.Then(Step.Sized(area)));

    /// <inheritdoc cref="OffsetStage.SizedToChildren()"/>
    public OffsetAndSizeStage<TSpace> SizedToChildren() => new OffsetAndSizeStage<TSpace>(Steps);
  }

  /// <summary>The scoped twin of <see cref="OffsetAndSizeStage"/>.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public sealed class OffsetAndSizeStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    internal OffsetAndSizeStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="OffsetAndSizeStage.Sized(IAreaStrategy)"/>
    /// <param name="area">The extent that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.ExtentsDoNotStack, error: true)]
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.ExtentsDoNotStack);

    /// <inheritdoc cref="OffsetAndSizeStage.Down(int)"/>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="OffsetAndSizeStage.Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="OffsetAndSizeStage.Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> AfterBlankRows() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="OffsetAndSizeStage.Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public OffsetAndSizeStage<TSpace> AfterBlankColumns() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);
  }

  /// <summary>The scoped twin of <see cref="BoundStage"/>.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public sealed class BoundStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    internal BoundStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));

    /// <inheritdoc cref="BoundStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="BoundStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The column that would be the second end.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.ProjectionHasOneEnd, error: true)]
    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.ProjectionHasOneEnd);

    /// <inheritdoc cref="BoundStage.Sized(IAreaStrategy)"/>
    /// <param name="area">The extent that would frame the search.</param>
    [Obsolete(PipelineRefusals.BoundFramesTheExtent, error: true)]
    public BoundStage<TSpace> Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.BoundFramesTheExtent);

    /// <inheritdoc cref="BoundStage.Down(int)"/>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage<TSpace> Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="BoundStage.Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public BoundStage<TSpace> Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);

    /// <inheritdoc cref="UnboundedStage.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.SecondAnchor, error: true)]
    public BoundStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.SecondAnchor);
  }
}
