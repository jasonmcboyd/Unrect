using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// One flow in progress: where the next child goes, how much has been consumed, and the rule about
  /// a sibling that consumed nothing. Both spellings of a flow — children as arguments and children
  /// as <c>Next</c> calls — run through this, so the arithmetic they do and the diagnostics they
  /// produce cannot drift apart.
  /// </summary>
  internal sealed class FlowState
  {
    private const string Outside = "A layout cursor cannot be used outside the layout that created it";

    /// <summary>
    /// Being outside a layout covers two different bugs, so the messages name which one:
    /// <see cref="LayoutCursor"/> refuses a cursor that never had a layout, and this class refuses
    /// one whose layout has already returned.
    /// </summary>
    internal const string NoLayout = Outside + "; this one never had a layout.";

    /// <inheritdoc cref="NoLayout"/>
    internal const string LayoutReturned = Outside + "; this one was used after its layout returned.";

    private const string SiblingNote = "the preceding sibling consumed nothing at this position";

    private int _along;
    private int _across;
    private int _previous;
    private bool _closed;

    public FlowState(IShape owner, Orientation orientation, ISpace extent, ShapeContext context)
    {
      Owner = owner;
      Orientation = orientation;
      Extent = extent;
      Context = context;
    }

    private IShape Owner { get; }
    private Orientation Orientation { get; }
    private ISpace Extent { get; }
    private ShapeContext Context { get; }

    /// <summary>How many children the flow has taken.</summary>
    public int Count { get; private set; }

    /// <summary>
    /// What the flow consumed: along its axis the sum of the children's advances, across it the
    /// widest of them.
    /// </summary>
    public Size Consumed => Orientation == Orientation.Vertical
      ? new Size(_across, _along)
      : new Size(_along, _across);

    /// <summary>Ends the flow, after which no cursor may add to it.</summary>
    public void Close() => _closed = true;

    /// <summary>
    /// Takes the next child, knowing its result type — what a cursor lambda declares, and the reason
    /// it costs neither a box nor a cast.
    /// </summary>
    public T Next<T>(IShape<T> shape)
    {
      var cursor = Admit(shape);
      AppliedResult<T> applied;

      try
      {
        applied = ShapeEngine.Apply(shape, Extent.GetSubspace(cursor), Context.Advance(cursor));
      }
      catch (ShapeException failure) when (FollowsAnEmptySibling(failure, cursor))
      {
        throw failure.WithNote(SiblingNote);
      }

      Advance(applied.Advance);
      return applied.Value;
    }

    /// <summary>
    /// Takes the next child from a list of them, whose result types the stack that holds the list
    /// has already erased into its combine function.
    /// </summary>
    public object? NextUntyped(IShape shape)
    {
      var cursor = Admit(shape);
      AppliedResult<object?> applied;

      try
      {
        applied = ShapeEngine.ApplyUntyped(shape, Extent.GetSubspace(cursor), Context.Advance(cursor));
      }
      catch (ShapeException failure) when (FollowsAnEmptySibling(failure, cursor))
      {
        throw failure.WithNote(SiblingNote);
      }

      Advance(applied.Advance);
      return applied.Value;
    }

    /// <summary>
    /// Lets a child into the flow and says where it goes: a flow consumes along its own axis only,
    /// so the cursor moves along that axis and nowhere else.
    /// </summary>
    private Offset Admit(IShape? shape)
    {
      if (_closed)
        throw new InvalidOperationException(LayoutReturned);

      var cursor = Orientation == Orientation.Vertical ? new Offset(0, _along) : new Offset(_along, 0);

      // A null shape is a hole in the declaration, not a shape of data: it is reported where the
      // child would have gone, and no tolerance boundary may absorb it.
      if (shape is null)
        throw Context.Advance(cursor).Failure(
          Owner,
          $"a null shape was declared as child {Count + 1}",
          RemainingAt(cursor),
          null,
          null,
          isProjectionFault: true);

      return cursor;
    }

    /// <summary>
    /// The space left at <paramref name="cursor"/>, or the whole extent when the flow has no room
    /// there to slice. A hole in the declaration has to be reportable from any position the flow
    /// reached, so the message and the cursor's location outrank an exact availability figure.
    /// </summary>
    private ISpace RemainingAt(Offset cursor)
      => cursor.Size.Width > Extent.Area.Size.Width || cursor.Size.Height > Extent.Area.Size.Height
        ? Extent
        : Extent.GetSubspace(cursor);

    /// <summary>
    /// A sibling that consumed nothing — an absorbed boundary, most often — leaves this child
    /// reading the very cells that just failed, so it fails the same way for the same reason. The
    /// absorption warning may since have been rolled back by an enclosing choice, in which case this
    /// note is the only thing left saying so. It has to be the same cell, though: a child that
    /// re-anchored itself and failed elsewhere failed on its own account.
    /// </summary>
    private bool FollowsAnEmptySibling(ShapeException failure, Offset cursor)
      => Count > 0 && _previous == 0 && failure.Location.IsAt(Context.Origin + cursor);

    private void Advance(Size advance)
    {
      _previous = Along(advance);
      _along += _previous;
      _across = Math.Max(_across, Across(advance));
      Count++;
    }

    private int Along(Size size) => Orientation == Orientation.Vertical ? size.Height : size.Width;

    private int Across(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;
  }
}
