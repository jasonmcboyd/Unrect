using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The pull engine's placement, applied over a region the push engine has already held whole:
  /// the same strategies, the same messages, the same scope. What a held child resolves at
  /// <c>Close</c>, and what a streaming child falls back to when its feed ended before its offset
  /// resolved.
  /// </summary>
  internal static class EagerPlacement
  {
    internal static bool TryPlace<TSpace>(
      IProjectionDefinition projection,
      Plane<TSpace> region,
      ProjectionContext context,
      bool strict,
      out Offset offset,
      out Plane<TSpace> inner,
      out ProjectionContext scope,
      out bool hasDeclaredArea)
      where TSpace : class, ISpace
    {
      inner = default;
      scope = context;
      hasDeclaredArea = false;

      try
      {
        offset = projection.Placement.Offset.GetOffset(region.Erased());
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        offset = default;

        if (strict)
          throw context.Failure(projection, ProjectionEngine.Missing(exception), region, null, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw context.Failure(projection, ProjectionEngine.Threw("offset", exception), region, null, exception, ProjectionEngine.IsFault(exception));
      }

      if (ProjectionEngine.Exceeds(offset.Size, region))
      {
        if (strict)
          throw context.Failure(projection, $"an offset of {ProjectionEngine.Describe(offset.Size)} does not fit the available space", region, offset.Size, null);

        return false;
      }

      inner = region.Slice(offset);
      scope = ProjectionContext.Skipped(projection) ? context.Blaming(projection) : context.Descend(projection);

      if (projection.Placement.Area is null)
        return true;

      Area area;

      try
      {
        area = projection.Placement.Area.GetArea(inner.Erased());
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw ProjectionEngine.AreaFailure(scope, projection, inner, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw ProjectionEngine.AreaFailure(scope, projection, inner, exception);
      }

      if (ProjectionEngine.Exceeds(area.Size, inner))
      {
        if (strict)
          throw scope.Failure(projection, $"an extent of {ProjectionEngine.Describe(area.Size)} does not fit here", inner, area.Size, null);

        return false;
      }

      inner = inner.Slice(area);
      hasDeclaredArea = true;
      return true;
    }
  }
}
