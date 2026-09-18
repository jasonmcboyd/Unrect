using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The handle the engine wraps around every child it starts: keeps the spans the child was
  /// offered, resolves the child's placement over them span by span
  /// (<see cref="StreamingPlacement{TSpace}"/>), feeds the child's own machine the spans of its
  /// inner region, and settles on what the child kept.
  /// <para>
  /// There is one placement and one way to run it; what differs is when the machine is fed. A
  /// <em>driven</em> child is fed as the spans arrive. A <em>held</em> child — one whose axis is not
  /// the driver's, or whose placement settles only at the end, or that bounds itself — takes every
  /// span offered and at <c>Close</c> replays them through the same placement, then drives its
  /// machine along its own axis over the region that came out. Either way the close settles
  /// whatever the placement left open: an offset that never started, a width or a length that
  /// waited for the end.
  /// </para>
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
    private readonly bool _held;
    private readonly StreamingPlacement<TSpace> _placement;
    private readonly List<Plane<TSpace>> _offered = new List<Plane<TSpace>>();
    private readonly List<Plane<TSpace>> _pending = new List<Plane<TSpace>>();
    private readonly List<IChildHandle<TSpace>> _children = new List<IChildHandle<TSpace>>();

    private Phase _phase;
    private int _column;
    private bool _startNext;
    private int _innerStart;
    private int _taken;
    private int _fed;
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
      _held = !PlacementRules.Streams(definition, _driver, out var offset, out var size, out var derived, out _);
      _placement = new StreamingPlacement<TSpace>(offset, size, derived, definition, _driver, strict);
    }

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

      if (_held || _phase == Phase.Offset)
        return Row(_offered[0]);

      var innerStart = Row(_offered[_innerStart]);

      if (!_placement.Derived && _placement.Width is null)
        return innerStart;

      if (_inner is IHolding holding)
      {
        if (holding.HeldFrom is not int position)
          return null;

        return Row(_offered[Math.Min(_innerStart + position, _offered.Count - 1)]);
      }

      var own = PlacementRules.Retains(_definition);

      if (own.IsNone)
        return null;

      if (own.IsExtent)
        return innerStart;

      return Math.Max(innerStart, current - own.Count!.Value + 1);
    }

    private int Row(Plane<TSpace> span) => _driver == Orientation.Vertical ? span.Origin.Height : span.Origin.Width;

    // --- What the parent reads off the handle --------------------------------------------------

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

      if (_held)
        return true;

      try
      {
        return Take(span, _offered.Count - 1, live: true);
      }
      catch
      {
        // A span the machine threw on was not taken.
        if (_offered.Count > 0 && ReferenceEquals(_offered[_offered.Count - 1].Space, span.Space) && _offered[_offered.Count - 1].Equals(span))
          _offered.RemoveAt(_offered.Count - 1);

        throw;
      }
    }

    /// <summary>
    /// One span through the placement: the offset phase until it starts, then the size phase, which
    /// feeds the inner what it takes. <paramref name="live"/> says the span was just offered — a
    /// refusal then hands it straight back — where a replayed span refused is simply not kept.
    /// </summary>
    private bool Take(Plane<TSpace> span, int index, bool live)
    {
      if (_phase == Phase.Offset)
      {
        if (_startNext)
        {
          _startNext = false;
        }
        else
        {
          var region = Spans.Region(_offered[0], index + 1, _driver).Erased();
          var step = _placement.Advance(region, index, Spans.Across(span.Area.Size, _driver), _parent, out var refused);

          if (refused)
          {
            PlacementFailed = true;
            return Refuse(live);
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

        Start(index, Cut(span, _column, null));
      }

      return TakeInner(span, live);
    }

    /// <summary>The offset is known: the inner starts at <paramref name="index"/>, over <paramref name="at"/>'s corner.</summary>
    private void Start(int index, Plane<TSpace> at)
    {
      _innerStart = index;
      _phase = Phase.Size;

      // A held child is driven along its own axis at close; its machine is built then.
      if (!_held)
        _inner = _definition.Build(_child.Within(this, Spans.Empty(at, _driver), _driver));
    }

    private bool TakeInner(Plane<TSpace> span, bool live)
    {
      var available = Cut(span, _column, null);

      if (_placement.Derived)
      {
        if (_held)
        {
          _taken++;
          return true;
        }

        if (!InnerNext(available))
        {
          _innerRefused = true;
          return Refuse(live);
        }

        _taken++;
        _fed++;
        return true;
      }

      var take = _placement.Take(InnerRegion(_taken + 1), _taken, _child);

      if (_placement.Failed)
      {
        PlacementFailed = true;
        return Refuse(live);
      }

      if (!take)
      {
        // The region the rule settled on, read before the refused span leaves the list: with
        // nothing taken it is the empty region at that span, which still has a width to settle.
        var settled = _placement.Width is null ? InnerRegion(_taken) : default;

        Refuse(live);

        if (_placement.Width is null)
          SettleWidth(settled, rowsSettled: true);

        return false;
      }

      _taken++;

      if (_placement.Width is int width && !_held)
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
      if (!_placement.TrySettleWidth(region, _taken, rowsSettled, _child))
      {
        if (_placement.Failed)
        {
          PlacementFailed = true;
          _phase = Phase.Finished;
        }

        return;
      }

      if (!_held)
        FeedPending(_pending.Count);
    }

    /// <summary>Feeds the inner the first <paramref name="count"/> spans that waited for the width, cut to it.</summary>
    private void FeedPending(int count)
    {
      var width = _placement.Width!.Value;

      for (var index = 0; index < count; index++)
      {
        FeedInner(Cut(_pending[index], 0, width));
        _fed++;
      }

      _pending.RemoveRange(0, count);
    }

    /// <summary>
    /// The span in hand was not taken. A live one is not among those offered — the parent gets it
    /// straight back — and nothing after it is; a replayed one stays, unkept, for the shortfall.
    /// </summary>
    private bool Refuse(bool live)
    {
      if (live)
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
        Settle();
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

    /// <summary>
    /// The close: a held child replays its spans through the placement first; then whatever the
    /// placement left open is settled — an offset that never started over the whole region, a
    /// width or a length that waited for the end — and the machine is fed what was kept, or built
    /// and driven over it along its own axis.
    /// </summary>
    private void Settle()
    {
      if (_held)
        for (var index = 0; index < _offered.Count && _phase != Phase.Finished; index++)
          Take(_offered[index], index, live: false);

      if (_phase == Phase.Offset && !PlacementFailed)
      {
        var region = _offered.Count == 0 ? Spans.Empty(_anchor, _driver) : Spans.Region(_offered[0], _offered.Count, _driver);

        if (!_placement.SettleOffset(region.Erased(), _parent))
        {
          PlacementFailed = true;
        }
        else
        {
          Offset = _placement.Offset;
          _column = Spans.Across(Offset.Size, _driver);

          var start = Spans.Along(Offset.Size, _driver);
          Start(start, Spans.EmptyAt(_offered.Count == 0 ? _anchor : _offered[0], start, _driver));

          for (var index = start; index < _offered.Count && _phase != Phase.Finished; index++)
            Take(_offered[index], index, live: false);
        }
      }

      if (PlacementFailed)
      {
        SettleNothing();
        return;
      }

      var kept = _taken;

      if (!_placement.Derived)
      {
        if (_placement.Width is null)
          SettleWidth(InnerRegion(_taken), rowsSettled: true);

        if (!PlacementFailed && !_placement.Complete(_taken))
        {
          var region = InnerRegion(_taken);

          if (_strict)
            throw _child.Failure(_definition, $"an extent of {EngineRules.Describe(_placement.Declared)} does not fit here", region, _placement.Declared, null);

          PlacementFailed = true;
        }

        if (!PlacementFailed && _placement.Along(InnerRegion(_taken), _taken, _child) is int along)
          kept = along;
        else
          PlacementFailed = true;

        if (PlacementFailed)
        {
          SettleNothing();
          return;
        }

        if (kept < _fed)
          throw Fault($"kept {kept} spans after its machine was fed {_fed}");
      }

      if (_held)
        DriveHeld(kept);
      else
        CloseDriven(kept);
    }

    /// <summary>A driven child: the spans still waiting are fed as far as the placement kept, and the machine closed.</summary>
    private void CloseDriven(int kept)
    {
      if (!_placement.Derived)
      {
        // An inner that was fed nothing closes over the empty region its rule settled on, cut to
        // the settled width: a discovered block over blank space is 0x0, not 0 rows of the anchor's width.
        if (kept == 0 && _fed == 0 && _inner is not null)
          _inner = _definition.Build(_child.Within(this, Narrow(InnerPlane(0), _placement.Width!.Value), _driver));

        FeedPending(Math.Min(kept - _fed, _pending.Count));
      }

      var settlement = InnerClose();
      var declared = !_placement.Derived;
      var consumed = declared ? Spans.ToSize(kept, _placement.Width!.Value, _driver) : settlement.Consumed;

      Settle(settlement.Value, consumed, EngineRules.Settled(settlement.Presence, declared, consumed));
    }

    /// <summary>A held child: its machine built over the region the placement settled on, and driven along its own axis.</summary>
    private void DriveHeld(int kept)
    {
      var declared = !_placement.Derived;
      var inner = declared ? Narrow(InnerPlane(kept), _placement.Width!.Value) : InnerPlane(kept);
      var along = _definition.Axis.Along(_driver);
      var machine = _definition.Build(_child.Within(this, Spans.Empty(inner, along ?? _driver), along ?? _driver));
      Settlement<T> settlement;

      try
      {
        settlement = _scope.Drive(machine, inner, along);
      }
      catch (Exception exception) when (exception is not ProjectionException)
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
      catch (Exception exception) when (exception is not ProjectionException)
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
      catch (Exception exception) when (exception is not ProjectionException)
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
      => (rows == 0 ? Spans.Empty(InnerOrigin(), _driver) : Spans.Region(_offered[_innerStart], rows, _driver))
        .Slice(Across(_column))
        .Erased();

    /// <summary>The same region as a plane over the space, clamped to what was offered, for a message or a machine.</summary>
    private Plane<TSpace> InnerPlane(int rows)
    {
      if (_innerStart >= _offered.Count)
        return Spans.Empty(InnerOrigin(), _driver).Slice(Across(Math.Min(_column, Spans.Across(InnerOrigin().Area.Size, _driver))));

      var region = rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], Math.Min(rows, _offered.Count - _innerStart), _driver);

      return region.Slice(Across(_column));
    }

    /// <summary>The span the inner starts on, or the empty span past the last offered when the offset skipped everything.</summary>
    private Plane<TSpace> InnerOrigin()
      => _innerStart < _offered.Count
        ? _offered[_innerStart]
        : _offered.Count == 0 ? _anchor : Spans.EmptyAt(_offered[0], _offered.Count, _driver);

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
