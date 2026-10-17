using System;
using System.Runtime.CompilerServices;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The cursor a layout lambda declares its children with — the same one for a flow and for an
  /// overlay, because what differs between them is what the composite does between children, not
  /// how a child is declared. One member: <see cref="Next{T}"/> declares the next child and hands
  /// back what it read. There is deliberately nothing to ask about where the cursor is or how much
  /// is left, because a declaration says what the data looks like, not how to walk it.
  /// <para>
  /// The lambda runs twice. Once when the layout is declared, with no space in hand, so the cursor
  /// can learn the children: every <c>Next</c> records its child and hands back the empty value of
  /// its type — zero, an empty string, an empty list. And once per application, after every child
  /// has settled, so the same lambda can assemble the result: every <c>Next</c> hands back the
  /// value its child read. The two runs must declare the same children in the same order — a shape
  /// that depends on a value is a fault when it runs — and the lambda must survive the first, which
  /// rules out reading a cell in it (a point read belongs in the leaf) or reaching into an object
  /// nothing has built yet (a <c>Select</c> after the layout is where that goes).
  /// </para>
  /// <para>
  /// It is a <c>ref struct</c>, so the compiler refuses every way of using it outside the lambda
  /// that received it — capturing it in a local function, storing it in a field, an array, or a
  /// list, returning it, or carrying it into a deferred query. The one cursor that is not a live
  /// one is <c>default</c>, which every call refuses at run time.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space the enclosing layout is declared over.</typeparam>
  public readonly ref struct LayoutCursor<TSpace>
    where TSpace : class, ISpace
  {
    private readonly LayoutPass<TSpace>? _pass;

    internal LayoutCursor(LayoutPass<TSpace>? pass)
    {
      _pass = pass;
    }

    /// <summary>
    /// Whether this is the declaration-time run, on which every <c>Next</c> hands back nothing. For
    /// a layout this library writes around a user's own type — the reflective table binder — which
    /// cannot promise that type survives being built from nothing; a declaration written in the
    /// vocabulary assembles and needs no such door.
    /// </summary>
    internal bool Recording => _pass is RecordingPass<TSpace>;

    /// <summary>
    /// Declares <paramref name="projection"/> as the next child and hands back what it read: in a
    /// flow, at the position the children before it left off; in an overlay, against the whole
    /// extent, wherever the projection's own placement puts it. Calling it is what puts a
    /// projection in the layout, so the order of the calls is the order of the children — nothing
    /// else about the lambda is a declaration.
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
    public T Next<T>(IProjectionDefinition<TSpace, T> projection, [CallerArgumentExpression("projection")] string? declared = null)
    {
      if (_pass is null)
        throw new InvalidOperationException(LayoutPass<TSpace>.NoLayout);

      return _pass.Next(projection, declared);
    }
  }
}
