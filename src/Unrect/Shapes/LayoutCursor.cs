using System;
using System.Runtime.CompilerServices;

namespace Unrect.Shapes
{
  /// <summary>
  /// A layout declared as a sequence of <see cref="LayoutCursor.Next{T}"/> calls, each returning the
  /// value its shape read, and the whole returning whatever the caller builds from them.
  /// </summary>
  public delegate TResult Layout<TResult>(LayoutCursor cursor);

  /// <summary>
  /// The cursor a layout lambda declares its children with — the same one for a flow and for an
  /// overlay, because what differs between them is what the composite does between calls, not how a
  /// child is declared. <see cref="Next{T}"/> is the whole of it: there is deliberately nothing to
  /// ask about where the cursor is or how much is left, because a declaration says what the data
  /// looks like, not how to walk it.
  /// <para>
  /// It is a <c>ref struct</c>, so the compiler refuses every way of using it outside the lambda
  /// that received it — capturing it in a nested lambda or local function, storing it in a field, an
  /// array, or a list, returning it, or carrying it into a deferred query. The one cursor that is
  /// not a live one is <c>default(LayoutCursor)</c>, which every call refuses at run time.
  /// </para>
  /// </summary>
  public readonly ref struct LayoutCursor
  {
    private readonly LayoutState? _state;

    internal LayoutCursor(LayoutState state)
    {
      _state = state;
    }

    /// <summary>
    /// Reads <paramref name="shape"/> and returns its value: in a flow, at the position the children
    /// before it left off; in an overlay, against the whole extent, wherever the shape's own
    /// placement puts it. Calling it is what puts a shape in the layout, so the order of the calls is
    /// the order of the children — nothing else about the lambda is a declaration.
    /// <para>
    /// Diagnostics call the child by the best name available: one given with
    /// <c>v.Next(summary.Named("summary"))</c>, else the identifier it was written as —
    /// <c>v.Next(transactions)</c> reads as <c>'transactions'</c> — else its kind and position, as
    /// <c>Cell#2</c>. So hoist a shape into a well-named local, or name it inline; anything that is
    /// not a plain identifier, such as <c>v.Next(row.Down(1))</c>, falls back to the position.
    /// </para>
    /// </summary>
    /// <param name="shape">The shape to read here.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="shape"/> argument. It is not a
    /// naming API — pass <c>.Named(…)</c> when you want to choose what a child is called.
    /// </param>
    public T Next<T>(IShape<T> shape, [CallerArgumentExpression("shape")] string? declared = null)
    {
      if (_state is null)
        throw new InvalidOperationException(LayoutState.NoLayout);

      return _state.Next(shape, declared);
    }
  }
}
