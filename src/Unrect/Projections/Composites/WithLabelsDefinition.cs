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

    public override Axes Axis => Body.Axis.OrEither();

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Narrows every span to the labelled width, pushes the labels for the body's subtree with the
    /// body's origin as their frame, and forwards. The body is started at the first span, because
    /// the frame is its origin.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly WithLabelsDefinition<TSpace, T> _labelled;
      private readonly ProjectorScope<TSpace> _scope;
      private IChildHandle<TSpace, T>? _body;

      public Machine(WithLabelsDefinition<TSpace, T> labelled, ProjectorScope<TSpace> scope)
      {
        _labelled = labelled;
        _scope = scope;
      }

      public bool Next(Plane<TSpace> span)
      {
        _body ??= StartBody(Narrow(span));

        return _body.Next(Narrow(span));
      }

      public Settlement<T> Close()
      {
        _body ??= StartBody(_scope.Anchor);

        var settlement = _body.Close();

        return new Settlement<T>(settlement.Value, _body.Advance, _body.Presence);
      }

      private Plane<TSpace> Narrow(Plane<TSpace> span)
      {
        var width = _labelled.Map.Labels.Count;

        return span.Width > width ? span.Narrowed(width) : span;
      }

      private IChildHandle<TSpace, T> StartBody(Plane<TSpace> at)
      {
        var scope = _scope.PushLabels(_labelled.LabelledAxis, _labelled.Map, at.Origin).At(Spans.Empty(at, _scope.Driver), _scope.Driver);

        return scope.Start(_labelled.Children[0], _labelled.Body, scope.Anchor, inheritSite: true);
      }
    }
  }
}
