using System;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The cursor a layout lambda declares its children with — the same one for a flow and for an
  /// overlay, because what differs between them is what the composite does between children, not
  /// how a child is declared. Two members: <see cref="Next{T}"/> records a child and hands back the
  /// slot its value will arrive in, and <see cref="Build{TResult}"/> closes the layout with the
  /// combiner that reads those slots. There is deliberately nothing to ask about where the cursor
  /// is or how much is left, because a declaration says what the data looks like, not how to walk
  /// it — and nothing in the lambda has a value to branch on, because the lambda runs once, when
  /// the layout is declared, before any space exists.
  /// <para>
  /// It is a <c>ref struct</c>, so the compiler refuses every way of using it outside the lambda
  /// that received it — capturing it in the combiner or a local function, storing it in a field,
  /// an array, or a list, returning it, or carrying it into a deferred query. The one cursor that
  /// is not a live one is <c>default</c>, which every call refuses at run time.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the enclosing layout is declared over.</typeparam>
  public readonly ref struct LayoutCursor<TSpace>
    where TSpace : class, ISpace
  {
    private readonly LayoutBuilder<TSpace>? _builder;

    internal LayoutCursor(LayoutBuilder<TSpace>? builder)
    {
      _builder = builder;
    }

    /// <summary>
    /// Declares <paramref name="projection"/> as the next child and hands back the slot its value
    /// will be read from: in a flow, at the position the children before it left off; in an
    /// overlay, against the whole extent, wherever the projection's own placement puts it. Calling
    /// it is what puts a projection in the layout, so the order of the calls is the order of the
    /// children — nothing else about the lambda is a declaration.
    /// <para>
    /// Diagnostics call the child by the best name available: one given with
    /// <c>v.Next(summary.Named("summary"))</c>, else the identifier it was written as —
    /// <c>v.Next(transactions)</c> reads as <c>'transactions'</c> — else its kind and position, as
    /// <c>Cell#2</c>. So hoist a projection into a well-named local, or name it inline; anything
    /// that is not a plain identifier, such as <c>v.Next(row.Down(1))</c>, falls back to the
    /// position.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What the child reads.</typeparam>
    /// <param name="projection">The projection to read here.</param>
    /// <param name="declared">
    /// Supplied by the compiler as the text of the <paramref name="projection"/> argument. It is
    /// not a naming API — pass <c>.Named(…)</c> when you want to choose what a child is called.
    /// </param>
    public Slot<T> Next<T>(IProjectionDefinition<TSpace, T> projection, [CallerArgumentExpression("projection")] string? declared = null)
    {
      if (_builder is null)
        throw new InvalidOperationException(LayoutBuilder<TSpace>.NoLayout);

      return _builder.Next(projection, declared);
    }

    /// <summary>
    /// Closes the layout: <paramref name="combine"/> builds the result from what the children read,
    /// through the slots <see cref="Next{T}"/> handed back — <c>read.Of(title)</c>. It runs once
    /// per application, after every child has been read, and it is the only way to a layout, so a
    /// lambda that forgets it does not compile. A layout that declared no child is refused here.
    /// </summary>
    /// <typeparam name="TResult">What the layout builds.</typeparam>
    /// <param name="combine">The result, from what the children read.</param>
    public Layout<TSpace, TResult> Build<TResult>(Func<Reading, TResult> combine)
    {
      if (_builder is null)
        throw new InvalidOperationException(LayoutBuilder<TSpace>.NoLayout);

      return _builder.Build(combine);
    }
  }
}
