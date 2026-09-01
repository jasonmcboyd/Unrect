using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// Placement without flow: every child is applied to the overlay's whole extent, each finding its
  /// own place inside it, with no cursor between them. Where a stack is a StackPanel, this is a
  /// Grid — children may overlap and may read the same cells, because they read rather than paint.
  /// One class for all arities, like <see cref="StackShape{T}"/>.
  /// </summary>
  internal sealed class OverlayShape<T> : ShapeBase<T>
  {
    public OverlayShape(IReadOnlyList<IShape> children, Func<object?[], T> combine, Placement placement)
      : base(placement)
    {
      if (children is null)
        throw new ArgumentNullException(nameof(children));

      var copy = new IShape[children.Count];

      for (var index = 0; index < copy.Length; index++)
        // The factories validate their own parameters; this is the invariant behind them.
        copy[index] = children[index] ?? throw new ArgumentException("An overlay cannot contain a null shape.", nameof(children));

      Children = copy;
      Combine = combine ?? throw new ArgumentNullException(nameof(combine));
    }

    private Func<object?[], T> Combine { get; }

    public override string Description => "Overlay";

    public override IReadOnlyList<IShape> Children { get; }

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var values = new object?[Children.Count];
      var width = 0;
      var height = 0;

      for (var index = 0; index < Children.Count; index++)
      {
        // The same extent every time: a child that does not fit is a hard error, as in a stack.
        var applied = ShapeEngine.ApplyUntyped(Children[index], extent, context);

        values[index] = applied.Value;
        width = Math.Max(width, applied.Advance.Width);
        height = Math.Max(height, applied.Advance.Height);
      }

      // Derived extent is the bounding box of where the children actually landed.
      return new ShapeResult<T>(Combine(values), new Size(width, height));
    }
  }
}
