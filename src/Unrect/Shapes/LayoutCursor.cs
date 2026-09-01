using System;

namespace Unrect.Shapes
{
  /// <summary>
  /// A flow declared as a sequence of <see cref="LayoutCursor.Next{T}"/> calls, each returning the
  /// value its shape read, and the whole returning whatever the caller builds from them.
  /// </summary>
  public delegate TResult Layout<TResult>(LayoutCursor cursor);

  /// <summary>
  /// The cursor a flow lambda declares its children with. <see cref="Next{T}"/> is the whole of it:
  /// there is deliberately nothing to ask about where the cursor is or how much is left, because a
  /// declaration says what the data looks like, not how to walk it.
  /// <para>
  /// It is a <c>ref struct</c>, so the compiler refuses every way of using it outside the lambda
  /// that received it — capturing it in a nested lambda or local function, storing it in a field, an
  /// array, or a list, returning it, or carrying it into a deferred query. The one cursor that is
  /// not a live one is <c>default(LayoutCursor)</c>, which every call refuses at run time.
  /// </para>
  /// </summary>
  public readonly ref struct LayoutCursor
  {
    private readonly FlowState? _state;

    internal LayoutCursor(FlowState state)
    {
      _state = state;
    }

    /// <summary>
    /// Reads <paramref name="shape"/> at the flow's current position, advances the flow past what
    /// it consumed, and returns its value. Calling it is what puts a shape in the flow, so the
    /// order of the calls is the order of the children — nothing else about the lambda is a
    /// declaration.
    /// <para>
    /// Name a child where it belongs, on the shape: <c>v.Next(summary.Named("summary"))</c>.
    /// </para>
    /// </summary>
    public T Next<T>(IShape<T> shape)
    {
      if (_state is null)
        throw new InvalidOperationException(FlowState.NoLayout);

      return _state.Next(shape);
    }
  }
}
