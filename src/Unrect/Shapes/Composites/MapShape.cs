using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// Backs <c>Select</c>. Its own placement is applied by the engine like any other shape's, so
  /// <c>x.Select(f).After(o)</c> and <c>x.After(o).Select(f)</c> land in the same place.
  /// </summary>
  internal sealed class MapShape<TSource, TResult> : ShapeBase<TResult>
  {
    public MapShape(IShape<TSource> inner, Func<TSource, TResult> selector, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Selector = selector ?? throw new ArgumentNullException(nameof(selector));
      Children = new IShape[] { inner };
    }

    private IShape<TSource> Inner { get; }
    private Func<TSource, TResult> Selector { get; }

    public override string Description => "Select";

    public override IReadOnlyList<IShape> Children { get; }

    public override bool IsTransparent => Name is null;

    public override ShapeResult<TResult> Project(ISpace extent, ShapeContext context)
    {
      var applied = ShapeEngine.Apply(Inner, extent, context);
      return new ShapeResult<TResult>(Selector(applied.Value), applied.Advance);
    }
  }
}
