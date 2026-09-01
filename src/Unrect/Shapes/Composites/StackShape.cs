using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One class for every arity of <c>Vertical</c> and <c>Horizontal</c>: the children are a list and
  /// the tuple shape lives in the combine function the factory closes over.
  /// </summary>
  internal sealed class StackShape<T> : ShapeBase<T>
  {
    public StackShape(Orientation orientation, IReadOnlyList<IShape> children, Func<object?[], T> combine, Placement placement)
      : base(placement)
    {
      if (children is null)
        throw new ArgumentNullException(nameof(children));

      var copy = new IShape[children.Count];

      for (var index = 0; index < copy.Length; index++)
        // The factories validate their own parameters; this is the invariant behind them.
        copy[index] = children[index] ?? throw new ArgumentException("A stack cannot contain a null shape.", nameof(children));

      Children = copy;
      Orientation = orientation;
      Combine = combine ?? throw new ArgumentNullException(nameof(combine));
    }

    private Orientation Orientation { get; }
    private Func<object?[], T> Combine { get; }

    public override string Description => Orientation == Orientation.Vertical ? "Vertical" : "Horizontal";

    public override IReadOnlyList<IShape> Children { get; }

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var state = new FlowState(this, Orientation, extent, context);
      var values = new object?[Children.Count];

      for (var index = 0; index < Children.Count; index++)
        values[index] = state.NextUntyped(Children[index]);

      return new ShapeResult<T>(Combine(values), state.Consumed);
    }
  }
}
