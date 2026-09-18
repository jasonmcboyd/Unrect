using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The handle the engine wraps around every child it starts: keeps the spans the child was
  /// offered, feeds the child's own machine the spans of its inner region as its placement
  /// resolves them (<see cref="StreamingPlacement{TSpace}"/>), and settles on what the child kept.
  /// A child whose axis is not the driver's, or whose placement cannot be driven per span, is
  /// <em>held</em>: it takes every span offered, and at <c>Close</c> is placed over the region they
  /// cover (<see cref="EagerPlacement"/>) and driven along its own axis.
  /// <para>
  /// This is also where the protocol is validated: a machine that takes a span after refusing one,
  /// or settles on more than it was offered, is a fault blamed on the node. And it is where the
  /// tree of open machines lives: children started under this handle report to it, and it answers
  /// for its subtree when the session asks what may still be read.
  /// </para>
  /// </summary>
  internal sealed class ChildProjector<TSpace, T> : IProjector<TSpace, T>, IChildHandle<TSpace, T>, IChildRegistry<TSpace>
    where TSpace : class, ISpace
  {
    private enum Phase
    {
      Offset,
      Size,
      Finished,
      Closed,
    }

    private readonly SessionScope<TSpace> _scope;
    private readonly ProjectorScope<TSpace> _parent;
    private readonly ProjectorScope<TSpace> _child;
    private readonly IProjectionDefinition<TSpace, T> _definition;
    private readonly Plane<TSpace> _anchor;
    private readonly bool _strict;
    private readonly Orientation _driver;
    private readonly List<Plane<TSpace>> _offered = new List<Plane<TSpace>>();
    private readonly List<Plane<TSpace>> _pending = new List<Plane<TSpace>>();
    private readonly List<IChildHandle<TSpace>> _children = new List<IChildHandle<TSpace>>();

    /// <summary>The per-span placement, or null for a held child.</summary>
    private readonly StreamingPlacement<TSpace>? _placement;

    private Phase _phase;
    private int _column;
    private bool _startNext;
    private int _innerStart;
    private int _taken;
    private IProjector<TSpace, T>? _inner;
    private bool _innerRefused;
    private Settlement<T> _settlement;

    internal ChildProjector(SessionScope<TSpace> scope, ProjectorScope<TSpace> parent, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, bool strict)
    {
      _scope = scope;
      _parent = parent;
      _definition = definition;
      _anchor = anchor;
      _strict = strict;
      _driver = scope.Driver;
      _child = PathRenderer.Skipped(definition) ? parent.Blaming(definition) : parent.Descend(definition);

      // Driven or held is PlacementRules' decision, shared with the cost report so the two agree.
      if (PlacementRules.Streams(definition, _driver, out var offset, out var size, out var derived, out _))
        _placement = new StreamingPlacement<TSpace>(offset, size, derived, definition, _driver, strict);

      Reach = _placement is null ? Reach.Extent : definition.Reach;
    }

    private bool Held => _placement is null;

    // --- The tree of open machines ---------------------------------------------------------------

    public void Opened(IChildHandle<TSpace> child) => _children.Add(child);

    public void Closed(IChildHandle<TSpace> child) => _children.Remove(child);

    /// <summary>
    /// The oldest row this subtree may still read, and the innermost machine that needs it: this
    /// machine's own hold, then every open child's answer, the deeper taking a tie.
    /// </summary>
    public Hold? Retained(int current)
    {
      Hold? oldest = RetainFrom(current) is int own ? new Hold(own, _definition) : (Hold?)null;

      foreach (var child in _children)
        if (child.Retained(current) is Hold held && (oldest is null || held.Row <= oldest.Value.Row))
          oldest = held;

      return oldest;
    }

    /// <summary>
    /// The first source row this machine itself may still read, or null when it holds none. A held
    /// child may read all of what it was offered when it is placed at close; a child still
    /// resolving an offset or a width reads back to where that began; a placed child reads back
    /// only as far as its own node's <see cref="DefinitionNode.Retains"/> says.
    /// </summary>
    private int? RetainFrom(int current)
    {
      if (_phase == Phase.Closed || _offered.Count == 0)
        return null;

      if (Held || _phase == Phase.Offset)
        return Row(_offered[0]);

      var innerStart = Row(_offered[_innerStart]);

      if (!_placement!.Derived && _placement.Width is null)
        return innerStart;

      if (_inner is IHolding holding)
      {
        if (holding.HeldFrom is not int position)
          return null;

        return Row(_offered[Math.Min(_innerStart + position, _offered.Count - 1)]);
      }

      var own = _definition is DefinitionNode node ? node.Retains : Reach.Extent;

      if (own.IsNone)
        return null;

      if (own.IsExtent)
        return innerStart;

      return Math.Max(innerStart, current - own.Count!.Value + 1);
    }

    private int Row(Plane<TSpace> span) => _driver == Orientation.Vertical ? span.Origin.Height : span.Origin.Width;

    // --- What the parent reads off the handle --------------------------------------------------

    /// <summary>What this child announces to the parent that started it.</summary>
    public Reach Reach { get; }

    public Offset Offset { get; private set; }

    /// <summary>What the child kept, offset included, from where it was started.</summary>
    public Size Advance { get; private set; }

    /// <summary>What the child itself consumed, offset excluded — a repeat's productivity guard reads this.</summary>
    public Size Consumed { get; private set; }

    public Presence Presence { get; private set; }

    /// <summary>
    /// Set when the placement failed and the child was started non-strictly: the parent reads the
    /// refusal here rather than catching a failure. A strict placement throws instead.
    /// </summary>
    public bool PlacementFailed { get; private set; }

    /// <summary>The spans offered to this child that it did not keep, oldest first — for the parent to feed to the successor.</summary>
    public IEnumerable<Plane<TSpace>> Shortfall()
    {
      var kept = PlacementFailed ? 0 : Spans.Along(Advance, _driver);

      for (var index = kept; index < _offered.Count; index++)
        yield return _offered[index];
    }

    // --- The protocol --------------------------------------------------------------------------

    public bool Next(Plane<TSpace> span)
    {
      if (_phase == Phase.Closed)
        throw Fault("was fed a span after it was closed");

      if (_phase == Phase.Finished)
        return false;

      _offered.Add(span);

      try
      {
        return Take(span);
      }
      catch
      {
        // A span the machine threw on was not taken.
        if (_offered.Count > 0 && ReferenceEquals(_offered[_offered.Count - 1].Space, span.Space) && _offered[_offered.Count - 1].Equals(span))
          _offered.RemoveAt(_offered.Count - 1);

        throw;
      }
    }

    private bool Take(Plane<TSpace> span)
    {
      if (Held)
        return true;

      if (_phase == Phase.Offset)
      {
        if (_startNext)
        {
          _startNext = false;
        }
        else
        {
          var region = Spans.Region(_offered[0], _offered.Count, _driver).Erased();
          var step = _placement!.Advance(region, _offered.Count - 1, Spans.Across(span.Area.Size, _driver), _parent, out var refused);

          if (refused)
          {
            PlacementFailed = true;
            return Refuse();
          }

          if (step == OffsetStep.Skip)
            return true;

          _column = _driver == Orientation.Vertical ? _placement.Offset.Width : _placement.Offset.Height;
          Offset = _placement.Offset;

          if (step == OffsetStep.StartNext)
          {
            _startNext = true;
            return true;
          }
        }

        _innerStart = _offered.Count - 1;
        _phase = Phase.Size;
        _inner = _definition.Build(_child.Within(this, Spans.Empty(Cut(span, _column, null), _driver), _driver));
      }

      return TakeInner(span);
    }

    private bool TakeInner(Plane<TSpace> span)
    {
      var available = Cut(span, _column, null);
      var placement = _placement!;

      if (placement.Derived)
      {
        if (!InnerNext(available))
        {
          _innerRefused = true;
          return Refuse();
        }

        _taken++;
        return true;
      }

      var take = placement.Take(InnerRegion(_taken + 1), _taken, _child);

      if (placement.Failed)
      {
        PlacementFailed = true;
        return Refuse();
      }

      if (!take)
      {
        // The region the rule settled on, read before the refused span leaves the list: with
        // nothing taken it is the empty region at that span, which still has a width to settle.
        var settled = placement.Width is null ? InnerRegion(_taken) : default;

        Refuse();

        if (placement.Width is null)
          SettleWidth(settled, rowsSettled: true);

        return false;
      }

      _taken++;

      if (placement.Width is int width)
      {
        FeedInner(Cut(available, 0, width));
        return true;
      }

      _pending.Add(available);
      SettleWidth(InnerRegion(_taken), rowsSettled: false);
      return true;
    }

    /// <summary>Asks the placement for its width, and once it has one feeds the inner every span that waited for it.</summary>
    private void SettleWidth(Plane<ISpace> region, bool rowsSettled)
    {
      var placement = _placement!;

      if (!placement.TrySettleWidth(region, _taken, rowsSettled, _child))
      {
        if (placement.Failed)
        {
          PlacementFailed = true;
          _phase = Phase.Finished;
        }

        return;
      }

      foreach (var pending in _pending)
        FeedInner(Cut(pending, 0, placement.Width!.Value));

      _pending.Clear();
    }

    /// <summary>The span in hand was not taken: it is not among those offered, and nothing after it is.</summary>
    private bool Refuse()
    {
      _offered.RemoveAt(_offered.Count - 1);
      _phase = Phase.Finished;
      return false;
    }

    public Settlement<T> Close()
    {
      if (_phase == Phase.Closed)
        throw Fault("was closed twice");

      try
      {
        if (Held || _phase == Phase.Offset)
          ResolveEagerly();
        else
          SettleStreaming();
      }
      finally
      {
        _phase = Phase.Closed;
        _scope.Owner?.Closed(this);
      }

      if (Spans.Along(Advance, _driver) > _offered.Count)
        throw Fault($"settled on {Spans.Along(Advance, _driver)} spans but was offered {_offered.Count}");

      return _settlement;
    }

    public object? CloseBoxed() => Close().Value;

    private void SettleStreaming()
    {
      var placement = _placement!;

      if (!placement.Derived)
      {
        if (placement.Width is null && !PlacementFailed)
          SettleWidth(InnerRegion(_taken), rowsSettled: true);

        if (PlacementFailed)
        {
          SettleNothing();
          return;
        }

        if (!placement.Complete(_taken))
        {
          var region = InnerRegion(_taken);

          if (_strict)
            throw _child.Failure(_definition, $"an extent of {EngineRules.Describe(placement.Declared)} does not fit here", region, placement.Declared, null);

          PlacementFailed = true;
          SettleNothing();
          return;
        }
      }

      // An inner that was fed nothing closes over the empty region its rule settled on, cut to the
      // settled width: a discovered block over blank space is 0x0, not 0 rows of the anchor's width.
      if (!placement.Derived && _taken == 0 && _inner is not null)
        _inner = _definition.Build(_child.Within(this, Narrow(InnerPlane(0), placement.Width!.Value), _driver));

      var settlement = InnerClose();
      var declared = !placement.Derived;
      var consumed = declared ? Spans.ToSize(_taken, placement.Width!.Value, _driver) : settlement.Consumed;

      Settle(settlement.Value, consumed, EngineRules.Settled(settlement.Presence, declared, consumed));
    }

    /// <summary>
    /// The held path: the child is placed over everything it was offered, with the strategies'
    /// whole-region form, and its machine driven along its own axis over the region that came out.
    /// </summary>
    private void ResolveEagerly()
    {
      var region = _offered.Count == 0 ? Spans.Empty(_anchor, _driver) : Spans.Region(_offered[0], _offered.Count, _driver);

      if (!EagerPlacement.TryPlace(_definition, region, _driver, _parent, _strict, out var offset, out var inner, out var scope, out var declared))
      {
        PlacementFailed = true;
        SettleNothing();
        return;
      }

      Offset = offset;

      var along = _definition.Axis.Along(_driver);
      var machine = _definition.Build(scope.Within(this, Spans.Empty(inner, along ?? _driver), along ?? _driver));
      Settlement<T> settlement;

      try
      {
        settlement = _scope.Drive(machine, inner, along);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw Threw(exception, inner);
      }

      var consumed = declared ? inner.Area.Size : settlement.Consumed;

      Settle(settlement.Value, consumed, EngineRules.Settled(settlement.Presence, declared, consumed));
    }

    private void Settle(T value, Size consumed, Presence presence)
    {
      Consumed = consumed;
      Presence = presence;
      Advance = Offset.Size + consumed;
      _settlement = new Settlement<T>(value, consumed, presence);
    }

    private void SettleNothing()
    {
      Offset = default;
      Settle(default!, new Size(0, 0), Presence.Empty);
    }

    // --- The inner machine, and what it threw ---------------------------------------------------

    private void FeedInner(Plane<TSpace> span)
    {
      if (_innerRefused)
        return;

      if (!InnerNext(span))
        _innerRefused = true;
    }

    private bool InnerNext(Plane<TSpace> span)
    {
      try
      {
        return _inner!.Next(span);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw Threw(exception, InnerPlane(_taken + 1));
      }
    }

    private Settlement<T> InnerClose()
    {
      try
      {
        return _inner!.Close();
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (Exception exception)
      {
        throw Threw(exception, InnerPlane(_taken));
      }
    }

    private ProjectionException Threw(Exception exception, Plane<TSpace> extent)
      => _child.Failure(
        _definition,
        $"the projection threw {exception.GetType().Name}: {exception.Message}",
        extent,
        null,
        exception,
        EngineRules.IsFault(exception));

    private ProjectionException Fault(string violation)
    {
      var region = _offered.Count == 0 ? _anchor : Spans.Region(_offered[0], _offered.Count, _driver);

      return _child.Failure(_definition, $"{PathRenderer.Describe(_definition)} {violation}; a machine that broke the protocol is a bug in the node, not a shape of data", region, null, null, isFault: true);
    }

    // --- Regions -------------------------------------------------------------------------------

    /// <summary>The inner region <paramref name="rows"/> spans tall, from where the inner started, erased for a rule.</summary>
    private Plane<ISpace> InnerRegion(int rows)
      => (rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], rows, _driver))
        .Slice(Across(_column))
        .Erased();

    /// <summary>The same region as a plane over the space, clamped to what was offered, for a message.</summary>
    private Plane<TSpace> InnerPlane(int rows)
    {
      if (_innerStart >= _offered.Count)
        return _anchor;

      var region = rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], Math.Min(rows, _offered.Count - _innerStart), _driver);

      return region.Slice(Across(_column));
    }

    /// <summary>An offset of <paramref name="distance"/> across the driver's axis.</summary>
    private Offset Across(int distance)
      => _driver == Orientation.Vertical ? new Offset(distance, 0) : new Offset(0, distance);

    /// <summary>The same region, <paramref name="width"/> wide across the driver's axis.</summary>
    private Plane<TSpace> Narrow(Plane<TSpace> region, int width)
      => _driver == Orientation.Vertical
        ? region.Slice(new Area(width, region.Area.Height))
        : region.Slice(new Area(region.Width, width));

    /// <summary>The span from <paramref name="column"/> across the driver's axis, <paramref name="width"/> wide or to its edge.</summary>
    private Plane<TSpace> Cut(Plane<TSpace> span, int column, int? width)
      => _driver == Orientation.Vertical
        ? span.Slice(new Offset(column, 0), new Area(width ?? span.Width - column, 1))
        : span.Slice(new Offset(0, column), new Area(1, width ?? span.Area.Height - column));
  }
}
