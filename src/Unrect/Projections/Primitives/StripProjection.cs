using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class StripProjection<T> : ProjectionBase<T>
  {
    public StripProjection(Orientation orientation, Func<CellStrip, T> project, Placement placement, string description)
      : base(placement)
    {
      Orientation = orientation;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private Orientation Orientation { get; }
    private Func<CellStrip, T> Projection { get; }

    public override string Description { get; }

    public override ProjectionResult<T> Project(ISpace extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (Orientation == Orientation.Horizontal && size.Height != 1)
        throw context.Failure($"a Row must be exactly one row tall; this one is {size.Height} rows tall", extent);

      if (Orientation == Orientation.Vertical && size.Width != 1)
        throw context.Failure($"a Column must be exactly one column wide; this one is {size.Width} columns wide", extent);

      return new ProjectionResult<T>(Projection(new CellStrip(extent, Orientation, context.Origin)), size);
    }
  }
}
