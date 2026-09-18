using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A composition presented as one node: the body reads, and the unit supplies the placement, the
  /// description, and the children a walker sees — the declaration the user wrote, not the parts it
  /// was built from. The parts fold out of a rendered path by being marked scaffolding, so nothing
  /// here depends on what the body happens to be.
  /// </summary>
  internal sealed class UnitDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public UnitDefinition(IProjectionDefinition<TSpace, T> body, IReadOnlyList<Child> children, string description, Placement placement)
      : base(placement)
    {
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Children = children;
      Description = description;
    }

    private IProjectionDefinition<TSpace, T> Body { get; }

    public override string Description { get; }

    public override IReadOnlyList<Child> Children { get; }

    public override Axes Axis => Body.Axis.OrEither();

    public override Reach Reach => Body.Reach;

    public override IProjector<TSpace, T> Start(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    private sealed class Machine : ForwardingProjector<TSpace, T, T>
    {
      public Machine(UnitDefinition<TSpace, T> unit, ProjectorScope<TSpace> scope)
        : base(unit, scope, new Child(unit.Body, default), unit.Body)
      {
      }

      protected override T Finish(T value) => value;
    }

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var applied = ProjectionEngine.Apply(Body, extent, context);

      return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
    }
  }
}
