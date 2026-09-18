using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A composite whose children were declared through a cursor and closed into a
  /// <see cref="Layout{TSpace, T}"/>: the children are complete and ordered, and the result is the
  /// combiner applied to what they read. Subclasses differ in one thing: the machine they build,
  /// which is what decides whether a child moves the next one along.
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
  }
}
