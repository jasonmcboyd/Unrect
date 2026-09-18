using System;
using System.IO;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The one code path that applies a projection's placement — exactly once, at every level,
  /// including the top-level <c>Map</c> call. Because <see cref="IProjectionDefinition{TSpace, TResult}.Project"/> is
  /// handed the resolved extent, no projection can see or re-apply an offset.
  /// </summary>
  public static class ProjectionEngine
  {
    // Test-only, and the whole of the switch: the differential suite runs a declaration twice, once
    // with its bounds discovered as they are consumed and once with every extent measured up front,
    // and asserts the two agree — values, consumed extents and diagnostics alike. [ThreadStatic]
    // rather than a plain static because the test suite runs classes in parallel and a
    // decomposition is synchronous: the setting reaches exactly the Map calls the setting thread
    // makes.
    [ThreadStatic]
    private static bool _forcedEager;

    // The other switch: the whole suite through the push interpreter instead. Off by default;
    // UNRECT_PUSH=1 in the environment flips the default for a run, and UsePush() flips it for a
    // scope. Consulted only where an application begins — Map, Apply, MapWithDiagnostics — so a
    // machine that falls back to eager placement still uses this engine's placement underneath.
    [ThreadStatic]
    private static bool? _push;

    private static readonly bool PushByDefault = Environment.GetEnvironmentVariable("UNRECT_PUSH") == "1";

    /// <summary>Whether an application that begins now runs on the push interpreter.</summary>
    internal static bool Pushing => _push ?? PushByDefault;

    /// <summary>Runs every application begun in the scope on the push interpreter.</summary>
    internal static IDisposable UsePush() => new PushScope(true);

    /// <summary>Runs every application begun in the scope on the pull interpreter.</summary>
    internal static IDisposable UsePull() => new PushScope(false);

    private sealed class PushScope : IDisposable
    {
      private readonly bool? _previous;

      public PushScope(bool push)
      {
        _previous = _push;
        _push = push;
      }

      public void Dispose() => _push = _previous;
    }

    /// <summary>
    /// Resolves <paramref name="projection"/>'s placement against <paramref name="availableSpace"/>
    /// and projects it. Strict: a placement that does not fit throws rather than signalling failure
    /// to the caller — use <c>TryApply</c> where running out of space is expected.
    /// </summary>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, TSpace availableSpace, ProjectionContext context)
      where TSpace : class, ISpace
      => Apply(projection, Plane<TSpace>.Of(availableSpace), context);

    /// <inheritdoc cref="Apply{TSpace, TResult}(IProjectionDefinition{TSpace, TResult}, TSpace, ProjectionContext)"/>
    internal static AppliedResult<TResult> Apply<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, Plane<TSpace> availableSpace, ProjectionContext context)
      where TSpace : class, ISpace
      => Project(projection, Place(projection, availableSpace, context));

    /// <summary>
    /// Applies the projection unless its own placement does not fit, which is a repeat's stopping
    /// condition. Failures deeper inside the projection — a nested misfit, a projection that throws
    /// — still propagate: format drift inside a block is an error, not a quiet truncation.
    /// </summary>
    public static bool TryApply<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, TSpace availableSpace, ProjectionContext context, out AppliedResult<TResult> result)
      where TSpace : class, ISpace
      => TryApply(projection, Plane<TSpace>.Of(availableSpace), context, out result);

    /// <inheritdoc cref="TryApply{TSpace, TResult}(IProjectionDefinition{TSpace, TResult}, TSpace, ProjectionContext, out AppliedResult{TResult})"/>
    internal static bool TryApply<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, Plane<TSpace> availableSpace, ProjectionContext context, out AppliedResult<TResult> result)
      where TSpace : class, ISpace
    {
      if (!TryPlace(projection, availableSpace, context, strict: false, out var placed))
      {
        result = default;
        return false;
      }

      result = Project(projection, placed);
      return true;
    }

    private static Placed<TSpace> Place<TSpace>(IProjectionDefinition projection, Plane<TSpace> availableSpace, ProjectionContext context)
      where TSpace : class, ISpace
    {
      // Unreachable: TryPlace(strict: true) throws on every path that would return false. It stays
      // because it is the assertion that keeps the two modes' contract visible at the call site —
      // if a future branch forgets to throw, this is what says so instead of a null extent later.
      if (!TryPlace(projection, availableSpace, context, strict: true, out var placed))
        throw new InvalidOperationException("A strict placement must throw rather than fail.");

      return placed;
    }

    /// <summary>
    /// Resolves the projection's own placement. Running out of space is a stopping condition when
    /// <paramref name="strict"/> is false (that is what a repeat asks for); every other way a
    /// strategy can fail is a malformed declaration and throws either way.
    /// </summary>
    private static bool TryPlace<TSpace>(IProjectionDefinition projection, Plane<TSpace> availableSpace, ProjectionContext context, bool strict, out Placed<TSpace> placed)
      where TSpace : class, ISpace
    {
      placed = default;

      Offset offset;
      try
      {
        offset = projection.Placement.Offset.GetOffset(availableSpace.Erased());
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw context.Failure(projection, Missing(exception), availableSpace, null, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw context.Failure(projection, Threw("offset", exception), availableSpace, null, exception, IsFault(exception));
      }

      if (Exceeds(offset.Size, availableSpace))
      {
        if (strict)
          throw context.Failure(projection, $"an offset of {Describe(offset.Size)} does not fit the available space", availableSpace, offset.Size, null);

        return false;
      }

      var inner = availableSpace.Slice(offset);
      // A projection the path skips is not entered — it contributes no segment — so it reports
      // against whatever context it was called with. At the root there is nothing in that context to
      // report against, so the root is told who it is applying instead.
      var scope = PathRenderer.Skipped(projection) ? context.Blaming(projection) : context.Descend(projection);

      if (projection.Placement.Area is null)
      {
        placed = Announce(offset, inner, scope, hasDeclaredArea: false);
        return true;
      }

      // Minted once and shared by the scan, the bound and the area strategy. They have to name the
      // SAME region: a scan replays its state against the region it was begun with, and a bound reads
      // its ceiling off the region it was built with — hand those two different regions and a nested
      // discovery resumes on one its parent had already excluded rows from.
      var innerSpace = inner.Erased();

      if (Bind(projection, inner, innerSpace, scope, strict) is Bound bound)
      {
        placed = Announce(offset, inner.Bounded(bound, bound.Width), scope, hasDeclaredArea: true);
        return true;
      }

      Area area;
      try
      {
        area = projection.Placement.Area.GetArea(innerSpace);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw AreaFailure(scope, projection, inner, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw AreaFailure(scope, projection, inner, exception);
      }

      if (Exceeds(area.Size, inner))
      {
        if (strict)
          throw scope.Failure(projection, $"an extent of {Describe(area.Size)} does not fit here", inner, area.Size, null);

        return false;
      }

      placed = Announce(offset, inner.Slice(area), scope, hasDeclaredArea: true);
      return true;
    }

    /// <summary>
    /// The extent as a bound to be discovered while the projection consumes it, or null where it
    /// must be measured up front. Two conditions, both hard:
    /// <list type="number">
    /// <item>the strategy says its bound is a per-row rule, by implementing <see cref="IIncrementalAreaStrategy"/>; and</item>
    /// <item>the placement is strict. A repeat stops when its item's <em>placement</em> fails,
    /// and a failure deferred into the projection would arrive after the item had been collected —
    /// so a non-strict placement is always measured up front.</item>
    /// </list>
    /// Beginning the scan is the strategy call this replaces, so it fails exactly as measuring
    /// would.
    /// <para>
    /// <b>What this buys, and where it stops.</b> A composite streams over a bound: placing a child
    /// asks <see cref="Exceeds"/> whether there is a row at the offset, and slices the extent with
    /// <see cref="Plane{TSpace}.Slice(Offset)"/>, which keeps an unsettled height unsettled. What still
    /// settles a bound in full is a strategy reading <see cref="ISpace.Area"/> — which is what a
    /// DECLARED area on the child is, since the strategy is handed the extent and asks it how tall
    /// it is. So a shape that knows its own shape slices before it declares: a tiler cuts a band of
    /// its stride and hands that measured band down, where a <c>.Sized(RowsWhileAnyValue())</c>
    /// child of the same flow measures the whole tail first.
    /// </para>
    /// </summary>
    private static Bound? Bind<TSpace>(IProjectionDefinition projection, Plane<TSpace> inner, Plane<ISpace> innerSpace, ProjectionContext scope, bool strict)
      where TSpace : class, ISpace
    {
      if (!strict || _forcedEager || projection.Placement.Area is not IIncrementalAreaStrategy incremental)
        return null;

      IAreaScan scan;
      try
      {
        scan = incremental.BeginArea(innerSpace);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw AreaFailure(scope, projection, inner, exception);
      }

      // A scan's height is bounded by the rows there are — that is what its eager reading is
      // defined as — so the only way a discovered extent can fail to fit is its width, and saying
      // so needs the height the eager reading would have measured. Declining to bind hands that one
      // case to the measured path below, which is the only place that reports it.
      if (scan.Width > inner.Width)
        return null;

      return new Bound(innerSpace, scan, exception => AreaFailure(scope, projection, inner, exception));
    }

    /// <summary>
    /// The placed region, with the band it opens announced to a space that asked to hear about one
    /// (<see cref="ISweepAware"/>) — once per placement, where the region is cut.
    /// <para>
    /// What is announced is the extent the placement <em>declared</em>, never the one a discovered
    /// bottom edge settles to: asking a region still being discovered how tall it is would read the
    /// file to find out, which is the cost a backend keeps the announcement for.
    /// </para>
    /// </summary>
    private static Placed<TSpace> Announce<TSpace>(Offset offset, Plane<TSpace> extent, ProjectionContext scope, bool hasDeclaredArea)
      where TSpace : class, ISpace
    {
      if (extent.Space is ISweepAware sweeping)
        sweeping.Sweeping(extent.Origin, extent.Declared);

      return new Placed<TSpace>(offset, extent, scope, hasDeclaredArea);
    }

    private static AppliedResult<TResult> Project<TSpace, TResult>(IProjectionDefinition<TSpace, TResult> projection, Placed<TSpace> placed)
      where TSpace : class, ISpace
    {
      ProjectionResult<TResult> result;
      try
      {
        result = projection.Project(placed.Extent, placed.Scope);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw placed.Scope.Failure(
          projection,
          $"the projection threw {exception.GetType().Name}: {exception.Message}",
          placed.Extent,
          null,
          exception,
          IsFault(exception));
      }

      // A declared area is consumed in full, even when the projection used less of it — which is
      // where a bound that was left to be discovered is read to exhaustion. A projection that read
      // every row has already settled it, so the canonical case forces nothing twice.
      var consumed = placed.HasDeclaredArea ? placed.Extent.Area.Size : result.Consumed;

      return new AppliedResult<TResult>(result.Value, placed.Offset, consumed, Settled(result.Presence, placed.HasDeclaredArea, consumed));
    }

    /// <summary>
    /// A declared extent that settled at nothing — <c>Range(RowsWhileAnyValue(), …)</c> over a band
    /// with no valued row — looked and found zero, which is <see cref="Presence.Empty"/> rather than
    /// the <see cref="Presence.Read"/> a projection over an empty region reports by saying nothing.
    /// It is read off the settled extent rather than off the strategy, so the two doors and the two
    /// forcing modes cannot disagree about it.
    /// <para>
    /// A projection that already said what it was — a boundary's <see cref="Presence.Absorbed"/>, a
    /// repetition's own <see cref="Presence.Empty"/> — outranks this, because it knows why.
    /// </para>
    /// </summary>
    internal static Presence Settled(Presence presence, bool hasDeclaredArea, Size consumed)
      => presence == Presence.Read && hasDeclaredArea && (consumed.Width == 0 || consumed.Height == 0)
        ? Presence.Empty
        : presence;

    /// <summary>
    /// How a declared extent's failure is reported: a strategy that ran out of room says so, and
    /// one that broke says what broke.
    /// <para>
    /// Shared with <see cref="Bound"/>, which is handed it as the identity of the placement
    /// whose bound it discovers. That is what makes a deferred failure the placement's failure: the
    /// subject, path, location and fault flag are not merely alike, they are produced by this one
    /// expression from the same scope, projection and space. Only the moment differs.
    /// </para>
    /// </summary>
    internal static ProjectionException AreaFailure<TSpace>(ProjectionContext scope, IProjectionDefinition projection, Plane<TSpace> inner, Exception exception)
      where TSpace : class, ISpace
      => exception is OutOfBoundsException
        ? scope.Failure(projection, "its area ran past the space available here", inner, null, exception)
        : scope.Failure(projection, Threw("area", exception), inner, null, exception, IsFault(exception));

    /// <summary>
    /// Whether something broke rather than disagreed with the data. These mean the code is wrong or
    /// the environment failed — a null bug, a bad index into an array or a view, a disk that
    /// stopped answering, a workbook read after its owner was disposed — so no tolerance boundary
    /// may quietly swallow them. Everything else — a cell of the wrong kind, an unparseable value,
    /// an overflow — is the sort of failure tolerance is for.
    /// <para>
    /// It is consulted at every site where the engine wraps a foreign exception, not just the
    /// projection, and that is the point. A strategy reads cells too: under streaming, a disk read
    /// failing inside <c>SkipBlankRows</c> within <c>section.Optional()</c> would otherwise be
    /// reported as "section absent", with a warning, and the parse would continue and produce a
    /// quietly wrong answer.
    /// </para>
    /// <para>
    /// The membership is deliberate on both sides. <see cref="System.IO.FileNotFoundException"/>,
    /// <see cref="System.IO.DirectoryNotFoundException"/> and the reader's own IO failures derive
    /// from <see cref="IOException"/> and are covered. <see cref="ObjectDisposedException"/>
    /// derives from <see cref="InvalidOperationException"/>, which is <em>not</em> listed and must
    /// not be — parse helpers throw that for data reasons — so it is named explicitly.
    /// <see cref="InvalidCastException"/> is listed because a cast is a claim about types rather
    /// than about the data — this library's own casts are invariants it owes itself, and a map
    /// function's cast of its own result is the reader's claim about the reader's types. Either way
    /// a failed one says the code is wrong, never that a section is missing.
    /// <see cref="ArgumentException"/> itself stays absorbable, for the same reason in reverse: a
    /// parse helper throws it about the data.
    /// <see cref="OutOfBoundsException"/> is not here at all: running out of room is how a
    /// repeat stops, and no IO condition produces it.
    /// </para>
    /// </summary>
    internal static bool IsFault(Exception exception)
      => exception is NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException
        or IOException                 // the disk, the network share, the workbook replaced mid-read
        or ObjectDisposedException     // a view outliving its Workbook
        or InvalidCastException        // an invariant this library owes itself; never the data
        or OutOfMemoryException;       // never a statement about the data

    // A note on the last one: under a genuine out-of-memory condition the wrap itself may fail to
    // allocate, and the original exception then escapes unwrapped. That is fine and is not a hole —
    // an unwrapped OutOfMemoryException is not a ProjectionException, so no tolerance boundary
    // catches it either. The property that matters holds by both routes: it is never absorbed.

    // Asked as "is there a row at size.Height - 1" rather than "how tall are you": the same answer
    // on a measured extent, and one row rather than all of them on one still being discovered.
    internal static bool Exceeds<TSpace>(Size size, Plane<TSpace> space)
      where TSpace : class, ISpace
      => size.Width > space.Width
      || (size.Height > 0 && !space.HasRow(size.Height - 1));

    internal static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    // A matcher that found nothing says what it was looking for; anything else just ran out of
    // room.
    internal static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";

    private readonly struct Placed<TSpace>
      where TSpace : class, ISpace
    {
      public Placed(Offset offset, Plane<TSpace> extent, ProjectionContext scope, bool hasDeclaredArea)
      {
        Offset = offset;
        Extent = extent;
        Scope = scope;
        HasDeclaredArea = hasDeclaredArea;
      }

      public Offset Offset { get; }
      public Plane<TSpace> Extent { get; }
      public ProjectionContext Scope { get; }
      public bool HasDeclaredArea { get; }
    }

    /// <summary>
    /// Test-only. Measures every declared extent up front on the calling thread, as the engine did
    /// before bounds could be discovered, until the returned scope is disposed. Nested scopes
    /// restore rather than clear, so it composes with itself.
    /// </summary>
    internal static IDisposable ForceEager() => new EagerScope();

    private sealed class EagerScope : IDisposable
    {
      private readonly bool _previous;

      public EagerScope()
      {
        _previous = _forcedEager;
        _forcedEager = true;
      }

      public void Dispose() => _forcedEager = _previous;
    }
  }
}
