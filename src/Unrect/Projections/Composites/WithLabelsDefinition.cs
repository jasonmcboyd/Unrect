using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The "provide a map to subspaces" primitive: a transparent single-child wrapper that pushes a
  /// <see cref="LabelMap"/> as the ambient labels along an axis for the body's whole subtree, then
  /// forwards the body's reading unchanged. Modelled on <c>Select</c>'s wrapper — its own placement
  /// is <see cref="Placement.Default"/> (it forces nothing) and the engine advances it as a
  /// transparent node, so the body reads at the wrapper's own frame and column translation is the
  /// identity in the canonical table composition.
  /// </summary>
  internal sealed class WithLabelsDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public WithLabelsDefinition(LabelAxis axis, LabelMap map, IProjectionDefinition<TSpace, T> body, Placement placement)
      : base(placement)
    {
      LabelledAxis = axis;
      Map = map ?? throw new ArgumentNullException(nameof(map));
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Children = new[] { new Child(body, default) };
    }

    private LabelAxis LabelledAxis { get; }
    private LabelMap Map { get; }
    private IProjectionDefinition<TSpace, T> Body { get; }

    public override string Description => LabelledAxis == LabelAxis.Column ? "WithColumnLabels" : "WithRowLabels";

    public override IReadOnlyList<Child> Children { get; }

    public override bool IsWrapper => true;

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // Bound the body to the labelled width only when the extent is wider, so a sheet with trailing
      // blank columns reads under the same columns the labels describe. On an exact-width extent the
      // body is handed through untouched, forcing nothing.
      var width = Map.Labels.Count;
      var body = extent.Width > width
        ? extent.Narrowed(width)
        : extent;

      var applied = ProjectionEngine.Apply(Body, body, context.PushLabels(LabelledAxis, Map, body.Origin));

      return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
    }
  }
}
