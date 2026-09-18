using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A composite whose children were declared through a cursor and closed into a
  /// <see cref="Layout{TSpace, T}"/>: the children are complete and ordered, and the result is the
  /// combiner applied to what they read. Subclasses differ in one thing: the
  /// <see cref="LayoutState{TSpace}"/> they run on, which is what decides whether a child moves the
  /// next one along.
  /// </summary>
  internal abstract class LayoutDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    protected LayoutDefinition(Layout<TSpace, T> layout, Placement placement)
      : base(placement)
    {
      Layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    private protected Layout<TSpace, T> Layout { get; }

    public override IReadOnlyList<Child> Children => Layout.Children;

    /// <summary>The state that decides what this layout does with its extent between children.</summary>
    protected abstract LayoutState<TSpace> NewState(Plane<TSpace> extent, ProjectionContext context);

    public override ProjectionResult<T> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var state = NewState(extent, context);
      var children = Layout.Children;
      var values = new object?[children.Count];

      // Every child, in declaration order, at the site the declaration wrote it. A failure inside a
      // child belongs to that child and travels out through here untouched.
      for (var index = 0; index < values.Length; index++)
        values[index] = Layout.Runners[index].Apply(state, children[index].Site);

      T value;

      try
      {
        value = Layout.Combine(new Reading(Layout.Builder, values));
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }

      return new ProjectionResult<T>(value, state.Consumed, state.Presence);
    }
  }
}
