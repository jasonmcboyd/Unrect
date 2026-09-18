using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class BlockDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public BlockDefinition(Func<CellBlock<TSpace>, T> project, Placement placement, string description)
      : base(placement)
    {
      Projection = project ?? throw new ArgumentNullException(nameof(project));
      Description = description;
    }

    private Func<CellBlock<TSpace>, T> Projection { get; }

    public override string Description { get; }

    /// <summary>A block lambda reads its whole extent at random, so it streams along no axis: held, then handed the region as one span.</summary>
    public override Axes Axis => Axes.None;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, T>(this, scope, 1);

    internal override bool Collects => true;


    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectionContext context)
    {
      T value;

      try
      {
        value = Projection(new CellBlock<TSpace>(extent, context));
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }

      // The extent is measured after the projection has run, never before: on a bound still being
      // discovered, asking first would settle it before the projection had read a row.
      return new Settlement<T>(value, extent.Area.Size);
    }
  }
}
