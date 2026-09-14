using System;

using Unrect.Core;
using Unrect.Projections;

namespace PlacementGauntlet.Staged
{
  /// <summary>
  /// SPIKE — <b>HEADING, the fossil pair dissolved.</b> One stage word in place of
  /// <c>Under</c> + <c>Caption</c>.
  /// <para>
  /// The diagnosis it answers: <c>Under</c> was a postfix modifier whose TYPE changed to content
  /// while its SEAT did not, and <c>Caption</c> was shoehorned into value-yielding-leaf because no
  /// category existed for <i>located, consumed, asserted structure that yields nothing</i>. The
  /// stage calculus is that missing category, so a heading is a stage — and taking the TEXT rather
  /// than a <c>Caption</c> leaf is not a convenience, it is the cure: a leaf argument would keep the
  /// fossil alive as a value that is constructed only to be thrown away.
  /// </para>
  /// <para>
  /// <b>It is a spelling, not a semantics.</b> The terminal replays
  /// <c>projection.Under(Caption(text), …)</c> as the INNERMOST step, which is the machinery the
  /// <c>Under</c> entry already built and the geography trial verified at L3. Chained headings
  /// accumulate into ONE replayed call rather than nesting — <c>Heading(a).Heading(b)</c> is
  /// <c>Under(Caption(a), Caption(b))</c>, one flow, not two — which is why this stage keeps the
  /// placement it was handed separate from the headings and recomposes the step each time.
  /// </para>
  /// <para>
  /// <b>Self-anchoring.</b> A heading locates its own section by content, so it is an entry in its
  /// own right; and it is a transition from the anchor, offset, bound and size stages, in ruling 1's
  /// canonical order — anchors/offsets, then bounds/sizes, then headings, then the subject. Nothing
  /// leads back: an anchor after a heading is refused with the same teaching message its
  /// <c>Under</c> predecessor carried.
  /// </para>
  /// </summary>
  public sealed class HeadingStage : PlacementStage
  {
    internal const string HeadingIsTheAnchor =
      "a section announced by a heading is already located by it: Heading finds the row by content, asserts "
      + "the text and consumes it. If the section needs an anchor as well — the repeat-stop recipe, where the "
      + "anchor must sit on the heading-and-content flow — declare it as the pipeline's entry, which reads "
      + "first because it is furthest up the sheet: On(mark).Heading(\"…\").Of(section).";

    private readonly Steps _placement;
    private readonly string[] _headings;

    internal HeadingStage(Steps placement, string[] headings)
      : base(placement.Before(Step.Under(Headings.Captions(headings))))
    {
      _placement = placement;
      _headings = headings;
    }

    /// <summary>
    /// The next heading DOWN the sheet — <c>Heading("IRR Details").Heading("Cash Flows Using
    /// Transfer Date")</c>, in document order, both above the subject.
    /// </summary>
    public HeadingStage Heading(string text) => new HeadingStage(_placement, Headings.And(_headings, text));

    [Obsolete(HeadingIsTheAnchor, error: true)]
    public HeadingStage On(IRowLandmark landmark) => throw new NotSupportedException(HeadingIsTheAnchor);

    [Obsolete(HeadingIsTheAnchor, error: true)]
    public HeadingStage Below(IRowLandmark landmark) => throw new NotSupportedException(HeadingIsTheAnchor);

    [Obsolete(HeadingIsTheAnchor, error: true)]
    public HeadingStage RightOf(IColumnLandmark landmark) => throw new NotSupportedException(HeadingIsTheAnchor);

    [Obsolete(HeadingIsTheAnchor, error: true)]
    public HeadingStage OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(HeadingIsTheAnchor);
  }

  /// <summary>The scoped twin of <see cref="HeadingStage"/>, refusals and all.</summary>
  /// <typeparam name="TSpace">What the pipeline's declaration demands of the space.</typeparam>
  public sealed class HeadingStage<TSpace> : PlacementStage<TSpace>
    where TSpace : class, ICellValues
  {
    private readonly Steps _placement;
    private readonly string[] _headings;

    internal HeadingStage(Steps placement, string[] headings)
      : base(placement.Before(Step.Under(Headings.Captions(headings))))
    {
      _placement = placement;
      _headings = headings;
    }

    /// <inheritdoc cref="HeadingStage.Heading(string)"/>
    public HeadingStage<TSpace> Heading(string text) => new HeadingStage<TSpace>(_placement, Headings.And(_headings, text));

    [Obsolete(HeadingStage.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> On(IRowLandmark landmark) => throw new NotSupportedException(HeadingStage.HeadingIsTheAnchor);

    [Obsolete(HeadingStage.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> Below(IRowLandmark landmark) => throw new NotSupportedException(HeadingStage.HeadingIsTheAnchor);

    [Obsolete(HeadingStage.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> RightOf(IColumnLandmark landmark) => throw new NotSupportedException(HeadingStage.HeadingIsTheAnchor);

    [Obsolete(HeadingStage.HeadingIsTheAnchor, error: true)]
    public HeadingStage<TSpace> OffsetBy(IOffsetStrategy offset) => throw new NotSupportedException(HeadingStage.HeadingIsTheAnchor);
  }

  /// <summary>The two operations both heading stages need, written once.</summary>
  internal static class Headings
  {
    internal static string[] One(string text) => new[] { NotEmpty(text) };

    internal static string[] And(string[] headings, string text)
    {
      var next = new string[headings.Length + 1];

      Array.Copy(headings, next, headings.Length);
      next[headings.Length] = NotEmpty(text);

      return next;
    }

    /// <summary>
    /// The heading texts as the leaves the replay hands to <c>Under</c>. This is the ONE place
    /// <c>Caption</c> is still constructed for the discard case, and it is inside the library rather
    /// than in a declaration — which is the whole of what the dissolution moves.
    /// </summary>
    internal static IProjection<string>[] Captions(string[] headings)
    {
      var captions = new IProjection<string>[headings.Length];

      for (var i = 0; i < headings.Length; i++)
        captions[i] = Projection.Caption(headings[i]);

      return captions;
    }

    private static string NotEmpty(string text)
      => string.IsNullOrWhiteSpace(text)
        ? throw new ArgumentException("A heading is the text a section announces itself by, so it cannot be blank.", nameof(text))
        : text;
  }
}
