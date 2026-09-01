using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A shape whose children exist only while it runs. Tooling that walks a declaration without a
  /// space would otherwise read an empty <c>Children</c> as "leaf", which is a lie; this says so
  /// instead.
  /// </summary>
  internal interface IOpaqueComposite
  {
    /// <summary>Why the children are missing, for a renderer to show in their place.</summary>
    string Reason { get; }
  }

  /// <summary>
  /// A flow whose children are declared by calling <c>Next</c> rather than by being passed in. It
  /// lays them out exactly as <see cref="StackShape{T}"/> does — the same arithmetic, through the
  /// same <see cref="FlowState"/> — and describes itself identically, so no diagnostic can tell
  /// which spelling produced it. What it cannot do is say what its children are without a space.
  /// </summary>
  internal sealed class CursorStackShape<T> : ShapeBase<T>, IOpaqueComposite
  {
    public CursorStackShape(Orientation orientation, Layout<T> build, Placement placement)
      : base(placement)
    {
      Orientation = orientation;
      Build = build ?? throw new ArgumentNullException(nameof(build));
    }

    private Orientation Orientation { get; }
    private Layout<T> Build { get; }

    public override string Description => Orientation == Orientation.Vertical ? "Vertical" : "Horizontal";

    public string Reason => "declared by a cursor lambda; children are known only while it runs";

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var state = new FlowState(this, Orientation, extent, context);

      // One pass, immediately: what the lambda reads, it reads now. A failure inside a Next call
      // belongs to the child that raised it and travels out through here untouched — but the flow
      // still ends here, so a cursor that somehow outlived the lambda is refused either way.
      T value;

      try
      {
        value = Build(new LayoutCursor(state));
      }
      finally
      {
        state.Close();
      }

      // A flow that declared nothing would match anything, describe nothing, and quietly end an
      // enclosing repetition by consuming nothing. That is a bug in the declaration, so no
      // tolerance boundary may absorb it.
      if (state.Count == 0)
        throw context.Failure(
          this,
          "a flow must declare at least one shape; this one called Next zero times",
          extent,
          null,
          null,
          isProjectionFault: true);

      return new ShapeResult<T>(value, state.Consumed);
    }
  }
}
