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

      Axis = axis;
      Header = header ?? throw new ArgumentNullException(nameof(header));
      Body = body ?? throw new ArgumentNullException(nameof(body));
      Description = description ?? "UnderColumnLabels";

      // The header is child one and the body child two, as a flow would have numbered them.
      Children = new[] { new Child(header, UseSite.From(null, 1)), new Child(body, UseSite.From(null, 2)) };
    }

    private LabelAxis Axis { get; }
    private IProjectionDefinition<TSpace, LabelMap> Header { get; }
    private IProjectionDefinition<TSpace, T> Body { get; }

    public override string Description { get; }

    public override IReadOnlyList<Child> Children { get; }

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // A vertical flow of two: the header band, then the body under the labels it produced. The
      // same arithmetic and the same sibling rule as any flow, because it runs on a flow's state.
      var state = new FlowState<TSpace>(Orientation.Vertical, extent, context);

      var labels = state.Next(Header, Children[0].Site);
      var value = state.Next(new WithLabelsDefinition<TSpace, T>(Axis, labels, Body, Placement.Default), Children[1].Site);

      return new ProjectionResult<T>(value, state.Consumed, state.Presence);
    }
  }
}
