using System;
using System.Runtime.CompilerServices;

namespace Unrect.Projections
{
  /// <summary>
  /// A layout declared as a sequence of <see cref="LayoutCursor.Next{T}"/> calls, each returning
  /// the value its projection read, and the whole returning whatever the caller builds from them.
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
  /// that received it — capturing it in a nested lambda or local function, storing it in a field,
  /// an array, or a list, returning it, or carrying it into a deferred query. The one cursor that
  /// is not a live one is <c>default(LayoutCursor)</c>, which every call refuses at run time.
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
    /// Reads <paramref name="projection"/> and returns its value: in a flow, at the position the
    /// children before it left off; in an overlay, against the whole extent, wherever the
    /// projection's own placement puts it. Calling it is what puts a projection in the layout, so
    /// the order of the calls is the order of the children — nothing else about the lambda is a
    /// declaration.
    /// <para>
    /// Diagnostics call the child by the best name available: one given with
    /// <c>v.Next(summary.Named("summary"))</c>, else the identifier it was written as —
    /// <c>v.Next(transactions)</c> reads as <c>'transactions'</c> — else its kind and position, as
    /// <c>Cell#2</c>. So hoist a projection into a well-named local, or name it inline; anything
    /// that is not a plain identifier, such as <c>v.Next(row.Down(1))</c>, falls back to the
    /// position.
    /// </para>
    /// </summary>
    /// <param name="projection">The projection to read here.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="projection"/> argument. It is
    /// not a naming API — pass <c>.Named(…)</c> when you want to choose what a child is called.
    /// </param>
    public T Next<T>(IProjection<T> projection, [CallerArgumentExpression("projection")] string? declared = null)
    {
      if (_state is null)
        throw new InvalidOperationException(LayoutState.NoLayout);

      return _state.Next(projection, declared);
    }

    /// <summary>
    /// The layout in progress, or null for <c>default(LayoutCursor)</c> — how a demanding layout
    /// re-types this cursor without the state leaving the assembly.
    /// </summary>
    internal LayoutState? State => _state;
  }

  /// <summary>
  /// EXPERIMENT (typed-spaces): the cursor form of <see cref="Layout{TSpace, TResult}"/>. Identical
  /// to <see cref="LayoutCursor"/> in every respect but one — <see cref="Next{T}"/> accepts a child
  /// demanding at most <typeparamref name="TSpace"/>, which is what makes a layout's demand the
  /// union of its children's, checked as each is declared rather than when the parse runs.
  /// </summary>
  /// <typeparam name="TSpace">The space the enclosing layout is declared over.</typeparam>
  public readonly ref struct LayoutCursor<TSpace>
    where TSpace : class, Core.ISpace
  {
    private readonly LayoutState? _state;

    internal LayoutCursor(LayoutState? state)
    {
      _state = state;
    }

    /// <inheritdoc cref="LayoutCursor.Next{T}"/>
    /// <param name="projection">
    /// The projection to read here. A plain projection converts in by variance; a projection
    /// demanding more than <typeparamref name="TSpace"/> does not compile, and the fix is to say so
    /// on the layout.
    /// </param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="projection"/> argument.</param>
    public T Next<T>(IProjection<TSpace, T> projection, [CallerArgumentExpression("projection")] string? declared = null)
    {
      if (_state is null)
        throw new InvalidOperationException(LayoutState.NoLayout);

      // The one cast the typed layer makes, licensed by the rule on IProjection<TSpace, TResult>:
      // every projection this library builds implements IProjection<T>, and TSpace is a phantom the
      // engine never sees.
      return _state.Next((IProjection<T>)projection, declared);
    }
  }

  /// <summary>
  /// EXPERIMENT (typed-spaces): a layout over a space offering at least <typeparamref
  /// name="TSpace"/>. The demanding twin of <see cref="Layout{TResult}"/>;
  /// <c>VerticalFlow&lt;TSpace, TResult&gt;</c> and its siblings take one of these.
  /// </summary>
  /// <typeparam name="TSpace">The space the layout is declared over.</typeparam>
  /// <typeparam name="TResult">What the layout builds from what its children read.</typeparam>
  /// <param name="cursor">The cursor the layout declares its children with.</param>
  public delegate TResult Layout<TSpace, TResult>(LayoutCursor<TSpace> cursor)
    where TSpace : class, Core.ISpace;
}
