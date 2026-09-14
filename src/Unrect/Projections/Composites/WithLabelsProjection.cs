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
  internal sealed class WithLabelsProjection<T> : ProjectionBase<T>
  {
    public WithLabelsProjection(LabelAxis axis, LabelMap map, IProjection<T> body, Placement placement)
      : base(placement)
    {
      Axis = axis;
      Map = map ?? throw new ArgumentNullException(nameof(map));
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Children = new IProjection[] { body };
    }

    private LabelAxis Axis { get; }
    private LabelMap Map { get; }
    private IProjection<T> Body { get; }

    public override string Description => Axis == LabelAxis.Column ? "WithColumnLabels" : "WithRowLabels";

    public override IReadOnlyList<IProjection> Children { get; }

    public override bool IsTransparent => Name is null && !IsUnitBoundary;

    public override ProjectionResult<T> Project(ICellValues extent, ProjectionContext context)
    {
      // Bound the body to the labelled width only when the extent is wider, so a sheet with trailing
      // blank columns reads under the same columns the labels describe. On an exact-width extent the
      // body is handed through untouched, forcing nothing.
      var width = Map.Labels.Count;
      var body = BoundedSpace.WidthOf(extent) > width
        ? BoundedSpace.Narrow(extent, width)
        : extent;

      var applied = ProjectionEngine.Apply(Body, body, context.PushLabels(Axis, Map));

      return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
    }
  }
}
