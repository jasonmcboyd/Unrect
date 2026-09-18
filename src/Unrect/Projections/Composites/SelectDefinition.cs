using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Backs <c>Select</c>. Its own placement is applied by the engine like any other projection's,
  /// so <c>x.Select(f).OffsetBy(o)</c> and <c>x.OffsetBy(o).Select(f)</c> land in the same place.
  /// </summary>
  internal sealed class SelectDefinition<TSpace, TSource, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    public SelectDefinition(IProjectionDefinition<TSpace, TSource> inner, Func<TSource, TResult> selector, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Selector = selector ?? throw new ArgumentNullException(nameof(selector));
      Children = new[] { new Child(inner, default) };
    }

    private IProjectionDefinition<TSpace, TSource> Inner { get; }
    private Func<TSource, TResult> Selector { get; }

    public override string Description => "Select";

    public override IReadOnlyList<Child> Children { get; }

    public override bool IsWrapper => true;

    public override Axes Axis => Inner.Axis;

    public override IProjector<TSpace, TResult> Start(ProjectorScope<TSpace> scope) => new Machine(this, scope);

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
          throw Scope.Context.Reading(failure, Extent);
        }
      }
    }

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var applied = ProjectionEngine.Apply(Inner, extent, context);

      try
      {
        return new ProjectionResult<TResult>(Selector(applied.Value), applied.Advance, applied.Presence);
      }
      catch (CellReadException failure)
      {
        throw context.Reading(failure, extent);
      }
    }
  }
}
