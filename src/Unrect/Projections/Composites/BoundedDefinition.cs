using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Bounds its extent at a landmark and applies the inner projection to what comes before it.
  /// Where an offset says where a projection starts by content, this says where it ends by content.
  /// <para>
  /// It is a wrapper projection rather than an area strategy for one decisive reason: a strategy
  /// has no context, so it could not record the <c>Info</c> that <c>orEnd</c> owes the caller. The
  /// arithmetic underneath is the same "rows up to the landmark by full width" a strategy would do.
  /// </para>
  /// </summary>
  internal sealed class BoundedDefinition<TSpace, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    public BoundedDefinition(IProjectionDefinition<TSpace, TResult> inner, Landmark landmark, bool orEnd, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Landmark = landmark ?? throw new ArgumentNullException(nameof(landmark));
      OrEnd = orEnd;
      Children = new[] { new Child(inner, default) };
    }

    private IProjectionDefinition<TSpace, TResult> Inner { get; }
    private Landmark Landmark { get; }
    private bool OrEnd { get; }

    private bool IsVertical => Landmark.Orientation == Orientation.Vertical;

    public override string Description => Spelling(Landmark);

    public override IReadOnlyList<Child> Children { get; }

    /// <summary>Like a pad: a bound the user wrote as part of a projection is not a level of the tree.</summary>
    public override bool IsWrapper => true;

    /// <summary>
    /// The refusal a second bound gets, naming the modifier that asked for it. A bound written
    /// straight onto a bounded projection — or onto a clone of one, since a clone is not a layer —
    /// would replace the end already declared, and the landmark it replaced would never be looked
    /// for: the erasure is silent and total.
    /// </summary>
    public ArgumentException AlreadyEnded(Landmark landmark)
      => new ArgumentException(
        $"{ProjectionContext.DescribeThrough(this)} already ends at a landmark, and {Spelling(landmark)} would "
        + "replace that end rather than bound what is inside it — a projection has one end, and the replaced "
        + "landmark is never even sought. Bound it once: a bound written outside a wrapper nests instead of "
        + "replacing, so a Select or a Padded between the two leaves both ends in force.",
        "projection");

    /// <summary>A bound is driven along its landmark's axis: a row landmark is looked for on each row span, a column landmark on each column span.</summary>
    public override Axes Axis => Landmark.Orientation.Of();

    public override IProjector<TSpace, TResult> Start(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// A span matching the landmark is refused and ends the bound; every other span is the bound's,
    /// forwarded to the inner while it takes them and consumed by the bound after it stops — a
    /// bound is consumed in full, as a declared area is, which is what puts the next sibling on the
    /// landmark. A landmark never seen is the failure or the Info <c>orEnd</c> decides.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, TResult>
    {
      private readonly BoundedDefinition<TSpace, TResult> _bounded;
      private readonly ProjectorScope<TSpace> _scope;
      private ChildProjector<TSpace, TResult>? _inner;
      private Plane<TSpace>? _first;
      private int _offered;
      private bool _found;
      private bool _innerRefused;
      private bool _finished;
      private bool _closed;

      public Machine(BoundedDefinition<TSpace, TResult> bounded, ProjectorScope<TSpace> scope)
      {
        _bounded = bounded;
        _scope = scope;
      }

      private Orientation Along => _bounded.Landmark.Orientation;

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Context.Failure(_bounded, $"{ProjectionContext.Describe(_bounded)} was fed a span after it was closed", span, null, null, isFault: true);

        if (_finished)
          return false;

        if (_bounded.Landmark.Find(span.Erased()) is not null)
        {
          _found = true;
          _finished = true;
          return false;
        }

        _first ??= span;
        _offered++;
        _inner ??= _scope.Start(_bounded.Children[0], _bounded.Inner, Spans.Empty(span, _scope.Driver), inheritSite: true);

        if (!_innerRefused && !_inner.Next(span))
          _innerRefused = true;

        return true;
      }

      public Settlement<TResult> Close()
      {
        _closed = true;

        var extent = _first is Plane<TSpace> first ? Spans.Region(first, _offered, Along) : _scope.Anchor;

        if (!_found && !_bounded.OrEnd)
          throw _scope.Context.Failure(ProjectionContext.Through(_bounded), $"{_bounded.Landmark.Description} exists to end this projection", extent, null, null);

        if (!_found)
          _scope.Context.Report(DiagnosticSeverity.Info, _bounded, $"{_bounded.Landmark.Description} exists to end this projection, so it ran to the end of the space", extent);

        _inner ??= _scope.Start(_bounded.Children[0], _bounded.Inner, _scope.Anchor, inheritSite: true);

        var settlement = _inner.Close();

        return new Settlement<TResult>(settlement.Value, _bounded.Consumed(_offered, _inner.Advance), _inner.Presence);
      }
    }

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;
      var found = Landmark.Find(extent.Erased());
      var limit = found ?? (IsVertical ? size.Height : size.Width);

      // A missing end is a disagreement about the shape of the data, not a bug in the reading code,
      // so it is absorbable — and it is blamed on the projection being bounded, because "Until" is
      // not what the user was looking for.
      if (found is null && !OrEnd)
        throw context.Failure(ProjectionContext.Through(this), $"{Landmark.Description} exists to end this projection", extent, null, null);

      // Declared alternation rather than tolerance after a failure, so Info rather than Warning.
      if (found is null)
        context.Report(DiagnosticSeverity.Info, this, $"{Landmark.Description} exists to end this projection, so it ran to the end of the space", extent);

      var applied = ProjectionEngine.Apply(Inner, extent.Slice(Bound(limit, size)), context);

      // The bound is consumed whether or not the inner projection used it all, exactly as a
      // declared area is: that is what puts the next sibling ON the landmark rather than somewhere
      // before it. Across the axis, only what the inner projection reached — bounding rows must not
      // claim columns.
      return new ProjectionResult<TResult>(applied.Value, Consumed(limit, applied.Advance), applied.Presence);
    }

    /// <summary>The word a reader wrote for a bound on this axis, which is also how it describes itself.</summary>
    private static string Spelling(Landmark landmark)
      => landmark.Orientation == Orientation.Vertical ? "Until" : "UntilColumn";

    private Area Bound(int limit, Size size)
      => IsVertical ? new Area(size.Width, limit) : new Area(limit, size.Height);

    private Size Consumed(int limit, Size advance)
      => IsVertical ? new Size(advance.Width, limit) : new Size(limit, advance.Height);
  }
}
