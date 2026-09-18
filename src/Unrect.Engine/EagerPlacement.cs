using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Placement for a child the engine held whole: its offset and area resolved over the region it
  /// was offered by folding the placement's scans over it, once that region is known. What a
  /// child whose scans answer only at the end, or whose axis is not the driver's, gets instead of
  /// being driven.
  /// </summary>
  internal static class EagerPlacement
  {
    internal static bool TryPlace<TSpace>(
      IProjectionDefinition projection,
      Plane<TSpace> region,
      Orientation along,
      ProjectorScope<TSpace> parent,
      bool strict,
      out Offset offset,
      out Plane<TSpace> inner,
      out ProjectorScope<TSpace> scope,
      out bool hasDeclaredArea)
      where TSpace : class, ISpace
    {
      inner = default;
      scope = parent;
      hasDeclaredArea = false;

      try
      {
        offset = Scans.FoldOffset(projection.Placement.Offset.Begin(along), region.Erased(), along);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        offset = default;

        if (strict)
          throw parent.Failure(projection, EngineRules.Missing(exception), region, null, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw parent.Failure(projection, EngineRules.Threw("offset", exception), region, null, exception, EngineRules.IsFault(exception));
      }

      if (EngineRules.Exceeds(offset.Size, region))
      {
        if (strict)
          throw parent.Failure(projection, $"an offset of {EngineRules.Describe(offset.Size)} does not fit the available space", region, offset.Size, null);

        return false;
      }

      inner = region.Slice(offset);
      scope = PathRenderer.Skipped(projection) ? parent.Blaming(projection) : parent.Descend(projection);

      if (projection.Placement.Area is null)
        return true;

      Area area;

      try
      {
        area = new Area(Scans.FoldSize(projection.Placement.Area.Begin(along), inner.Erased(), along));
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (strict)
          throw EngineRules.AreaFailure(scope, projection, inner, exception);

        return false;
      }
      catch (Exception exception)
      {
        throw EngineRules.AreaFailure(scope, projection, inner, exception);
      }

      if (EngineRules.Exceeds(area.Size, inner))
      {
        if (strict)
          throw scope.Failure(projection, $"an extent of {EngineRules.Describe(area.Size)} does not fit here", inner, area.Size, null);

        return false;
      }

      inner = inner.Slice(area);
      hasDeclaredArea = true;
      return true;
    }
  }
}
