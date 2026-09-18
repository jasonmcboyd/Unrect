using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One cell, handed over as the address of itself. The leaf for a reading the vocabulary does not
  /// name: a point answers the canonical questions, and a backend's own extension answers the rest.
  /// </summary>
  /// <typeparam name="TSpace">The space the point addresses a cell of.</typeparam>
  internal sealed class PointDefinition<TSpace> : DefinitionNode<TSpace, Point<TSpace>>
    where TSpace : class, ISpace
  {
    public PointDefinition(Placement placement)
      : base(placement)
    {
    }

    public override string Description => "Point";

    public override IProjector<TSpace, Point<TSpace>> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, Point<TSpace>>(this, scope, 1);

    public override ProjectionResult<Point<TSpace>> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      // Reachable only when the placement was replaced — Point().Sized(…) — and left in because it
      // is also the half a writer would satisfy: one cell declared, one cell emitted.
      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"a Point must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      return new ProjectionResult<Point<TSpace>>(extent[0, 0], size);
    }
  }
}
