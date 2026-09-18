using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class StripDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public StripDefinition(Orientation orientation, Func<CellStrip<TSpace>, T> project, Placement placement, string description)
      : base(placement)
    {
      Orientation = orientation;
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private Orientation Orientation { get; }
    private Func<CellStrip<TSpace>, T> Projection { get; }

    public override string Description { get; }

    /// <summary>A row strip is one row-span; a column strip is one column-span, held and re-driven under a row-major driver.</summary>
    public override Axes Axis => Orientation == Orientation.Horizontal ? Axes.Vertical : Axes.Horizontal;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, T>(this, scope, 1);

    internal override bool Collects => true;


    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      var size = extent.Area.Size;

      if (Orientation == Orientation.Horizontal && size.Height != 1)
        throw scope.Failure($"a Row must be exactly one row tall; this one is {size.Height} rows tall", extent);

      if (Orientation == Orientation.Vertical && size.Width != 1)
        throw scope.Failure($"a Column must be exactly one column wide; this one is {size.Width} columns wide", extent);

      try
      {
        return new Settlement<T>(Projection(new CellStrip<TSpace>(extent, Orientation, scope)), size);
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }
    }
  }
}
