using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Backs <c>Select</c>. Its own placement is applied by the engine like any other projection's,
  /// so <c>x.Select(f).OffsetBy(o)</c> and <c>x.OffsetBy(o).Select(f)</c> land in the same place.
  /// </summary>
  internal sealed class MapProjection<TSource, TResult> : ProjectionBase<TResult>
  {
    public MapProjection(IProjection<TSource> inner, Func<TSource, TResult> selector, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Selector = selector ?? throw new ArgumentNullException(nameof(selector));
      Children = new IProjection[] { inner };
    }

    private IProjection<TSource> Inner { get; }
    private Func<TSource, TResult> Selector { get; }

    public override string Description => "Select";

    public override IReadOnlyList<IProjection> Children { get; }

    public override bool IsTransparent => Name is null;

    public override ProjectionResult<TResult> Project(ISpace extent, ProjectionContext context)
    {
      var applied = ProjectionEngine.Apply(Inner, extent, context);
      return new ProjectionResult<TResult>(Selector(applied.Value), applied.Advance);
    }
  }
}
