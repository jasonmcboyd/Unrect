using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A composite whose children are declared by calling <c>Next</c> on a cursor rather than by being
  /// passed in. Running the lambda is the only way to learn what it contains, which is why every
  /// layout is opaque to anything that walks a declaration without a space.
  /// <para>
  /// Subclasses differ in one thing: the <see cref="LayoutState"/> they run on, which is what decides
  /// whether a child moves the next one along. Everything else about declaring children this way —
  /// the single pass, closing the layout, and refusing one that declared nothing — is the same for
  /// all of them and lives here.
  /// </para>
  /// </summary>
  internal abstract class LayoutShape<T> : ShapeBase<T>, IOpaqueComposite
  {
    protected LayoutShape(Layout<T> build, Placement placement)
      : base(placement)
    {
      Build = build ?? throw new ArgumentNullException(nameof(build));
    }

    private Layout<T> Build { get; }

    public string Reason => "declared by a cursor lambda; children are known only while it runs";

    /// <summary>The state that decides what this layout does with its extent between children.</summary>
    protected abstract LayoutState NewState(ISpace extent, ShapeContext context);

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var state = NewState(extent, context);

      // One pass, immediately: what the lambda reads, it reads now. A failure inside a Next call
      // belongs to the child that raised it and travels out through here untouched — but the layout
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

      // A layout that declared nothing would match anything, describe nothing, and quietly end an
      // enclosing repetition by consuming nothing. That is a bug in the declaration, so no tolerance
      // boundary may absorb it.
      if (state.Count == 0)
        throw context.Failure(this, state.DeclaredNothing, extent, null, null, isFault: true);

      return new ShapeResult<T>(value, state.Consumed);
    }
  }
}
