using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A flow in progress: where the next child goes, how much has been consumed, and the rule about
  /// a sibling that consumed nothing. Every spelling of a flow runs through this, so the arithmetic
  /// they do and the diagnostics they produce cannot drift apart.
  /// </summary>
  internal sealed class FlowState : LayoutState
  {
    // The spec's §3.4 wanted this sentence to say WHICH kind of nothing the sibling was — absent
    // because a boundary absorbed it, or an empty region genuinely read. It stays one sentence, and
    // deliberately: the rule is about consumption, not about absorption, which is what
    // BoundaryProjectionTests.TheNoteIsAboutConsumptionRatherThanAboutAbsorption pins. Splitting the
    // wording would make the note claim a cause it does not test for. The presence needed to do it
    // now reaches here, so the change is a decision rather than a missing seam.
    private const string SiblingNote = "the preceding sibling consumed nothing at this position";

    private int _along;
    private int _across;
    private int _previous;

    public FlowState(IProjection owner, Orientation orientation, ISpace extent, ProjectionContext context)
      : base(owner, extent, context)
    {
      Orientation = orientation;
    }

    private Orientation Orientation { get; }

    /// <summary>
    /// What the flow consumed: along its axis the sum of the children's advances, across it the
    /// widest of them.
    /// </summary>
    public override Size Consumed => Orientation == Orientation.Vertical
      ? new Size(_across, _along)
      : new Size(_along, _across);

    public override string DeclaredNothing => NothingDeclared("a flow");

    /// <summary>
    /// Where the next child goes: a flow consumes along its own axis only, so the cursor moves
    /// along that axis and nowhere else.
    /// </summary>
    private Offset Cursor => Orientation == Orientation.Vertical ? new Offset(0, _along) : new Offset(_along, 0);

    /// <summary>
    /// Takes the next child, knowing its result type — what a cursor lambda declares, and the
    /// reason it costs neither a box nor a cast.
    /// </summary>
    public override T Next<T>(IProjection<T> projection, string? declared)
    {
      var cursor = Cursor;
      Admit(projection, cursor);

      var scope = Context.Advance(cursor).WithUseSite(UseSite.From(declared, Count + 1));
      AppliedResult<T> applied;

      try
      {
        applied = ProjectionEngine.Apply(projection, Extent.GetSubspace(cursor), scope);
      }
      catch (ProjectionException failure) when (FollowsAnEmptySibling(failure, cursor))
      {
        throw failure.WithNote(SiblingNote);
      }

      Advance(applied.Advance, applied.Presence);
      return applied.Value;
    }

    /// <summary>
    /// A sibling that consumed nothing — an absorbed boundary, most often — leaves this child
    /// reading the very cells that just failed, so it fails the same way for the same reason. The
    /// absorption warning may since have been rolled back by an enclosing choice, in which case
    /// this note is the only thing left saying so. It has to be the same cell, though: a child that
    /// re-anchored itself and failed elsewhere failed on its own account.
    /// <para>
    /// This is a flow's rule alone: it explains a child failing on cells a predecessor declined to
    /// take, and in an overlay there is no such relation — every child starts from the same origin
    /// whatever its neighbours did.
    /// </para>
    /// </summary>
    private bool FollowsAnEmptySibling(ProjectionException failure, Offset cursor)
      => Count > 0 && _previous == 0 && failure.Location.IsAt(Context.Origin + cursor);

    private void Advance(Size advance, Presence presence)
    {
      _previous = Along(advance);
      _along += _previous;
      _across = Math.Max(_across, Across(advance));
      Took(presence);
    }

    private int Along(Size size) => Orientation == Orientation.Vertical ? size.Height : size.Width;

    private int Across(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;
  }
}
