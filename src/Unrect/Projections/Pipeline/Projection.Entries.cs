using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The placement pipeline's entries: where a section sits, said before the section itself.
  /// <para>
  /// Every anchor in the vocabulary is here and nowhere else, and that is the whole of the
  /// compile-time discipline — an anchor is a root, movements never lead back to one, and nothing
  /// chains past a terminal, so the contradictions a placement can state are unspellable rather than
  /// refused when they run. What follows an entry is <see cref="PlacementStage"/>.
  /// </para>
  /// <para>
  /// The same words are still modifiers, unchanged: <c>section.Below(mark)</c> reads subject-first,
  /// <c>Below(mark).Of(section)</c> reads position-first, and they are one declaration. Prefer the
  /// entry where the position is what the reader needs first — which, on a sheet, is usually.
  /// </para>
  /// </summary>
  public static partial class Projection
  {
    // --- Anchors: a position stated as a relation to something in the grid -------------------------

    /// <inheritdoc cref="ProjectionExtensions.On{TProjection}(TProjection, IRowLandmark)"/>
    /// <param name="landmark">The row to sit on.</param>
    public static OffsetStage On(IRowLandmark landmark) => Enter(Step.OnRow(landmark));

    /// <inheritdoc cref="ProjectionExtensions.On{TProjection}(TProjection, IColumnLandmark)"/>
    /// <param name="landmark">The column to sit on.</param>
    public static OffsetStage On(IColumnLandmark landmark) => Enter(Step.OnColumn(landmark));

    /// <inheritdoc cref="ProjectionExtensions.On{TProjection}(TProjection, IRowLandmark)"/>
    /// <remarks>
    /// A demanding matcher opens a demanding pipeline with nothing annotated: the demand is in the
    /// matcher's type, and every stage after this one carries it out to the terminal.
    /// </remarks>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The row to sit on.</param>
    public static OffsetStage<TSpace> On<TSpace>(IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => EnterDemanding<TSpace>(Step.OnRow(Required(landmark).Landmark));

    /// <inheritdoc cref="On{TSpace}(IRowLandmark{TSpace})"/>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The column to sit on.</param>
    public static OffsetStage<TSpace> On<TSpace>(IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => EnterDemanding<TSpace>(Step.OnColumn(Required(landmark).Landmark));

    /// <inheritdoc cref="ProjectionExtensions.Below{TProjection}"/>
    /// <param name="landmark">The row to sit below.</param>
    public static OffsetStage Below(IRowLandmark landmark) => Enter(Step.Below(landmark));

    /// <inheritdoc cref="On{TSpace}(IRowLandmark{TSpace})"/>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The row to sit below.</param>
    public static OffsetStage<TSpace> Below<TSpace>(IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => EnterDemanding<TSpace>(Step.Below(Required(landmark).Landmark));

    /// <inheritdoc cref="ProjectionExtensions.RightOf{TProjection}"/>
    /// <param name="landmark">The column to sit right of.</param>
    public static OffsetStage RightOf(IColumnLandmark landmark) => Enter(Step.RightOf(landmark));

    /// <inheritdoc cref="On{TSpace}(IRowLandmark{TSpace})"/>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The column to sit right of.</param>
    public static OffsetStage<TSpace> RightOf<TSpace>(IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => EnterDemanding<TSpace>(Step.RightOf(Required(landmark).Landmark));

    // --- The strategy door, the filler-steppers and the counted movements --------------------------

    /// <inheritdoc cref="ProjectionExtensions.OffsetBy{TProjection}"/>
    /// <param name="offset">Where the section starts.</param>
    public static OffsetStage OffsetBy(IOffsetStrategy offset) => Enter(Step.OffsetBy(offset));

    /// <inheritdoc cref="ProjectionExtensions.AfterBlankRows{TProjection}"/>
    public static OffsetStage AfterBlankRows() => Enter(Step.AfterBlankRows());

    /// <inheritdoc cref="ProjectionExtensions.AfterBlankColumns{TProjection}"/>
    public static OffsetStage AfterBlankColumns() => Enter(Step.AfterBlankColumns());

    /// <summary>
    /// Steps over the blank rows and then the blank columns in front of the section — the one-word
    /// entry for the instinct the default deliberately refuses.
    /// <para>
    /// Silence is adjacency: a declaration that says nothing starts exactly where the one before it
    /// left off, because the blank bands a <c>separatedBy</c> counts on, and the empty region an
    /// <c>Optional</c> section needs to see, are content rather than noise. Skipping them is
    /// therefore a declared exception like any other, and this is how it is declared.
    /// </para>
    /// </summary>
    public static OffsetStage SkipEmptyRowsAndColumns() => AfterBlankRows().AfterBlankColumns();

    /// <inheritdoc cref="ProjectionExtensions.Down{TProjection}"/>
    /// <param name="rows">How far down.</param>
    public static OffsetStage Down(int rows) => Enter(Step.Down(rows));

    /// <inheritdoc cref="ProjectionExtensions.Right{TProjection}"/>
    /// <param name="columns">How far right.</param>
    public static OffsetStage Right(int columns) => Enter(Step.Right(columns));

    // --- Bounds -----------------------------------------------------------------------------------

    /// <inheritdoc cref="UnboundedStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage Until(IRowLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.None.Then(Step.UntilRow(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage.Until(IRowLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> Until<TSpace>(IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilRow(Required(landmark).Landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage.UntilColumn(IColumnLandmark, bool)"/>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => new BoundStage(Steps.None.Then(Step.UntilColumn(landmark, orEnd)));

    /// <inheritdoc cref="UnboundedStage.UntilColumn(IColumnLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand the matcher raises.</typeparam>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static BoundStage<TSpace> UntilColumn<TSpace>(IColumnLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => new BoundStage<TSpace>(Steps.None.Then(Step.UntilColumn(Required(landmark).Landmark, orEnd)));

    // --- The heading ------------------------------------------------------------------------------

    /// <inheritdoc cref="HeadingStage"/>
    /// <param name="text">What the heading row says.</param>
    public static HeadingStage Heading(string text) => new HeadingStage(Steps.None, Headings.One(text));

    private static OffsetStage Enter(Step step) => new OffsetStage(Steps.None.Then(step));

    private static OffsetStage<TSpace> EnterDemanding<TSpace>(Step step)
      where TSpace : class, ISpace
      => new OffsetStage<TSpace>(Steps.None.Then(step));

    private static TLandmark Required<TLandmark>(TLandmark landmark)
      where TLandmark : class
      => landmark ?? throw new ArgumentNullException(nameof(landmark));
  }
}
