using System;

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
    public static AppliedResult<TResult> Apply<TResult>(IShape<TResult> shape, ISpace availableSpace, ShapeContext context)
      => Project(shape, shape.Project, Place(shape, availableSpace, context));

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

      result = Project(shape, shape.Project, placed);
      return true;
    }

    private static Placed Place(IShape shape, ISpace availableSpace, ShapeContext context)
    {
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
        return strict
          ? throw context.Failure(shape, Missing(exception), availableSpace, null, exception)
          : false;
      }
      catch (Exception exception)
      {
        throw context.Failure(shape, Threw("offset", exception), availableSpace, null, exception);
      }

      if (Exceeds(offset.Size, availableSpace))
      {
        return strict
          ? throw context.Failure(shape, $"an offset of {Describe(offset.Size)} does not fit the available space", availableSpace, offset.Size, null)
          : false;
      }

      var inner = availableSpace.GetSubspace(offset);
      var scope = shape.IsTransparent ? context.Advance(offset) : context.Descend(shape, offset);

      if (shape.Placement.Area is null)
      {
        placed = new Placed(offset, inner, scope, false);
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
        return strict
          ? throw scope.Failure(shape, "its area ran past the space available here", inner, null, exception)
          : false;
      }
      catch (Exception exception)
      {
        throw scope.Failure(shape, Threw("area", exception), inner, null, exception);
      }

      if (Exceeds(area.Size, inner))
      {
        return strict
          ? throw scope.Failure(shape, $"an extent of {Describe(area.Size)} does not fit here", inner, area.Size, null)
          : false;
      }

      placed = new Placed(offset, inner.GetSubspace(area), scope, true);
      return true;
    }

    private static AppliedResult<TResult> Project<TResult>(
      IShape shape,
      Func<ISpace, ShapeContext, ShapeResult<TResult>> project,
      Placed placed)
    {
      ShapeResult<TResult> result;
      try
      {
        result = project(placed.Extent, placed.Scope);
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

      // A declared area is consumed in full, even when the projection used less of it.
      var consumed = placed.HasDeclaredArea ? placed.Extent.Area.Size : result.Consumed;
      return new AppliedResult<TResult>(result.Value, placed.Offset, consumed);
    }

    /// <summary>
    /// Whether a projection broke rather than disagreed with the data. These mean the code is
    /// wrong, not the file — a null bug, a bad index into an array or a view — so no tolerance
    /// boundary may quietly swallow them; everything else — a cell of the wrong kind, an
    /// unparseable value, an overflow — is the sort of failure tolerance is for.
    /// (ArgumentException itself stays absorbable: parse-style APIs throw it for data reasons.)
    /// </summary>
    private static bool IsFault(Exception exception)
      => exception is NullReferenceException
        or IndexOutOfRangeException
        or ArgumentOutOfRangeException
        or ArgumentNullException;

    private static bool Exceeds(Size size, ISpace space)
      => size.Width > space.Area.Size.Width || size.Height > space.Area.Size.Height;

    private static string Describe(Size size) => $"{size.Width}x{size.Height}";

    internal static string Threw(string what, Exception exception)
      => $"its {what} strategy threw {exception.GetType().Name}: {exception.Message}";

    // A seek that found nothing says what it was looking for; anything else just ran out of room.
    private static string Missing(OutOfBoundsException exception)
      => exception is AnchorNotFoundException anchor
        ? $"{anchor.Description} exists in the available space"
        : "its offset ran past the available space";

    private readonly struct Placed
    {
      public Placed(Offset offset, ISpace extent, ShapeContext scope, bool hasDeclaredArea)
      {
        Offset = offset;
        Extent = extent;
        Scope = scope;
        HasDeclaredArea = hasDeclaredArea;
      }

      public Offset Offset { get; }
      public ISpace Extent { get; }
      public ShapeContext Scope { get; }
      public bool HasDeclaredArea { get; }
    }
  }
}
