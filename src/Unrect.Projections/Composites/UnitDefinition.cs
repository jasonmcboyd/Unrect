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
  public sealed class UnitDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A composed rung presented as one unit: <paramref name="body"/> runs, and the path shows <paramref name="description"/> with <paramref name="children"/> beneath it rather than the body's own plumbing.
    /// <para>Fluent: <c>composed.AsUnit("Table&lt;T&gt;")</c>.</para>
    /// </summary>
    /// <param name="body">What runs.</param>
    /// <param name="children">The children the unit shows tooling; the body's own are hidden.</param>
    /// <param name="description">The path segment.</param>
    /// <param name="placement">Where the unit is.</param>
    public UnitDefinition(IProjectionDefinition<TSpace, T> body, IReadOnlyList<Child> children, string description, Placement<TSpace> placement)
      : base(placement)
    {
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Children = children ?? throw new ArgumentNullException(nameof(children));
      Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    private IProjectionDefinition<TSpace, T> Body { get; }

    /// <inheritdoc/>
    public override string Description { get; }

    /// <inheritdoc/>
    public override IReadOnlyList<Child> Children { get; }

    /// <summary>A unit hides nothing of its own; what its body cannot show, it says for it.</summary>
    public override string? Opacity => Body.Opacity;

    /// <inheritdoc/>
    public override Axes Axis => Body.Axis.OrEither();

    /// <inheritdoc/>
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
