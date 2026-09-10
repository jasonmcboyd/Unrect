using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Projections;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — the inverted pipeline's intermediates, plain half (<c>staged-placement-note.md</c>
  /// §5.2). Placement runs in execution order and the projection TERMINATES the pipeline:
  /// <code>
  /// Below(mark)          // entry factory — anchors exist ONLY here
  ///   .Down(1)           // movement: stage -> stage
  ///   .Sized(strategy)   // optional size stage
  ///   .VerticalFlow(v =&gt; …);   // terminal: the projection closes the pipeline
  /// </code>
  /// <para>
  /// <b>Why classes rather than interfaces with extension terminals.</b> A terminal whose result
  /// type argument must be STATED — <c>Table&lt;Transaction&gt;()</c> is the corpus's commonest —
  /// cannot be an extension method on a stage generic in the space: C# infers a method's type
  /// arguments all or none, so <c>stage.Table&lt;Transaction&gt;()</c> would demand the space
  /// argument too. Instance members on a (generic) type take the space from the receiver's TYPE and
  /// leave the method generic only in what it reads. Same reason <c>ProjectionScope&lt;TSpace&gt;</c>
  /// exists; the inversion does not remove it.
  /// </para>
  /// </summary>
  public abstract class PlacementStage
  {
    private protected PlacementStage(Steps steps) => Steps = steps;

    internal Steps Steps { get; }

    /// <summary>What this pipeline has declared, for reading in a tooltip or a report.</summary>
    public override string ToString() => $"{GetType().Name} {Steps}";

    // --- Layout terminals -------------------------------------------------------------------------

    public IProjection<T> VerticalFlow<T>(Layout<T> build) => Close(Projection.VerticalFlow(build));

    public IProjection<T> HorizontalFlow<T>(Layout<T> build) => Close(Projection.HorizontalFlow(build));

    public IProjection<T> Overlay<T>(Layout<T> build) => Close(Projection.Overlay(build));

    /// <summary>The witness form, for a layout whose demand lives inside its own lambda.</summary>
    public IProjection<TSpace, T> VerticalFlow<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Close(Projection.VerticalFlow(over, build));

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Demand{TSpace}, Layout{TSpace, T})"/>
    public IProjection<TSpace, T> Overlay<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Close(Projection.Overlay(over, build));

    // --- Repetition -------------------------------------------------------------------------------

    public IProjection<IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.VerticalRepeat(item, separatedBy, atLeast, declared));

    public IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<TSpace, T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      where TSpace : class, ISpace
      => Close(Projection.VerticalRepeat(item, separatedBy, atLeast, declared));

    public IProjection<IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.HorizontalRepeat(item, separatedBy, atLeast, declared));

    // --- Tables -----------------------------------------------------------------------------------

    public IProjection<IReadOnlyList<T>> Table<T>() => Close(Projection.Table<T>());

    public IProjection<IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Close(Projection.Table(bind));

    public IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    public IProjection<TSpace, IReadOnlyList<T>> Table<TSpace, T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      where TSpace : class, ISpace
      => Close(Projection.Table(headerRows, eachRow, declared));

    public IProjection<IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<CaptionMap, IProjection<T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    public IProjection<IReadOnlyList<T>> Table<T>(Func<TableRow, T> project) => Close(Projection.Table(project));

    // --- Leaves and regions -----------------------------------------------------------------------

    public IProjection<string> Text() => Close(Projection.Text());

    public IProjection<decimal> Decimal() => Close(Projection.Decimal());

    public IProjection<int> Integer() => Close(Projection.Integer());

    public IProjection<double> Double() => Close(Projection.Double());

    public IProjection<DateTime> Date() => Close(Projection.Date());

    public IProjection<bool> Boolean() => Close(Projection.Boolean());

    public IProjection<string> Caption(string text) => Close(Projection.Caption(text));

    public IProjection<T> Row<T>(Func<CellStrip, T> project) => Close(Projection.Row(project));

    public IProjection<T> Column<T>(Func<CellStrip, T> project) => Close(Projection.Column(project));

    public IProjection<T> Range<T>(Func<CellBlock, T> project) => Close(Projection.Range(project));

    public IProjection<T> Range<T>(IAreaStrategy area, Func<CellBlock, T> project) => Close(Projection.Range(area, project));

    // --- The hoisted-reuse terminal ---------------------------------------------------------------

    /// <summary>
    /// Places a projection declared elsewhere — <c>Below(mark).Of(transactions)</c>, which is
    /// alternative B's <c>Placed(Below(mark), transactions)</c> in fluent clothes.
    /// </summary>
    public IProjection<T> Of<T>(IProjection<T> projection) => Close(projection);

    /// <inheritdoc cref="Of{T}(IProjection{T})"/>
    public IProjection<TSpace, T> Of<TSpace, T>(IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => Close(projection);

    private IProjection<T> Close<T>(IProjection<T> projection) => Steps.ApplyTo(projection);

    // The one cast the façade makes, on the library's own licence: every projection it builds
    // implements IProjection<T>, and the demand lives only in the static type.
    private IProjection<TSpace, T> Close<TSpace, T>(IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => Steps.ApplyTo((IProjection<T>)projection);
  }

  /// <summary>A pipeline whose extent is not yet bounded by a landmark.</summary>
  public abstract class UnboundedStage : PlacementStage
  {
    private protected UnboundedStage(Steps steps) : base(steps)
    {
    }

    /// <summary>
    /// The captions this section sits under — <c>On(mark).Under(Caption("Detail")).Of(lines)</c>.
    /// <para>
    /// Geography: a caption is ABOVE its section on the sheet, so it spells BEFORE the subject. The
    /// step is recorded as the innermost one, so an anchor declared to its left lands on the
    /// caption-and-content flow rather than on the content alone — which is the documented
    /// repeat-stop recipe, spelled top to bottom.
    /// </para>
    /// </summary>
    public UnderStage Under(params IProjection<string>[] captions)
      => new UnderStage(Steps.Before(Step.Under(captions)));

    /// <summary>
    /// The heading transition — <c>On(mark).Heading("Region A").Of(lines)</c>. Ruling 1's canonical
    /// order: anchors and offsets, then bounds and sizes, then headings, then the subject.
    /// </summary>
    /// <inheritdoc cref="HeadingStage"/>
    public HeadingStage Heading(string text) => new HeadingStage(Steps, Headings.One(text));

    /// <summary>
    /// The bound as a PIPELINE STAGE — <c>Below(m).Until(next).VerticalFlow(…)</c>.
    /// <para>
    /// <b>SUPERSEDED (the geography law, 2026-09-09).</b> An operator sits on the side of its
    /// subject where its referent sits on the sheet, and a bound's landmark is BELOW the section it
    /// ends — so postfix <c>.Until</c> was geographically right all along. Kept here as the record
    /// of what the stage form looked like; the acceptance reads use the postfix form, and a
    /// renovation should not ship this.
    /// </para>
    /// </summary>
    public BoundStage Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    public BoundStage<TSpace> Until<TSpace>(IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(landmark.Landmark, orEnd)));

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    public BoundStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.Then(Step.UntilColumn(landmark, orEnd)));
  }

  /// <summary>
  /// Offset declared, size open. Movements compose onto it; there are deliberately NO anchor members
  /// — an anchor is a root, so a second one is unspellable rather than silently dropped.
  /// </summary>
  public sealed class OffsetStage : UnboundedStage
  {
    internal OffsetStage(Steps steps) : base(steps)
    {
    }

    public OffsetStage Down(int rows) => new OffsetStage(Steps.Then(Step.Down(rows)));

    public OffsetStage Right(int columns) => new OffsetStage(Steps.Then(Step.Right(columns)));

    public OffsetStage AfterBlankRows() => new OffsetStage(Steps.Then(Step.AfterBlankRows()));

    public OffsetStage AfterBlankColumns() => new OffsetStage(Steps.Then(Step.AfterBlankColumns()));

    public OffsetAndSizeStage Sized(IAreaStrategy area) => new OffsetAndSizeStage(Steps.Then(Step.Sized(area)));

    /// <summary>
    /// The size stage stated and left at its default — optional explicitness, never ceremony: the
    /// terminal sizes to its children (or to its content) whether this is written or not.
    /// </summary>
    public OffsetAndSizeStage SizedToChildren() => new OffsetAndSizeStage(Steps);
  }

  /// <summary>Both slots written: no anchors, no movements, no second size.</summary>
  public sealed class OffsetAndSizeStage : UnboundedStage
  {
    internal OffsetAndSizeStage(Steps steps) : base(steps)
    {
    }
  }

  /// <summary>
  /// Bounded: the extent's end is declared, so only a heading or a terminal can follow.
  /// <para>
  /// The heading transition is what ruling 1's canonical order asks for — bounds before headings —
  /// and it is the one thing a bound stage gained in the Heading trial. Nothing else was added: a
  /// second bound, a size and every anchor stay unspellable.
  /// </para>
  /// </summary>
  public sealed class BoundStage : PlacementStage
  {
    internal BoundStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    public HeadingStage Heading(string text) => new HeadingStage(Steps, Headings.One(text));
  }

  /// <summary>
  /// Everything above the subject has been said — where the section starts, how big it is, and what
  /// announces it — so only the subject remains.
  /// <para>
  /// The four anchors are present as <c>[Obsolete(error)]</c> stubs rather than absent, because the
  /// reason they are refused here is worth saying: <b>a caption already locates the section</b>.
  /// <c>Under</c> finds its captions by content and consumes them, so a second locator to its right
  /// is either the same statement twice or a contradiction. Where both are genuinely wanted — the
  /// repeat-stop recipe, where the anchor must sit on the flow so a repetition can run out of
  /// sections — the anchor is the pipeline's ENTRY and reads first, because it is furthest up the
  /// sheet.
  /// </para>
  /// </summary>
  public sealed class UnderStage : PlacementStage
  {
    internal const string CaptionIsTheAnchor =
      "a section that sits under a caption is already located by it: Under finds the caption by content "
      + "and consumes it. If the section needs an anchor as well — the repeat-stop recipe, where the "
      + "anchor must sit on the caption-and-content flow — declare it as the pipeline's entry, which "
      + "reads first because it is furthest up the sheet: On(mark).Under(caption).Of(section).";

    internal UnderStage(Steps steps) : base(steps)
    {
    }

    [Obsolete(CaptionIsTheAnchor, error: true)]
    public UnderStage On(IRowLandmark landmark) => throw new NotSupportedException(CaptionIsTheAnchor);

    [Obsolete(CaptionIsTheAnchor, error: true)]
    public UnderStage Below(IRowLandmark landmark) => throw new NotSupportedException(CaptionIsTheAnchor);

    [Obsolete(CaptionIsTheAnchor, error: true)]
    public UnderStage RightOf(IColumnLandmark landmark) => throw new NotSupportedException(CaptionIsTheAnchor);

    [Obsolete(CaptionIsTheAnchor, error: true)]
    public UnderStage OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(CaptionIsTheAnchor);
  }
}
