namespace Unrect.Projections
{
  /// <summary>
  /// What kind of something — or of nothing — a projection made of the extent it was handed. It
  /// rides beside <see cref="ProjectionResult{T}.Consumed"/> because a consumed extent of zero says
  /// four different things at once: a repetition with no next item, a tolerance boundary that
  /// absorbed a failure, a region that is legitimately empty, and the trigger for the
  /// following-sibling note. The rules that used to infer which of those a zero meant read it here
  /// instead.
  /// <para>
  /// It is denotation metadata, not geometry: nothing about it changes what a projection consumes.
  /// </para>
  /// <para>
  /// Internal deliberately. The engine, the composites and the diagnostics use it; nothing public
  /// changes shape. "Was that section absent or empty?" is a real caller's question, but it is
  /// answered additively later, by a caller that exists.
  /// </para>
  /// </summary>
  internal enum Presence
  {
    /// <summary>
    /// Content was recognized: the region is real and non-vacuous. The default, so a projection
    /// that says nothing about itself is taken to have read what it was handed. Deliberately
    /// includes a blank-tolerant leaf that took a blank: it read a real 1x1 cell whose kind is
    /// Blank — <see cref="Empty"/> is reserved for zero-extent regions, so presence and extent
    /// never contradict.
    /// </summary>
    Read = 0,

    /// <summary>
    /// The declaration looked and legitimately found a zero-extent region — a discovered extent
    /// that settled at zero rows, a repetition that collected no items. "The data contained zero of
    /// these" is a fact about the data.
    /// </summary>
    Empty,

    /// <summary>
    /// A tolerance boundary exercised itself: nothing was read, and the extent is honestly unknown.
    /// This is the one that must never blur with <see cref="Empty"/> — empty looked and found zero;
    /// absorbed did not look.
    /// </summary>
    Absorbed,
  }
}
