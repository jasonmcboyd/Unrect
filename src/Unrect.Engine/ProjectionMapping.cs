using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Applying a definition to a space: the engine's door. <see cref="Map"/> hands back the value,
  /// <see cref="Apply"/> the value with what it consumed, <see cref="MapWithDiagnostics"/> the value
  /// with everything the run had to say. A run is one forward pass over the space's rows.
  /// </summary>
  public static class ProjectionMapping
  {
    /// <summary>
    /// Decomposes <paramref name="space"/> and projects it in one call. The projection's own
    /// placement is applied here too, exactly as it would be nested inside another projection.
    /// <para>
    /// <paramref name="space"/> is the very type the declaration was written over, so a
    /// declaration that reads formulas will not compile against a grid that has none.
    /// </para>
    /// <para>
    /// Coordinates in failures are relative to <paramref name="space"/>, so a <c>Map</c> called
    /// from inside another projection's Project restarts them and reports positions relative to its
    /// own space. Compose projections instead of nesting <c>Map</c> calls wherever you can.
    /// </para>
    /// <para>
    /// The root of a path renders by description rather than by a name, deliberately: inferring one
    /// from the receiver would need an optional compiler-supplied parameter, and that would stop
    /// <c>Map</c> being usable as a method group — <c>spaces.Select(report.Map)</c>, one projection
    /// over many workbooks, is the reason this library exists. Name the root with <c>Named</c> if a
    /// path should carry it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static TResult Map<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
      => projection.Apply(space).Value;

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/> plus where the projection landed and how much it
    /// consumed.
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      return PushSession<TSpace>.Apply(projection, space, SessionScope<TSpace>.Root(space));
    }

    /// <summary>
    /// <see cref="Map{TSpace, TResult}"/>, keeping what the decomposition noticed: every tolerance
    /// boundary that absorbed a failure, every alternative a choice passed over, and space the
    /// projection did not describe. A failure nothing declared tolerance for still throws —
    /// declared tolerance is the only thing that ever softens a parse.
    /// <para>
    /// Space nothing described is reported as an <c>Info</c>, except where the entire parse was one
    /// absorbed failure: <c>projection.Optional().MapWithDiagnostics(space)</c> — tolerance
    /// declared at the root, the nearest thing to a lenient mode — would otherwise say "consumed 0
    /// of N rows" underneath a warning that already named the projection, the reason, and the cell.
    /// Anything else still reports, including a root that consumed nothing after absorbing in two
    /// places, or a repeat that found no sections at all.
    /// </para>
    /// <para>
    /// Diagnostics belong to one call: a <c>Map</c> nested inside a projection collects its own and
    /// discards them, so tolerance declared in there is invisible out here. Another reason to
    /// compose projections rather than nest calls.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the projection is written over.</typeparam>
    /// <typeparam name="TResult">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static MapResult<TResult> MapWithDiagnostics<TSpace, TResult>(this IProjectionDefinition<TSpace, TResult> projection, TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var scope = SessionScope<TSpace>.Root(space);
      var mark = scope.Diagnostics.Mark();
      var extent = Plane<TSpace>.Of(space);
      var applied = PushSession<TSpace>.Apply(projection, space, scope);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && scope.Diagnostics.AbsorbedAt(mark)))
        ProjectionExtensions.ReportUnconsumed(projection, extent, applied.Offset.Size, applied.Consumed, scope);

      return new MapResult<TResult>(applied.Value, scope.Diagnostics.Snapshot());
    }
  }
}
