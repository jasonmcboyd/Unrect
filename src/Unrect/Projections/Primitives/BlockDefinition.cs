using System;

using Unrect.Core;

namespace Unrect.Projections
{
  internal sealed class BlockDefinition<TSpace, T> : CollectorNode<TSpace, T>
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

    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
    {
      T value;

      try
      {
        value = Projection(new CellBlock<TSpace>(extent, scope));
      }
      catch (CellReadException failure)
      {
        throw scope.Reading(failure, extent);
      }

      return new Settlement<T>(value, extent.Area.Size);
    }
  }
}
