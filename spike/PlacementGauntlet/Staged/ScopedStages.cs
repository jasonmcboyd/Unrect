using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Projections;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — the inverted pipeline's intermediates, scoped half: the same stages with the space
  /// already answered, entered by <c>Place.Over&lt;TSpace&gt;()</c> or by a demanding matcher
  /// (<c>On(RowWithFormula())</c> lands here on its own).
  /// <para>
  /// <b>Finding, recorded where it was met.</b> The scope's one-sentence rule does NOT survive the
  /// inversion. <c>ProjectionScope&lt;TSpace&gt;</c> carries eight members — only the factories that
  /// take projections — because everything else is space-indifferent and composes in by variance. A
  /// scoped STAGE cannot be that small: the stage itself carries the demand (its landmark may have
  /// raised one), so EVERY terminal on it must hand that demand out in its result type. The scoped
  /// half is therefore the whole terminal spread, not a subset of it.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public abstract class PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected PlacementStage(Steps steps) => Steps = steps;

    internal Steps Steps { get; }

    public override string ToString() => $"{GetType().Name} {Steps}";

    // --- Layouts. The three members that exist for INFERENCE: a lambda body cannot drive it, so a
    //     flow whose demand lives inside its own lambda has to be told, and the receiver's type is
    //     the telling. This is the whole reason a scoped entry exists.

    public IProjection<TSpace, T> VerticalFlow<T>(Layout<TSpace, T> build)
      => Close(Projection.VerticalFlow(Demand<TSpace>.Instance, build));

    public IProjection<TSpace, T> HorizontalFlow<T>(Layout<TSpace, T> build)
      => Close(Projection.HorizontalFlow(Demand<TSpace>.Instance, build));

    public IProjection<TSpace, T> Overlay<T>(Layout<TSpace, T> build)
      => Close(Projection.Overlay(Demand<TSpace>.Instance, build));

    // --- Repetition, tables, alternation ----------------------------------------------------------

    public IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.VerticalRepeat(item, separatedBy, atLeast, declared: declared));

    public IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      => Close(Projection.HorizontalRepeat(item, separatedBy, atLeast, declared: declared));

    public IProjection<TSpace, IReadOnlyList<T>> Table<T>() => Close<IReadOnlyList<T>>(Projection.Table<T>());

    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<T>, TableBinding<T>> bind)
      => Close<IReadOnlyList<T>>(Projection.Table(bind));

    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    public IProjection<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<LabelMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      => Close(Projection.Table(headerRows, eachRow, declared));

    public IProjection<TSpace, T> Choice<T>(params IProjection<TSpace, T>[] alternatives)
      => Close(Projection.Choice(alternatives));

    // --- Leaves. Present here NOT for prose but because the stage's own demand has to leave through
    //     whatever terminal closes the pipeline.

    public IProjection<TSpace, string> Text() => Close<string>(Projection.Text());

    public IProjection<TSpace, decimal> Decimal() => Close<decimal>(Projection.Decimal());

    public IProjection<TSpace, DateTime> Date() => Close<DateTime>(Projection.Date());

    public IProjection<TSpace, string> Caption(string text) => Close<string>(Projection.Caption(text));

    public IProjection<TSpace, T> Row<T>(Func<CellStrip, T> project) => Close<T>(Projection.Row(project));

    public IProjection<TSpace, T> Range<T>(Func<CellBlock, T> project) => Close<T>(Projection.Range(project));

    public IProjection<TSpace, T> Of<T>(IProjection<TSpace, T> projection) => Close(projection);

    private IProjection<TSpace, T> Close<T>(IProjection<T> projection) => Steps.ApplyTo(projection);

    private IProjection<TSpace, T> Close<T>(IProjection<TSpace, T> projection)
      => Steps.ApplyTo((IProjection<T>)projection);
  }

  /// <summary>The scoped twin of <see cref="UnboundedStage"/>.</summary>
  public abstract class UnboundedStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private protected UnboundedStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Under(IProjection{string}[])"/>
    /// <remarks>
    /// The captions are PLAIN and cannot be otherwise: <c>ProjectionExtensions.Under</c> takes
    /// <c>IProjection&lt;string&gt;[]</c>, and that is deliberate rather than an oversight — "a
    /// caption demands nothing of its space, which is the whole of what a caption is". So a scoped
    /// <c>Under</c> scopes the SECTION, never the captions; a demanding caption is refused, and the
    /// refusal is recorded in <c>MustNotCompile.cs</c> as (m).
    /// </remarks>
    public UnderStage<TSpace> Under(params IProjection<string>[] captions)
      => new UnderStage<TSpace>(Steps.Before(Step.Under(captions)));

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));

    /// <inheritdoc cref="UnboundedStage.Until(IRowLandmark, bool)"/>
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(landmark, orEnd)));

    public BoundStage<TSpace> Until(IRowLandmark<TSpace> landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilRow(landmark.Landmark, orEnd)));

    public BoundStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.Then(Step.UntilColumn(landmark, orEnd)));
  }

  /// <summary>
  /// The scoped offset stage. It carries the same refusal as its plain twin — no anchors — and
  /// carries it TWICE OVER, deliberately: the four anchor members are present as
  /// <c>[Obsolete(error)]</c> stubs so the gauntlet can measure the difference between refusal by
  /// ABSENCE (CS1061 on the plain stages, a compiler sentence about a missing member) and refusal by
  /// a TEACHING message (CS0619, the library's own words). Both are compile errors; only one says
  /// what to write instead.
  /// </summary>
  public sealed class OffsetStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    private const string SecondAnchor =
      "a pipeline declares where it starts once, and its anchor is its entry: On/Below/RightOf/OffsetBy "
      + "are factories, not stages. To search inside a region another landmark found, nest — place the "
      + "region, and place this projection within it. To carry on from a position, use Down or Right, "
      + "which compose onto it.";

    internal OffsetStage(Steps steps) : base(steps)
    {
    }

    public OffsetStage<TSpace> Down(int rows) => new OffsetStage<TSpace>(Steps.Then(Step.Down(rows)));

    public OffsetStage<TSpace> Right(int columns) => new OffsetStage<TSpace>(Steps.Then(Step.Right(columns)));

    public OffsetStage<TSpace> AfterBlankRows() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankRows()));

    public OffsetStage<TSpace> AfterBlankColumns() => new OffsetStage<TSpace>(Steps.Then(Step.AfterBlankColumns()));

    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area) => new OffsetAndSizeStage<TSpace>(Steps.Then(Step.Sized(area)));

    public OffsetAndSizeStage<TSpace> SizedToChildren() => new OffsetAndSizeStage<TSpace>(Steps);

    [Obsolete(SecondAnchor, error: true)]
    public OffsetStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(SecondAnchor);

    [Obsolete(SecondAnchor, error: true)]
    public OffsetStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(SecondAnchor);

    [Obsolete(SecondAnchor, error: true)]
    public OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(SecondAnchor);

    [Obsolete(SecondAnchor, error: true)]
    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(SecondAnchor);
  }

  /// <summary>The scoped twin of <see cref="OffsetAndSizeStage"/>.</summary>
  public sealed class OffsetAndSizeStage<TSpace> : UnboundedStage<TSpace>
    where TSpace : class, ISpace
  {
    internal OffsetAndSizeStage(Steps steps) : base(steps)
    {
    }
  }

  /// <summary>The scoped twin of <see cref="BoundStage"/>.</summary>
  public sealed class BoundStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    internal BoundStage(Steps steps) : base(steps)
    {
    }

    /// <inheritdoc cref="UnboundedStage.Heading(string)"/>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps, Headings.One(text));
  }

  /// <summary>The scoped twin of <see cref="UnderStage"/>, refusals and all.</summary>
  public sealed class UnderStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    internal UnderStage(Steps steps) : base(steps)
    {
    }

    [Obsolete(UnderStage.CaptionIsTheAnchor, error: true)]
    public UnderStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(UnderStage.CaptionIsTheAnchor);

    [Obsolete(UnderStage.CaptionIsTheAnchor, error: true)]
    public UnderStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(UnderStage.CaptionIsTheAnchor);

    [Obsolete(UnderStage.CaptionIsTheAnchor, error: true)]
    public UnderStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(UnderStage.CaptionIsTheAnchor);

    [Obsolete(UnderStage.CaptionIsTheAnchor, error: true)]
    public UnderStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(UnderStage.CaptionIsTheAnchor);
  }
}
