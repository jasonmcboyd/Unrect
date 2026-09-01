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
      var values = new object?[Children.Count];
      var along = 0;
      var across = 0;
      var previous = 0;

      for (var index = 0; index < Children.Count; index++)
      {
        var cursor = Step(along);
        AppliedResult<object?> applied;

        try
        {
          applied = ShapeEngine.ApplyUntyped(Children[index], extent.GetSubspace(cursor), context.Advance(cursor));
        }
        // A sibling that consumed nothing — an absorbed boundary, most often — leaves this child
        // reading the very cells that just failed, so it fails the same way for the same reason.
        // The absorption warning may since have been rolled back by an enclosing choice, in which
        // case this note is the only thing left saying so. It has to be the same cell, though: a
        // child that re-anchored itself and failed elsewhere failed on its own account.
        catch (ShapeException failure) when (index > 0 && previous == 0 && failure.Location.IsAt(context.Origin + cursor))
        {
          throw failure.WithNote("the preceding sibling consumed nothing at this position");
        }

        values[index] = applied.Value;
        previous = Along(applied.Advance);
        along += previous;
        across = Math.Max(across, Across(applied.Advance));
      }

      return new ShapeResult<T>(Combine(values), Extent(along, across));
    }

    // A stack consumes along its own axis only; across it, the widest child wins.
    private Offset Step(int along) => Orientation == Orientation.Vertical ? new Offset(0, along) : new Offset(along, 0);

    private int Along(Size size) => Orientation == Orientation.Vertical ? size.Height : size.Width;

    private int Across(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;

    private Size Extent(int along, int across)
      => Orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);
  }
}
