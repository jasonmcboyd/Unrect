using System;
using System.IO;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// The one code path that applies a shape's placement — exactly once, at every level, including
  /// the top-level <c>Map</c> call. Because <see cref="IShape{TResult}.Project"/> is handed the
  /// resolved extent, no projection can see or re-apply an offset.
  /// </summary>
  public static class ShapeEngine
  {
    // Test-only, and the whole of the switch: the differential suite runs a declaration twice, once
    // with its bounds discovered as they are consumed and once with every extent measured up front,
    // and asserts the two agree — values, consumed extents and diagnostics alike. [ThreadStatic]
    // rather than a plain static because the test suite runs classes in parallel and a decomposition
    // is synchronous: the setting reaches exactly the Map calls the setting thread makes.
    [ThreadStatic]
    private static bool _forcedEager;

    /// <summary>
    /// Resolves <paramref name="shape"/>'s placement against <paramref name="availableSpace"/> and
    /// projects it. Strict: a placement that does not fit throws rather than signalling failure to
    /// the caller — use <see cref="TryApply{TResult}"/> where running out of space is expected.
    /// </summary>
    public static AppliedResult<TResult> Apply<TResult>(IShape<TResult> shape, ISpace availableSpace, ShapeContext context)
      => Project(shape, Place(shape, availableSpace, context));

    /// <summary>
    /// Applies the shape unless its own placement does not fit, which is <c>Repeat</c>'s stopping
    /// condition. Failures deeper inside the shape — a nested misfit, a projection that throws —
    /// still propagate: format drift inside a block is an error, not a quiet truncation.
    /// </summary>
    public static bool TryApply<TResult>(IShape<TResult> shape, ISpace availableSpace, ShapeContext context, out AppliedResult<TResult> result)
    {
      if (!TryPlace(shape, availableSpace, context, strict: false, out var placed))
      {
        result = default;
        return false;
      }

      result = Project(shape, placed);
      return true;
    }

    private static Placed Place(IShape shape, ISpace availableSpace, ShapeContext context)
    {
      // Unreachable: TryPlace(strict: true) throws on every path that would return false. It stays
      // because it is the assertion that keeps the two modes' contract visible at the call site —
      // if a future branch forgets to throw, this is what says so instead of a null extent later.
      if (!TryPlace(shape, availableSpace, context, strict: true, out var placed))
        throw new InvalidOperationException("A strict placement must throw rather than fail.");

      return placed;
    }

    /// <summary>
    /// Resolves the shape's own placement. Running out of space is a stopping condition when
    /// <paramref name="strict"/> is false (that is what <c>Repeat</c> asks for); every other way a
    /// strategy can fail is a malformed declaration and throws either way.
    /// </summary>
    private static bool TryPlace(IShape shape, ISpace availableSpace, ShapeContext context, bool strict, out Placed placed)
    {
      placed = default;

      Offset offset;
      try
      {
        offset = shape.Placement.Offset.GetOffset(availableSpace);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw context.Failure(shape, Missing(exception), availableSpace, null, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw context.Failure(shape, Threw("offset", exception), availableSpace, null, exception, IsFault(exception));
      }

      if (Exceeds(offset.Size, availableSpace))
      {
        if (strict)
          throw context.Failure(shape, $"an offset of {Describe(offset.Size)} does not fit the available space", availableSpace, offset.Size, null);

        return false;
      }

      var inner = availableSpace.GetSubspace(offset);
      var scope = shape.IsTransparent ? context.Advance(offset) : context.Descend(shape, offset);

      if (shape.Placement.Area is null)
      {
        placed = new Placed(offset, inner, scope, false);
        return true;
      }

      if (Bind(shape, inner, scope, strict) is BoundedSpace bound)
      {
        placed = new Placed(offset, bound, scope, hasDeclaredArea: true, bound: bound);
        return true;
      }

      Area area;
      try
      {
        area = shape.Placement.Area.GetArea(inner);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw AreaFailure(scope, shape, inner, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw AreaFailure(scope, shape, inner, exception);
      }

      if (Exceeds(area.Size, inner))
      {
        if (strict)
          throw scope.Failure(shape, $"an extent of {Describe(area.Size)} does not fit here", inner, area.Size, null);

        return false;
      }

      placed = new Placed(offset, inner.GetSubspace(area), scope, true);
      return true;
    }

    /// <summary>
    /// The extent as a bound to be discovered while the projection consumes it, or null where it must
    /// be measured up front. Two conditions, both hard:
    /// <list type="number">
    /// <item>the strategy says its bound is a per-row rule, by implementing <see cref="IIncrementalAreaStrategy"/>; and</item>
    /// <item>the placement is strict. A <c>Repeat</c> stops when its item's <em>placement</em> fails,
    /// and a failure deferred into the projection would arrive after the item had been collected — so
    /// a non-strict placement is always measured up front.</item>
    /// </list>
    /// Beginning the scan is the strategy call this replaces, so it fails exactly as measuring would.
    /// <para>
    /// <b>What this buys, and where it stops.</b> Only a LEAF extent stays unresolved for long. The
    /// moment a bound extent is handed to a composite, placing that composite's first child runs
    /// <see cref="Exceeds"/> against it and then slices it with <c>GetSubspace(offset)</c> — two
    /// questions about the parent's height, both asked before any child projects — so a
    /// <c>.Sized(RowsWhileAnyValue())</c> on a flow or an overlay resolves in full and immediately.
    /// Making those two sites bound-aware is future work, deliberately not attempted here; the limit
    /// is pinned by <c>LazyDenotationTests</c>' census so that lifting it has to be a decision.
    /// </para>
    /// </summary>
    private static BoundedSpace? Bind(IShape shape, ISpace inner, ShapeContext scope, bool strict)
    {
      if (!strict || _forcedEager || shape.Placement.Area is not IIncrementalAreaStrategy incremental)
        return null;

      IAreaScan scan;
      try
      {
        scan = incremental.BeginArea(inner);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw AreaFailure(scope, shape, inner, exception);
      }

      // A scan's height is bounded by the rows there are — that is what its eager reading is defined
      // as — so the only way a discovered extent can fail to fit is its width, and saying so needs
      // the height the eager reading would have measured. Declining to bind hands that one case to
      // the measured path below, which is the only place that reports it.
      if (scan.Width > inner.Area.Width)
        return null;

      return new BoundedSpace(inner, scan, exception => AreaFailure(scope, shape, inner, exception));
    }

    private static AppliedResult<TResult> Project<TResult>(IShape<TResult> shape, Placed placed)
    {
      ShapeResult<TResult> result;
      try
      {
        result = shape.Project(placed.Extent, placed.Scope);
      }
      catch (ShapeException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw placed.Scope.Failure(
          shape,
          $"the projection threw {exception.GetType().Name}: {exception.Message}",
          placed.Extent,
          null,
          exception,
          IsFault(exception));
      }

      // A declared area is consumed in full, even when the projection used less of it — which is
      // where a bound that was left to be discovered is read to exhaustion. A projection that read
      // every row has already settled it, so the canonical case forces nothing twice.
      var consumed = placed.HasDeclaredArea
        ? placed.Bound?.ForceResolved() ?? placed.Extent.Area.Size
        : result.Consumed;

      return new AppliedResult<TResult>(result.Value, placed.Offset, consumed);
    }

    /// <summary>
    /// How a declared extent's failure is reported: a strategy that ran out of room says so, and one
    /// that broke says what broke.
    /// <para>
    /// Shared with <see cref="BoundedSpace"/>, which is handed it as the identity of the placement
    /// whose bound it discovers. That is what makes a deferred failure the placement's failure: the
    /// subject, path, location and fault flag are not merely alike, they are produced by this one
    /// expression from the same scope, shape and space. Only the moment differs.
    /// </para>
    /// </summary>
    private static ShapeException AreaFailure(ShapeContext scope, IShape shape, ISpace inner, Exception exception)
      => exception is OutOfBoundsException
        ? scope.Failure(shape, "its area ran past the space available here", inner, null, exception)
        : scope.Failure(shape, Threw("area", exception), inner, null, exception, IsFault(exception));

    /// <summary>
    /// Whether something broke rather than disagreed with the data. These mean the code is wrong or
    /// the environment failed — a null bug, a bad index into an array or a view, a disk that stopped
    /// answering, a workbook read after its owner was disposed — so no tolerance boundary may
    /// quietly swallow them. Everything else — a cell of the wrong kind, an unparseable value, an
    /// overflow — is the sort of failure tolerance is for.
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
    /// from <see cref="IOException"/> and are covered. <see cref="ObjectDisposedException"/> derives
    /// from <see cref="InvalidOperationException"/>, which is <em>not</em> listed and must not be —
    /// parse helpers throw that for data reasons — so it is named explicitly.
    /// <see cref="ArgumentException"/> itself stays absorbable, for the same reason.
    /// <see cref="OutOfBoundsException"/> is not here at all: running out of room is how a
    /// <c>Repeat</c> stops, and no IO condition produces it.
    /// </para>
    /// </summary>
    internal static bool IsFault(Exception exception)
      => exception is NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException
        or IOException                 // the disk, the network share, the workbook replaced mid-read
        or ObjectDisposedException     // a view outliving its Workbook
        or OutOfMemoryException;       // never a statement about the data

    // A note on the last one: under a genuine out-of-memory condition the wrap itself may fail to
    // allocate, and the original exception then escapes unwrapped. That is fine and is not a hole —
    // an unwrapped OutOfMemoryException is not a ShapeException, so no tolerance boundary catches
    // it either. The property that matters holds by both routes: it is never absorbed.

    private static bool Exceeds(Size size, ISpace space)
      => size.Width > space.Area.Width || size.Height > space.Area.Height;

    private static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    // A matcher that found nothing says what it was looking for; anything else just ran out of room.
    private static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";

    private readonly struct Placed
    {
      public Placed(Offset offset, ISpace extent, ShapeContext scope, bool hasDeclaredArea, BoundedSpace? bound = null)
      {
        Offset = offset;
        Extent = extent;
        Scope = scope;
        HasDeclaredArea = hasDeclaredArea;
        Bound = bound;
      }

      public Offset Offset { get; }
      public ISpace Extent { get; }
      public ShapeContext Scope { get; }
      public bool HasDeclaredArea { get; }

      /// <summary>
      /// The extent, when it is one still being discovered — the same object as <see cref="Extent"/>,
      /// named separately so the rule that a declared area is consumed in full does not depend on a
      /// property getter happening to force.
      /// </summary>
      public BoundedSpace? Bound { get; }
    }

    /// <summary>
    /// Test-only. Measures every declared extent up front on the calling thread, as the engine did
    /// before bounds could be discovered, until the returned scope is disposed. Nested scopes restore
    /// rather than clear, so it composes with itself.
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
