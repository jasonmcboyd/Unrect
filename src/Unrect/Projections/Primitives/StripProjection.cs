using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class StripProjection<TSpace, T> : ProjectionBase<TSpace, T>
    where TSpace : class, ISpace
  {
    public StripProjection(Orientation orientation, Func<CellStrip<TSpace>, T> project, Placement placement, string description)
      : base(placement)
    {
      Orientation = orientation;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private Orientation Orientation { get; }
    private Func<CellStrip<TSpace>, T> Projection { get; }

    public override string Description { get; }

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (Orientation == Orientation.Horizontal && size.Height != 1)
        throw context.Failure($"a Row must be exactly one row tall; this one is {size.Height} rows tall", extent);

      if (Orientation == Orientation.Vertical && size.Width != 1)
        throw context.Failure($"a Column must be exactly one column wide; this one is {size.Width} columns wide", extent);

      try
      {
        return new ProjectionResult<T>(Projection(new CellStrip<TSpace>(extent, Orientation, context)), size);
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }
    }
  }
}
