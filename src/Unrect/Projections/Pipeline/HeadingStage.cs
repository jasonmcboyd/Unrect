using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// What a section announces itself by: <c>Heading("Portfolio Income").Of(lines)</c> finds the row
  /// that says it, asserts the text, consumes the row at the full available width, and places the
  /// section immediately below.
  /// <para>
  /// <b>A heading is structure, not a value.</b> It contributes no node of its own — the terminal
  /// mints the <c>Caption</c> leaves the pipeline replays, so a heading that is not there fails in
  /// the caption's own words, at the caption's own place in the path, exactly as a hand-written one
  /// would. Where the record <em>wants</em> the text, that is a different act with a different word:
  /// declare <c>Caption</c> as a child of a flow and read what it yields. <b>Heading asserts, Caption
  /// captures, geometry skips, matchers locate.</b>
  /// </para>
  /// <para>
  /// <b>It takes the text.</b> There is no overload taking a projection: a heading built out of a
  /// leaf whose value is then discarded is the shape this word exists to remove. A row whose text
  /// varies per file is not a heading but a landmark — locate it by shape with <c>RowWhere</c> — and
  /// a row nothing needs to say about is geometry.
  /// </para>
  /// <para>
  /// <b>It is self-anchoring</b>, so it needs nothing to its left: a heading locates its own section
  /// by content. It is also a stage, reached from the anchors, offsets, extents and bounds in the
  /// pipeline's order — where the section starts, how big it is, what announces it, then what it
  /// reads. Nothing leads back: an anchor after a heading is refused, because the heading has
  /// already said where the section is.
  /// </para>
  /// <para>
  /// <b>Headings chain in document order</b> — <c>Heading("IRR Details").Heading("Cash Flows Using
  /// Transfer Date")</c> — and accumulate into <em>one</em> statement, two rows above one section,
  /// rather than nesting into two sections. Layering is nesting and is spelled as nesting:
  /// <c>Heading(outer).Of(Heading(inner).Of(lines))</c>.
  /// </para>
  /// </summary>
  public sealed class HeadingStage : PlacementStage
  {
    private readonly Steps _placement;
    private readonly string[] _headings;

    internal HeadingStage(Steps placement, string[] headings)
      : base(placement.Before(Step.Headings(Headings.Captions(headings))))
    {
      _placement = placement;
      _headings = headings;
    }

    /// <summary>
    /// The next heading down the sheet, in document order — both above the same section.
    /// </summary>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage Heading(string text) => new HeadingStage(_placement, Headings.And(_headings, text));

    /// <summary>Refused: a section announced by a heading is already located by it.</summary>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <summary>Refused: a bound and an extent come before the headings — or after the subject.</summary>
    /// <param name="landmark">The row the extent would stop before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage Until(IRowLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The column the extent would stop before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <inheritdoc cref="Until(IRowLandmark, bool)"/>
    /// <param name="area">The extent that would be declared.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <summary>Refused: a movement belongs with the offset, ahead of the headings.</summary>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage AfterBlankRows() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage AfterBlankColumns() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage SkipToFirstNonBlankCell() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);
  }

  /// <summary>The scoped twin of <see cref="HeadingStage"/>, refusals and all.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public sealed class HeadingStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ISpace
  {
    private readonly Steps _placement;
    private readonly string[] _headings;

    internal HeadingStage(Steps placement, string[] headings)
      : base(placement.Before(Step.Headings(Headings.Captions(headings))))
    {
      _placement = placement;
      _headings = headings;
    }

    /// <inheritdoc cref="HeadingStage.Heading(string)"/>
    /// <param name="text">What the heading row says.</param>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(_placement, Headings.And(_headings, text));

    /// <inheritdoc cref="HeadingStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored on.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="HeadingStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored on.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> On(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="HeadingStage.On(IRowLandmark)"/>
    /// <param name="landmark">The row that would be anchored below.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="HeadingStage.On(IRowLandmark)"/>
    /// <param name="landmark">The column that would be anchored right of.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="HeadingStage.On(IRowLandmark)"/>
    /// <param name="offset">The offset that would replace the pipeline's.</param>
    [Obsolete(PipelineRefusals.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(PipelineRefusals.HeadingIsTheAnchor);

    /// <inheritdoc cref="HeadingStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The row the extent would stop before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage<TSpace> Until(IRowLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <inheritdoc cref="HeadingStage.Until(IRowLandmark, bool)"/>
    /// <param name="landmark">The column the extent would stop before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage<TSpace> UntilColumn(IColumnLandmark landmark, bool orEnd = false)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <inheritdoc cref="HeadingStage.Until(IRowLandmark, bool)"/>
    /// <param name="area">The extent that would be declared.</param>
    [Obsolete(PipelineRefusals.GeometryComesBeforeTheHeadings, error: true)]
    public HeadingStage<TSpace> Sized(IAreaStrategy area)
      => throw new NotSupportedException(PipelineRefusals.GeometryComesBeforeTheHeadings);

    /// <inheritdoc cref="HeadingStage.Down(int)"/>
    /// <param name="rows">How far down.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage<TSpace> Down(int rows) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="HeadingStage.Down(int)"/>
    /// <param name="columns">How far right.</param>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage<TSpace> Right(int columns) => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="HeadingStage.Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage<TSpace> AfterBlankRows() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="HeadingStage.Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage<TSpace> AfterBlankColumns() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);

    /// <inheritdoc cref="HeadingStage.Down(int)"/>
    [Obsolete(PipelineRefusals.OffsetComesFirst, error: true)]
    public HeadingStage<TSpace> SkipToFirstNonBlankCell() => throw new NotSupportedException(PipelineRefusals.OffsetComesFirst);
  }

  /// <summary>The three operations both heading stages need, written once.</summary>
  internal static class Headings
  {
    internal static string[] One(string text) => new[] { NotBlank(text) };

    internal static string[] And(string[] headings, string text)
    {
      var next = new string[headings.Length + 1];

      Array.Copy(headings, next, headings.Length);
      next[headings.Length] = NotBlank(text);

      return next;
    }

    /// <summary>
    /// The heading texts as the leaves the replay hands to <c>Under</c>. This is the one place a
    /// caption is still built for a value nobody reads, and it is inside the library rather than in
    /// a declaration — which is the whole of what the word moves.
    /// </summary>
    internal static IProjection<string>[] Captions(string[] headings)
    {
      var captions = new IProjection<string>[headings.Length];

      for (var index = 0; index < headings.Length; index++)
        captions[index] = Projection.Caption(headings[index]);

      return captions;
    }

    private static string NotBlank(string text)
      => string.IsNullOrWhiteSpace(text)
        ? throw new ArgumentException(
          "A heading is the text a section announces itself by, so it cannot be blank.",
          nameof(text))
        : text;
  }
}
