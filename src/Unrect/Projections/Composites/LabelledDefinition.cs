using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A header read once, then a body beneath it resolving columns through what the header named:
  /// the composition a built-in <c>Table</c> is made of, as one node whose two children are the
  /// header's projection and the body's. It takes the header's <em>projection</em> rather than a
  /// <see cref="LabelMap"/> value, because the map is known only once a header is read — the one
  /// place the library would otherwise have chosen a shape from a value, which a declaration must
  /// not do.
  /// </summary>
  internal sealed class LabelledDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    public LabelledDefinition(LabelAxis axis, IProjectionDefinition<TSpace, LabelMap> header, IProjectionDefinition<TSpace, T> body, Placement placement, string? description = null)
      : base(placement)
    {
      if (axis != LabelAxis.Column)
        throw new ArgumentOutOfRangeException(nameof(axis), axis, "Only a column header is read above its body in this release.");

      LabelledAxis = axis;
      Header = header ?? throw new ArgumentNullException(nameof(header));
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Description = description ?? "UnderColumnLabels";

      // The header is child one and the body child two, as a flow would have numbered them.
      Children = new[] { new Child(header, UseSite.From(null, 1)), new Child(body, UseSite.From(null, 2)) };
    }

    private LabelAxis LabelledAxis { get; }
    private IProjectionDefinition<TSpace, LabelMap> Header { get; }
    private IProjectionDefinition<TSpace, T> Body { get; }

    public override string Description { get; }

    public override IReadOnlyList<Child> Children { get; }

    public override Axes Axis => Axes.Vertical;

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>The header until it refuses, then the body under the labels it produced: a flow of two, with the second child built from the first's value.</summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly LabelledDefinition<TSpace, T> _labelled;
      private readonly ProjectorScope<TSpace> _scope;
      private IChildHandle<TSpace, LabelMap>? _header;
      private IChildHandle<TSpace, T>? _body;
      private Plane<TSpace>? _first;
      private int _along;
      private int _across;
      private bool _read;
      private bool _finished;
      private bool _closed;

      public Machine(LabelledDefinition<TSpace, T> labelled, ProjectorScope<TSpace> scope)
      {
        _labelled = labelled;
        _scope = scope;
      }

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Context.Failure(_labelled, $"{PathRenderer.Describe(_labelled)} was fed a span after it was closed", span, null, null, isFault: true);

        _first ??= span;

        return Offer(span);
      }

      public Settlement<T> Close()
      {
        _closed = true;

        if (_body is null)
        {
          _header ??= StartHeader(_scope.Anchor);
          CloseHeader();
        }

        CloseBody();

        return new Settlement<T>(_body!.Consumed.Width == 0 && _body.Consumed.Height == 0 && _header!.Consumed.Height == 0 ? default! : _value!, Spans.ToSize(_along, _across, Orientation.Vertical), _read ? Presence.Read : Presence.Empty);
      }

      private T? _value;

      private bool Offer(Plane<TSpace> span)
      {
        if (_finished)
          return false;

        if (_body is null)
        {
          _header ??= StartHeader(Spans.Empty(span, _scope.Driver));

          if (_header.Next(span))
            return true;

          CloseHeader();
        }

        if (_body!.Next(span))
          return true;

        _finished = true;
        return false;
      }

      private IChildHandle<TSpace, LabelMap> StartHeader(Plane<TSpace> at)
        => _scope.Start(_labelled.Children[0], _labelled.Header, at);

      private void CloseHeader()
      {
        var labels = _header!.Close().Value;
        Took(_header.Advance, _header.Presence);

        var at = _first is Plane<TSpace> first ? Spans.EmptyAt(first, _along, Orientation.Vertical) : _scope.Anchor;
        var body = new WithLabelsDefinition<TSpace, T>(_labelled.LabelledAxis, labels, _labelled.Body, Placement.Default);

        _body = _scope.Start(_labelled.Children[1], body, at);

        foreach (var span in _header.Shortfall())
          if (!Offer(span))
            break;
      }

      private void CloseBody()
      {
        var settlement = _body!.Close();
        _value = settlement.Value;
        Took(_body.Advance, _body.Presence);
      }

      private void Took(Size advance, Presence presence)
      {
        _along += advance.Height;
        _across = Math.Max(_across, advance.Width);
        _read |= presence == Presence.Read;
      }
    }

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // A vertical flow of two: the header band, then the body under the labels it produced. The
      // same arithmetic and the same sibling rule as any flow, because it runs on a flow's state.
      var state = new FlowState<TSpace>(Orientation.Vertical, extent, context);

      var labels = state.Next(Header, Children[0].Site);
      var value = state.Next(new WithLabelsDefinition<TSpace, T>(LabelledAxis, labels, Body, Placement.Default), Children[1].Site);

      return new ProjectionResult<T>(value, state.Consumed, state.Presence);
    }
  }
}
