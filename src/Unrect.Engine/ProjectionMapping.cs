using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Applying a definition to a space: the engine's door, and the run behind it. <see cref="Map"/>
  /// hands back the value, <see cref="Apply"/> the value with what it consumed,
  /// <see cref="MapWithDiagnostics"/> the value with everything the run had to say. A run is one
  /// forward pass over the space's rows: each offered to the root machine in order, and, over a
  /// streamed source, released once no open machine may still read it.
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

      return Run(projection, space, SessionScope<TSpace>.Root(space));
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
      var (applied, diagnostics) = ApplyWithDiagnostics(projection, space);

      return new MapResult<TResult>(applied.Value, diagnostics);
    }

    /// <summary>
    /// <see cref="Apply{TSpace, TResult}"/> together with everything the run noticed, unconsumed
    /// space included — what <see cref="MapWithDiagnostics{TSpace, TResult}"/> hands back the value
    /// half of, and what a test observes whole.
    /// </summary>
    internal static (AppliedResult<TResult> Applied, IReadOnlyList<ProjectionDiagnostic> Diagnostics) ApplyWithDiagnostics<TSpace, TResult>(
      IProjectionDefinition<TSpace, TResult> projection,
      TSpace space)
      where TSpace : class, ISpace
    {
      if (projection is null)
        throw new ArgumentNullException(nameof(projection));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var scope = SessionScope<TSpace>.Root(space);
      var mark = scope.Diagnostics.Mark();
      var extent = Plane<TSpace>.Of(space);
      var applied = Run(projection, space, scope);

      // Suppressed only when the whole parse is one absorbed failure: two boundaries that each
      // absorbed something have left a gap worth mentioning, even though neither consumed anything.
      if (!(applied.Advance.Width == 0 && applied.Advance.Height == 0 && scope.Diagnostics.AbsorbedAt(mark)))
        ProjectionExtensions.ReportUnconsumed(projection, extent, applied.Offset.Size, applied.Consumed, scope);

      return (applied, scope.Diagnostics.Snapshot());
    }

    /// <summary>
    /// The run: <paramref name="definition"/>'s rows pushed at the machine it builds. A space that
    /// is an <see cref="IRowFeed"/> is fed as its rows arrive, and after every row the feed is told
    /// what the open machines still hold, so it may drop the rest; any other space retains its rows
    /// and is simply cut into them.
    /// </summary>
    private static AppliedResult<TResult> Run<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> definition, TSpace space, SessionScope<TSpace> scope)
      where TSpace : class, ISpace
    {
      var whole = Plane<TSpace>.Of(space);
      var root = scope.Start(new Child(definition, default), definition, scope.Anchor);

      if (space is IRowFeed feed)
      {
        var width = whole.Width;

        // Row-indexed rather than driven off what the feed has loaded: a direct read inside a
        // machine may have loaded rows ahead of the offer, and every one of them is still offered.
        for (var row = 0; Load(feed, row, definition, whole, scope); row++)
        {
          var span = whole.Slice(new Offset(0, row), new Area(width, 1));

          if (!root.Next(span))
            break;

          Trim(feed, root, definition, row, span, scope);
        }
      }
      else
      {
        foreach (var span in Spans.Of(whole, Orientation.Vertical))
          if (!root.Next(span))
            break;
      }

      var settlement = root.Close();

      return new AppliedResult<TResult>(settlement.Value, root.Offset, settlement.Consumed);
    }

    /// <summary>
    /// Has the feed load <paramref name="row"/>, or says the source is exhausted. A source that
    /// throws — the disk, a file replaced mid-read — is a fault, never a statement about the data.
    /// </summary>
    private static bool Load<TSpace, TResult>(IRowFeed feed, int row, IProjectionDefinition<TSpace, TResult> definition, Plane<TSpace> whole, ProjectorScope<TSpace> scope)
      where TSpace : class, ISpace
    {
      while (feed.Loaded <= row)
      {
        bool more;

        try
        {
          more = feed.Advance();
        }
        catch (Exception exception) when (exception is not ProjectionException)
        {
          var at = row < whole.Area.Height ? whole.Slice(new Offset(0, row), new Area(whole.Width, 1)) : whole;

          throw scope.Failure(definition, $"the source threw {exception.GetType().Name}: {exception.Message}", at, null, exception, isFault: true);
        }

        if (!more)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Releases every row before the oldest one still needed, asked of the root and answered for
    /// the whole tree, then checks the cap: more rows held than the feed allows is a fault naming
    /// the innermost machine holding that oldest row — an enclosing boundary holds whatever its
    /// child holds, so blaming it would name the wrapper for the leaf's reach.
    /// </summary>
    private static void Trim<TSpace>(IRowFeed feed, IChildHandle<TSpace> root, IProjectionDefinition definition, int current, Plane<TSpace> span, ProjectorScope<TSpace> scope)
      where TSpace : class, ISpace
    {
      var hold = root.Retained(current);
      var oldest = hold?.Row ?? current;

      feed.Release(oldest);

      if (feed.Cap is int cap && feed.Retained > cap)
      {
        var who = hold is Hold held ? PathRenderer.Describe(held.Holder) : "the declaration";

        throw scope.Failure(
          hold?.Holder ?? definition,
          $"{who} is holding {feed.Retained} rows, from row {oldest + 1} through row {current + 1}, more than the {cap} the source allows: "
          + "the declaration asks for more than a forward pass can keep. Raise the source's buffer cap, or bound the shape that holds",
          span,
          null,
          null,
          isFault: true);
      }
    }
  }
}
