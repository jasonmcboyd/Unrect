using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  public static partial class Projection
  {
    /// <summary>
    /// The vocabulary, fixed to the space a declaration is written over —
    /// <c>Projection.Over&lt;ISpreadsheetSpace&gt;().VerticalFlow(v =&gt; …)</c>, the requirement in
    /// prose position.
    /// <para>
    /// This is the door; <see cref="ProjectionScope{TSpace}"/> is what it opens, and everything
    /// about what belongs in a scope, what does not, and when to reach for one is written there.
    /// The witness form (<c>VerticalFlow(Formulas, v =&gt; …)</c>) is not a rival: the scope is
    /// sugar over the same factories, and both stay.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space everything built through the scope is declared over.</typeparam>
    public static ProjectionScope<TSpace> Over<TSpace>()
      where TSpace : class, ISpace
      => default;
  }

  /// <summary>
  /// The composing vocabulary with <typeparamref name="TSpace"/> already
  /// answered — what <see cref="Projection.Over{TSpace}"/> hands back.
  /// <code>
  /// var p = Projection.Over&lt;ISpreadsheetSpace&gt;();
  ///
  /// var line   = p.Overlay(o =&gt; new Line(o.Next(Text()), o.Next(Formula().Right(3))));
  /// var ledger = p.VerticalFlow(v =&gt; new Ledger(v.Next(p.Table(headerRows: 1, eachRow: line))));
  /// </code>
  /// <para>
  /// <b>Why a scope rather than type arguments.</b> C# infers a method's type arguments all or
  /// none, so <c>VerticalFlow&lt;ISpreadsheetSpace, Ledger&gt;(…)</c> would make you write the
  /// result type as well — one type argument of requirement and one of ceremony. A scope answers
  /// the space once, on the type, leaving every member generic only in what it reads; the lambda
  /// still tells the compiler the rest.
  /// </para>
  /// <para>
  /// <b>What is in here, and why so little.</b> Only the factories that <em>take</em> projections
  /// and build one: the three layouts, the two composing <c>Table</c> rungs, the two repeats and
  /// <c>Choice</c> — and, beside them, the placement pipeline's entries, which start a pipeline
  /// whose stages carry <typeparamref name="TSpace"/> from birth and so hand it to every terminal's
  /// lambda. Everything else in the vocabulary — every leaf, every matcher, every extent and offset,
  /// and all of <see cref="ProjectionExtensions"/> — is indifferent to the space, so it composes
  /// into a scoped declaration by variance with nothing said. Write those exactly as you always
  /// did; there is no scoped spelling of <c>Text()</c> because there would be nothing for it to do.
  /// </para>
  /// <para>
  /// <b>Which entry to reach for.</b> A witness tells one factory what it is declared over
  /// (<c>VerticalFlow(Formulas, v =&gt; …)</c>) and is the right size when a mostly-plain
  /// declaration has one demanding layout in it. A scope says it once for a whole declaration and
  /// is the right size when the answer is the same everywhere. They are the same machinery: every
  /// member here forwards to the witness form.
  /// </para>
  /// <para>
  /// <b>A scope also diagnoses better</b>, which is the argument that was not expected. A child
  /// demanding more than a <em>plain</em> flow allows is a failure of type inference, reported as
  /// "the type arguments for <c>Next</c> cannot be inferred" — a message that never says which
  /// capability, and points two lines away from the fix. In a scope the cursor's space is already
  /// answered, so the same mistake is a failed argument conversion and both types are named:
  /// <c>cannot convert from 'IProjection&lt;IFormulaSpace, string?&gt;' to
  /// 'IProjection&lt;ISpace, string&gt;'</c>.
  /// </para>
  /// <para>
  /// <b>Demand the weakest thing that works.</b> A scope raises everything built through it to
  /// <typeparamref name="TSpace"/> whether the children needed it or not — <c>p.Table(1, plainRow)</c>
  /// is a table that will not run on a plain grid. In application code that is the point: "this
  /// parser is for spreadsheets" is the honest requirement and naming each capability is ceremony.
  /// In a hoisted library projection it is a mistake: a helper should demand the narrowest
  /// capability it actually uses (<c>IFormulaSpace</c>, not the bundle) so that it composes with
  /// anything able to answer, and a helper written through a scope quietly demands more than it
  /// reads.
  /// </para>
  /// <para>
  /// It carries nothing and configures nothing: <c>default</c> is as good as
  /// <see cref="Projection.Over{TSpace}"/>, which is why it is a struct and why no member of it
  /// can fail on the scope itself. Hold one in a local for a declaration written over many lines,
  /// or call the door inline for a single one.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space everything built through this scope is declared over.</typeparam>
  public readonly struct ProjectionScope<TSpace>
    where TSpace : class, ISpace
  {
    // --- Layouts ----------------------------------------------------------------------------
    //
    // The three members that exist for INFERENCE rather than for prose: a lambda body cannot drive
    // inference, so a flow whose demand lives inside its own lambda has to be told. Everything
    // below them composes without a scope and is here so that a scoped declaration reads as one
    // thing (see the type's remarks).

    /// <inheritdoc cref="Projection.VerticalFlow{T}(Layout{T})"/>
    /// <typeparam name="TResult">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, TResult> VerticalFlow<TResult>(Layout<TSpace, TResult> build)
      => Projection.VerticalFlow(Demand<TSpace>.Instance, build);

    /// <inheritdoc cref="Projection.HorizontalFlow{T}(Layout{T})"/>
    /// <typeparam name="TResult">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, TResult> HorizontalFlow<TResult>(Layout<TSpace, TResult> build)
      => Projection.HorizontalFlow(Demand<TSpace>.Instance, build);

    /// <inheritdoc cref="Projection.Overlay{T}(Layout{T})"/>
    /// <typeparam name="TResult">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public IProjection<TSpace, TResult> Overlay<TResult>(Layout<TSpace, TResult> build)
      => Projection.Overlay(Demand<TSpace>.Instance, build);

    // --- Tables, repetition, alternation ------------------------------------------------------

    /// <inheritdoc cref="Projection.Table{T}(int, IProjection{T}, string)"/>
    /// <typeparam name="TResult">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<TResult>> Table<TResult>(
      int headerRows,
      IProjection<TSpace, TResult> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Projection.Table(headerRows, eachRow, declared);

    /// <inheritdoc cref="Projection.Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>
    /// <typeparam name="TResult">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<TResult>> Table<TResult>(
      int headerRows,
      Func<CaptionMap, IProjection<TSpace, TResult>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Projection.Table(headerRows, eachRow, declared);

    /// <inheritdoc cref="Projection.VerticalRepeat{T}"/>
    /// <typeparam name="TResult">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<TResult>> VerticalRepeat<TResult>(
      IProjection<TSpace, TResult> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Projection.VerticalRepeat(item, separatedBy, atLeast, declared);

    /// <inheritdoc cref="Projection.HorizontalRepeat{T}"/>
    /// <typeparam name="TResult">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public IProjection<TSpace, IReadOnlyList<TResult>> HorizontalRepeat<TResult>(
      IProjection<TSpace, TResult> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Projection.HorizontalRepeat(item, separatedBy, atLeast, declared);

    /// <inheritdoc cref="Projection.Choice{T}"/>
    /// <typeparam name="TResult">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public IProjection<TSpace, TResult> Choice<TResult>(params IProjection<TSpace, TResult>[] alternatives)
      => Projection.Choice(alternatives);

    // --- The placement pipeline's entries, with the space already answered -------------------------
    //
    // A pipeline entered here carries TSpace from its first word, so the terminal that closes it
    // hands the demand out and a layout terminal's lambda receives a cursor over the right space —
    // the same inference wall the three layout members above exist for, met one stage earlier.

    /// <inheritdoc cref="Projection.On(IRowLandmark)"/>
    /// <param name="landmark">The row to sit on.</param>
    public OffsetStage<TSpace> On(IRowLandmark landmark) => Enter(Step.OnRow(landmark));

    /// <inheritdoc cref="Projection.On(IColumnLandmark)"/>
    /// <param name="landmark">The column to sit on.</param>
    public OffsetStage<TSpace> On(IColumnLandmark landmark) => Enter(Step.OnColumn(landmark));

    /// <inheritdoc cref="Projection.On{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The row to sit on. A matcher demanding less is accepted as it is.</param>
    public OffsetStage<TSpace> On(IRowLandmark<TSpace> landmark) => Enter(Step.OnRow(Required(landmark).Landmark));

    /// <inheritdoc cref="Projection.On{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The column to sit on. A matcher demanding less is accepted as it is.</param>
    public OffsetStage<TSpace> On(IColumnLandmark<TSpace> landmark) => Enter(Step.OnColumn(Required(landmark).Landmark));

    /// <inheritdoc cref="Projection.Below(IRowLandmark)"/>
    /// <param name="landmark">The row to sit below.</param>
    public OffsetStage<TSpace> Below(IRowLandmark landmark) => Enter(Step.Below(landmark));

    /// <inheritdoc cref="Projection.Below{T}(IRowLandmark{T})"/>
    /// <param name="landmark">The row to sit below. A matcher demanding less is accepted as it is.</param>
    public OffsetStage<TSpace> Below(IRowLandmark<TSpace> landmark) => Enter(Step.Below(Required(landmark).Landmark));

    /// <inheritdoc cref="Projection.RightOf(IColumnLandmark)"/>
    /// <param name="landmark">The column to sit right of.</param>
    public OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => Enter(Step.RightOf(landmark));

    /// <inheritdoc cref="Projection.RightOf{T}(IColumnLandmark{T})"/>
    /// <param name="landmark">The column to sit right of. A matcher demanding less is accepted as it is.</param>
    public OffsetStage<TSpace> RightOf(IColumnLandmark<TSpace> landmark)
      => Enter(Step.RightOf(Required(landmark).Landmark));

    /// <inheritdoc cref="Projection.OffsetBy(IOffsetStrategy)"/>
    /// <param name="offset">Where the section starts.</param>
    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => Enter(Step.OffsetBy(offset));

    /// <inheritdoc cref="Projection.AfterBlankRows()"/>
    public OffsetStage<TSpace> AfterBlankRows() => Enter(Step.AfterBlankRows());

    /// <inheritdoc cref="Projection.AfterBlankColumns()"/>
    public OffsetStage<TSpace> AfterBlankColumns() => Enter(Step.AfterBlankColumns());

    /// <inheritdoc cref="Projection.SkipEmptyRowsAndColumns()"/>
    public OffsetStage<TSpace> SkipEmptyRowsAndColumns() => AfterBlankRows().AfterBlankColumns();

    /// <inheritdoc cref="Projection.Down(int)"/>
    /// <param name="rows">How far down.</param>
    public OffsetStage<TSpace> Down(int rows) => Enter(Step.Down(rows));

    /// <inheritdoc cref="Projection.Right(int)"/>
    /// <param name="columns">How far right.</param>
    public OffsetStage<TSpace> Right(int columns) => Enter(Step.Right(columns));

    /// <inheritdoc cref="Projection.Sized(IAreaStrategy)"/>
    /// <param name="area">The extent.</param>
    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => new OffsetAndSizeStage<TSpace>(Steps.None.Then(Step.Sized(area)));

    /// <inheritdoc cref="Projection.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="Projection.Until{T}(IRowLandmark{T}, bool)"/>
    /// <param name="landmark">The row the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(Required(landmark).Landmark, orEnd)));

    /// <inheritdoc cref="Projection.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="Projection.UntilColumn{T}(IColumnLandmark{T}, bool)"/>
    /// <param name="landmark">The column the extent stops before. A matcher demanding less is accepted as it is.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public BoundStage<TSpace> UntilColumn(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilColumn(Required(landmark).Landmark, orEnd)));

    /// <inheritdoc cref="HeadingStage"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps.None, Headings.One(text));

    private static OffsetStage<TSpace> Enter(Step step) => new OffsetStage<TSpace>(Steps.None.Then(step));

    private static TLandmark Required<TLandmark>(TLandmark landmark)
      where TLandmark : class
      => landmark ?? throw new ArgumentNullException(nameof(landmark));
  }
}
