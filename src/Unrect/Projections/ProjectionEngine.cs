using System;
using System.IO;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The one code path that applies a projection's placement — exactly once, at every level,
  /// including the top-level <c>Map</c> call. Because <see cref="IProjection{TResult}.Project"/> is
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

    /// <summary>
    /// Resolves <paramref name="projection"/>'s placement against <paramref name="availableSpace"/>
    /// and projects it. Strict: a placement that does not fit throws rather than signalling failure
    /// to the caller — use <see cref="TryApply{TResult}(IProjection{TResult}, ICellValues, ProjectionContext, out AppliedResult{TResult})"/> where running out of space is expected.
    /// </summary>
    public static AppliedResult<TResult> Apply<TResult>(IProjection<TResult> projection, ICellValues availableSpace, ProjectionContext context)
      => Apply(projection, availableSpace.Extent(), context);

    /// <inheritdoc cref="Apply{TResult}(IProjection{TResult}, ICellValues, ProjectionContext)"/>
    internal static AppliedResult<TResult> Apply<TResult>(IProjection<TResult> projection, Plane<ICellValues> availableSpace, ProjectionContext context)
      => Project(projection, Place(projection, availableSpace, context));

    /// <summary>
    /// Applies the projection unless its own placement does not fit, which is a repeat's stopping
    /// condition. Failures deeper inside the projection — a nested misfit, a projection that throws
    /// — still propagate: format drift inside a block is an error, not a quiet truncation.
    /// </summary>
    public static bool TryApply<TResult>(IProjection<TResult> projection, ICellValues availableSpace, ProjectionContext context, out AppliedResult<TResult> result)
      => TryApply(projection, availableSpace.Extent(), context, out result);

    /// <inheritdoc cref="TryApply{TResult}(IProjection{TResult}, ICellValues, ProjectionContext, out AppliedResult{TResult})"/>
    internal static bool TryApply<TResult>(IProjection<TResult> projection, Plane<ICellValues> availableSpace, ProjectionContext context, out AppliedResult<TResult> result)
    {
      if (!TryPlace(projection, availableSpace, context, strict: false, out var placed))
      {
        result = default;
        return false;
      }

      result = Project(projection, placed);
      return true;
    }

    private static Placed Place(IProjection projection, Plane<ICellValues> availableSpace, ProjectionContext context)
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
    private static bool TryPlace(IProjection projection, Plane<ICellValues> availableSpace, ProjectionContext context, bool strict, out Placed placed)
    {
      placed = default;

      Offset offset;
      try
      {
        offset = projection.Placement.Offset.GetOffset(availableSpace.AsCanonical());
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

      var inner = availableSpace.Tail(offset);
      var scope = projection.IsTransparent ? context.Advance(offset) : context.Descend(projection, offset);

      if (projection.Placement.Area is null)
      {
        placed = new Placed(offset, inner, scope, false);
        return true;
      }

      // Minted once and shared by the scan, the bound and the area strategy. They have to name the
      // SAME region: a scan replays its state against the region it was begun with, and a bound reads
      // its ceiling off the region it was built with — hand those two different regions and a nested
      // discovery resumes on one its parent had already excluded rows from.
      var innerSpace = inner.AsCanonical();

      if (Bind(projection, inner, innerSpace, scope, strict) is Bound bound)
      {
        placed = new Placed(offset, inner.Bounded(bound, bound.Width), scope, hasDeclaredArea: true);
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

      placed = new Placed(offset, inner.Cut(area), scope, true);
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
    /// <see cref="Extents.Tail"/>, which keeps an unsettled height unsettled. What still
    /// settles a bound in full is a strategy reading <see cref="ISpace.Area"/> — which is what a
    /// DECLARED area on the child is, since the strategy is handed the extent and asks it how tall
    /// it is. So a shape that knows its own shape slices before it declares: a tiler cuts a band of
    /// its stride and hands that measured band down, where a <c>.Sized(RowsWhileAnyValue())</c>
    /// child of the same flow measures the whole tail first.
    /// </para>
    /// </summary>
    private static Bound? Bind(IProjection projection, Plane<ICellValues> inner, Plane<ISpace> innerSpace, ProjectionContext scope, bool strict)
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

    private static AppliedResult<TResult> Project<TResult>(IProjection<TResult> projection, Placed placed)
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
    private static Presence Settled(Presence presence, bool hasDeclaredArea, Size consumed)
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
    private static ProjectionException AreaFailure(ProjectionContext scope, IProjection projection, Plane<ICellValues> inner, Exception exception)
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
    /// The membership is deliberate on both sides. <see cref="EngineInvariantException"/> is here
    /// because it is the one entry that is not about the environment at all: it says this library
    /// broke a rule it owes itself, and a tolerance boundary that swallowed one would report a bug
    /// in the reader as a section that is not there. <see cref="System.IO.FileNotFoundException"/>,
    /// <see cref="System.IO.DirectoryNotFoundException"/> and the reader's own IO failures derive
    /// from <see cref="IOException"/> and are covered. <see cref="ObjectDisposedException"/>
    /// derives from <see cref="InvalidOperationException"/>, which is <em>not</em> listed and must
    /// not be — parse helpers throw that for data reasons — so it is named explicitly.
    /// <see cref="MissingCapabilityException"/> derives from it too and is listed for the same
    /// reason and to the same end: a boundary that could not look has said nothing about the
    /// document, and reporting it as an absent section is exactly the swap this list prevents.
    /// <see cref="ArgumentException"/> itself stays absorbable, for the same reason.
    /// <see cref="OutOfBoundsException"/> is not here at all: running out of room is how a
    /// repeat stops, and no IO condition produces it.
    /// </para>
    /// </summary>
    internal static bool IsFault(Exception exception)
      => exception is EngineInvariantException  // a rule this library owes itself; never the data
        or NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException
        or IOException                 // the disk, the network share, the workbook replaced mid-read
        or ObjectDisposedException     // a view outliving its Workbook
        or MissingCapabilityException  // a boundary that could not look; never "not there"
        or OutOfMemoryException;       // never a statement about the data

    // A note on the last one: under a genuine out-of-memory condition the wrap itself may fail to
    // allocate, and the original exception then escapes unwrapped. That is fine and is not a hole —
    // an unwrapped OutOfMemoryException is not a ProjectionException, so no tolerance boundary
    // catches it either. The property that matters holds by both routes: it is never absorbed.

    // Asked as "is there a row at size.Height - 1" rather than "how tall are you": the same answer
    // on a measured extent, and one row rather than all of them on one still being discovered.
    private static bool Exceeds(Size size, Plane<ICellValues> space)
      => size.Width > space.Width
      || (size.Height > 0 && !space.HasRow(size.Height - 1));

    private static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    // A matcher that found nothing says what it was looking for; anything else just ran out of
    // room.
    private static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";

    private readonly struct Placed
    {
      public Placed(Offset offset, Plane<ICellValues> extent, ProjectionContext scope, bool hasDeclaredArea)
      {
        Offset = offset;
        Extent = extent;
        Scope = scope;
        HasDeclaredArea = hasDeclaredArea;
      }

      public Offset Offset { get; }
      public Plane<ICellValues> Extent { get; }
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
