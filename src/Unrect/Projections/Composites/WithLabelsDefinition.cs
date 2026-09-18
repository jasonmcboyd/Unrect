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

    public override IProjector<TSpace, T> Start(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Narrows every span to the labelled width, pushes the labels for the body's subtree with the
    /// body's origin as their frame, and forwards. The body is started at the first span, because
    /// the frame is its origin.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly WithLabelsDefinition<TSpace, T> _labelled;
      private readonly ProjectorScope<TSpace> _scope;
      private ChildProjector<TSpace, T>? _body;
      private Plane<TSpace>? _first;
      private int _offered;
      private bool _closed;

      public Machine(WithLabelsDefinition<TSpace, T> labelled, ProjectorScope<TSpace> scope)
      {
        _labelled = labelled;
        _scope = scope;
      }

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Context.Failure(_labelled, $"{ProjectionContext.Describe(_labelled)} was fed a span after it was closed", span, null, null, isFault: true);

        _first ??= span;
        _offered++;
        _body ??= StartBody(Narrow(span));

        return _body.Next(Narrow(span));
      }

      public Settlement<T> Close()
      {
        _closed = true;
        _body ??= StartBody(_scope.Anchor);

        var settlement = _body.Close();

        return new Settlement<T>(settlement.Value, _body.Advance, _body.Presence);
      }

      private Plane<TSpace> Narrow(Plane<TSpace> span)
      {
        var width = _labelled.Map.Labels.Count;

        return span.Width > width ? span.Narrowed(width) : span;
      }

      private ChildProjector<TSpace, T> StartBody(Plane<TSpace> at)
      {
        var scope = _scope.At(_scope.Context.PushLabels(_labelled.LabelledAxis, _labelled.Map, at.Origin), Spans.Empty(at, _scope.Driver), _scope.Driver);

        return scope.Start(_labelled.Children[0], _labelled.Body, scope.Anchor, inheritSite: true);
      }
    }

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
