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
  internal sealed class UnitProjection<T> : ProjectionBase<T>
  {
    public UnitProjection(IProjection<T> body, IReadOnlyList<IProjection> children, string description, Placement placement)
      : base(placement)
    {
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Children = children;
      Description = description;
    }

    private IProjection<T> Body { get; }

    public override string Description { get; }

    public override IReadOnlyList<IProjection> Children { get; }

    public override ProjectionResult<T> Project(Plane<ICellValues> extent, ProjectionContext context)
    {
      var applied = ProjectionEngine.Apply(Body, extent, context);

      return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
    }
  }
}
