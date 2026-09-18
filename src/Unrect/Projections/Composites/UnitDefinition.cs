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

    /// <summary>A unit hides nothing of its own; what its body cannot show, it says for it.</summary>
    public override string? Opacity => Body.Opacity;

    public override Axes Axis => Body.Axis.OrEither();

    public override Reach Reach => Body.Reach;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    private sealed class Machine : ForwardingProjector<TSpace, T, T>
    {
      public Machine(UnitDefinition<TSpace, T> unit, ProjectorScope<TSpace> scope)
        : base(unit, scope, new Child(unit.Body, default), unit.Body)
      {
      }

      protected override T Finish(T value) => value;
    }
  }
}
