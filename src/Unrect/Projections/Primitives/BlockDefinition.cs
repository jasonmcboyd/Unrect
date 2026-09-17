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

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
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
      return new ProjectionResult<T>(value, extent.Area.Size);
    }
  }
}
