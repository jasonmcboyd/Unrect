using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — the pipeline's ENTRIES. Every anchor in the vocabulary is here and nowhere else, which
  /// is the whole of the compile-time discipline: an anchor is a root, movements never lead back to
  /// one, and nothing chains past a terminal.
  /// <para>
  /// Written for use through <c>using static PlacementGauntlet.Staged.Place;</c>, so a declaration
  /// reads <c>Below(mark).Down(1).VerticalFlow(v =&gt; …)</c>.
  /// </para>
  /// </summary>
  public static class Place
  {
    // --- Anchors: a position stated as a relation to something in the grid -------------------------

    public static OffsetStage On(IRowLandmark landmark) => Start(Step.OnRow(landmark));

    public static OffsetStage On(IColumnLandmark landmark) => Start(Step.OnColumn(landmark));

    public static OffsetStage Below(IRowLandmark landmark) => Start(Step.Below(landmark));

    public static OffsetStage RightOf(IColumnLandmark landmark) => Start(Step.RightOf(landmark));

    // A demanding matcher enters the SCOPED hierarchy on its own: no witness, no scope, no
    // annotation — the demand is in the matcher's type and the pipeline carries it to the terminal.

    public static OffsetStage<TSpace> On<TSpace>(IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => StartScoped<TSpace>(Step.OnRow(landmark.Landmark));

    public static OffsetStage<TSpace> On<TSpace>(IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => StartScoped<TSpace>(Step.OnColumn(landmark.Landmark));

    public static OffsetStage<TSpace> Below<TSpace>(IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => StartScoped<TSpace>(Step.Below(landmark.Landmark));

    public static OffsetStage<TSpace> RightOf<TSpace>(IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => StartScoped<TSpace>(Step.RightOf(landmark.Landmark));

    // --- The strategy door, the filler-steppers, and the counted movements as entries --------------

    public static OffsetStage OffsetBy(IOffsetStrategy offset) => Start(Step.OffsetBy(offset));

    public static OffsetStage AfterBlankRows() => Start(Step.AfterBlankRows());

    public static OffsetStage AfterBlankColumns() => Start(Step.AfterBlankColumns());

    /// <summary>
    /// The one-word entry for the instinct the default deliberately refuses: silence is adjacency,
    /// so skipping filler is a declared exception like any other.
    /// </summary>
    public static OffsetStage SkipEmptyRowsAndColumns()
      => Start(Step.AfterBlankRows()).AfterBlankColumns();

    public static OffsetStage Down(int rows) => Start(Step.Down(rows));

    public static OffsetStage Right(int columns) => Start(Step.Right(columns));

    /// <summary>
    /// The neutral entry — the offset stage stated and left at its default.
    /// <para>
    /// <b>Finding.</b> Unscoped, this states nothing about the document and exists only to open a
    /// pipeline; it is the one entry that fails the stating-vs-appeasing test. Its real job is on
    /// the scope (<c>Place.Over&lt;T&gt;().Offset()</c>), where it carries the space into a layout
    /// lambda that inference cannot reach.
    /// </para>
    /// </summary>
    public static OffsetStage Offset() => new OffsetStage(Steps.None);

    // --- Size entries -----------------------------------------------------------------------------

    public static OffsetAndSizeStage Sized(IAreaStrategy area) => new OffsetAndSizeStage(Steps.None.Then(Step.Sized(area)));

    public static OffsetAndSizeStage SizedToChildren() => new OffsetAndSizeStage(Steps.None);

    // --- The caption entry ------------------------------------------------------------------------
    //
    // THE GEOGRAPHY LAW: an operator sits on the side of its subject where its referent sits on the
    // sheet. A caption is above its section, so it spells before it — and a bound's landmark is
    // below, so it spells after, which is postfix .Until unchanged.

    /// <inheritdoc cref="UnboundedStage.Under(IProjection{string}[])"/>
    public static UnderStage Under(params IProjection<string>[] captions)
      => new UnderStage(Steps.None.Before(Step.Under(captions)));

    /// <summary>
    /// The heading entry — <c>Heading("IRR Details").Of(details)</c>. Self-anchoring: a heading
    /// locates its own section by content, so it needs nothing to its left.
    /// </summary>
    /// <inheritdoc cref="HeadingStage"/>
    public static HeadingStage Heading(string text) => new HeadingStage(Steps.None, Headings.One(text));

    // --- Bound entries ----------------------------------------------------------------------------
    //
    // SUPERSEDED by the geography law (2026-09-09): a bound's landmark sits BELOW the section it
    // ends, so it belongs after the subject, where today's postfix .Until already is. Kept as the
    // record of what the stage form cost — a third entry family, because "entry at any stage" then
    // has to admit a bound-only pipeline for hoisted reuse.

    public static BoundStage Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.None.Then(Step.UntilRow(landmark, orEnd)));

    public static BoundStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.None.Then(Step.UntilColumn(landmark, orEnd)));

    /// <summary>
    /// The scoped door: <c>Place.Over&lt;ISpreadsheetSpace&gt;().Below(m).VerticalFlow(v =&gt; …)</c>.
    /// <para>
    /// It vends ENTRIES rather than factories, so the space is answered once at the head of the
    /// pipeline and every stage after it carries the answer.
    /// </para>
    /// </summary>
    public static PlacementScope<TSpace> Over<TSpace>()
      where TSpace : class, ISpace
      => default;

    private static OffsetStage Start(Step step) => new OffsetStage(Steps.None.Then(step));

    private static OffsetStage<TSpace> StartScoped<TSpace>(Step step)
      where TSpace : class, ISpace
      => new OffsetStage<TSpace>(Steps.None.Then(step));
  }

  /// <summary>
  /// SPIKE — the entries with the space already answered. Eight members under one rule: every entry
  /// the pipeline has, and nothing else, because a scope's whole job here is to start a pipeline
  /// whose stages carry <typeparamref name="TSpace"/> from birth.
  /// </summary>
  /// <typeparam name="TSpace">The space everything built through this scope is declared over.</typeparam>
  public readonly struct PlacementScope<TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The neutral entry: no placement declared, but the space answered.</summary>
    public OffsetStage<TSpace> Offset() => new OffsetStage<TSpace>(Steps.None);

    public OffsetStage<TSpace> On(IRowLandmark landmark) => Start(Step.OnRow(landmark));

    public OffsetStage<TSpace> On(IColumnLandmark landmark) => Start(Step.OnColumn(landmark));

    public OffsetStage<TSpace> Below(IRowLandmark landmark) => Start(Step.Below(landmark));

    public OffsetStage<TSpace> RightOf(IColumnLandmark landmark) => Start(Step.RightOf(landmark));

    public OffsetStage<TSpace> OffsetBy(IOffsetStrategy offset) => Start(Step.OffsetBy(offset));

    public OffsetStage<TSpace> AfterBlankRows() => Start(Step.AfterBlankRows());

    // The two the plain Place had and the scope did not — a gap found while building Entry C, which
    // re-exports the scope's entries verbatim and so cannot paper over a missing one.

    public OffsetStage<TSpace> AfterBlankColumns() => Start(Step.AfterBlankColumns());

    /// <inheritdoc cref="Place.SkipEmptyRowsAndColumns"/>
    public OffsetStage<TSpace> SkipEmptyRowsAndColumns() => AfterBlankRows().AfterBlankColumns();

    public OffsetStage<TSpace> Down(int rows) => Start(Step.Down(rows));

    public OffsetStage<TSpace> Right(int columns) => Start(Step.Right(columns));

    public OffsetAndSizeStage<TSpace> Sized(IAreaStrategy area)
      => new OffsetAndSizeStage<TSpace>(Steps.None.Then(Step.Sized(area)));

    public OffsetAndSizeStage<TSpace> SizedToChildren() => new OffsetAndSizeStage<TSpace>(Steps.None);

    /// <inheritdoc cref="UnboundedStage.Under(IProjection{string}[])"/>
    public UnderStage<TSpace> Under(params IProjection<string>[] captions)
      => new UnderStage<TSpace>(Steps.None.Before(Step.Under(captions)));

    /// <inheritdoc cref="Place.Heading(string)"/>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(Steps.None, Headings.One(text));

    /// <inheritdoc cref="Place.Until(IRowLandmark, bool)"/>
    public BoundStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(landmark, orEnd)));

    private OffsetStage<TSpace> Start(Step step) => new OffsetStage<TSpace>(Steps.None.Then(step));
  }
}
