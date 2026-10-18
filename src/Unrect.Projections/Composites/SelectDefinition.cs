using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Backs <c>Select</c>. Its own placement is applied by the engine like any other projection's,
  /// so <c>x.Select(f).OffsetBy(o)</c> and <c>x.OffsetBy(o).Select(f)</c> land in the same place.
  /// </summary>
  public sealed class SelectDefinition<TSpace, TSource, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// <paramref name="inner"/>'s value passed through <paramref name="selector"/>; a cell read that fails inside the selector is a located failure.
    /// <para>Fluent: <c>inner.Select(x =&gt; …)</c>.</para>
    /// </summary>
    /// <param name="inner">What is transformed.</param>
    /// <param name="selector">The transformation.</param>
    /// <param name="placement">Where the inner is.</param>
    public SelectDefinition(IProjectionDefinition<TSpace, TSource> inner, Func<TSource, TResult> selector, Placement<TSpace> placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Selector = selector ?? throw new ArgumentNullException(nameof(selector));
      Children = new[] { new Child(inner, default) };
    }

    private IProjectionDefinition<TSpace, TSource> Inner { get; }
    private Func<TSource, TResult> Selector { get; }

    /// <inheritdoc/>
    public override string Description => "Select";

    /// <inheritdoc/>
    public override IReadOnlyList<Child> Children { get; }

    /// <inheritdoc/>
    public override bool IsWrapper => true;

    /// <inheritdoc/>
    public override Axes Axis => Inner.Axis.OrEither();

    /// <inheritdoc/>
    public override IProjector<TSpace, TResult> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    private sealed class Machine : ForwardingProjector<TSpace, TSource, TResult>
    {
      private readonly SelectDefinition<TSpace, TSource, TResult> _select;

      public Machine(SelectDefinition<TSpace, TSource, TResult> select, ProjectorScope<TSpace> scope)
        : base(select, scope, select.Children[0], select.Inner)
        => _select = select;

      protected override TResult Finish(TSource value)
      {
        try
        {
          return _select.Selector(value);
        }
        catch (CellReadException failure)
        {
          throw Scope.Reading(failure, Extent);
        }
      }
    }
  }
}
