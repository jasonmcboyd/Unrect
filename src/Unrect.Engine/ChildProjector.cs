using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The placement machine the engine wraps around every child it starts: resolves the child's
  /// offset and area span by span where the calculus allows, feeds the child's own machine the
  /// spans of its inner region, and settles on what the child kept. A child whose axis is not the
  /// driver's, or whose placement cannot be driven per span, is <em>held</em>: it takes every span
  /// offered, and at <c>Close</c> is placed eagerly over the region they cover and re-driven along
  /// its own axis — the pull engine's own semantics, only later.
  /// <para>
  /// This is also where the protocol is validated: a machine that takes a span after refusing one,
  /// or settles on more than it was offered, is a fault blamed on the node.
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
    private readonly bool _held;
    private readonly OffsetRule? _offsetRule;
    private readonly SizeRule? _sizeRule;
    private readonly bool _derived;
    private readonly List<Plane<TSpace>> _pending = new List<Plane<TSpace>>();

    private Phase _phase;
    private int _skipped;
    private int _column;
    private bool _startNext;
    private int _innerStart;
    private int _taken;
    private int? _width;
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
      _held = !PlacementRules.Streams(definition, _driver, out _offsetRule, out _sizeRule, out _derived, out _);
      Reach = _held ? Reach.Extent : definition.Reach;
    }

    private readonly List<IChildHandle<TSpace>> _children = new List<IChildHandle<TSpace>>();

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

      if (!_derived && _width is null)
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

    /// <summary>What this child announces to the parent that started it.</summary>
    public Reach Reach { get; }

    public Offset Offset { get; private set; }

    public Size Advance { get; private set; }

    public Size Consumed { get; private set; }

    public Presence Presence { get; private set; }

    public bool PlacementFailed { get; private set; }

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
      if (_held)
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
          OffsetStep step;
          int column;

          try
          {
            step = _offsetRule!.Next(region, _offered.Count - 1, out column);
          }
          catch (ProjectionException)
          {
            throw;
          }
          catch (OutOfBoundsException exception)
          {
            if (_strict)
              throw _parent.Failure(_definition, EngineRules.Missing(exception), region, null, exception);

            PlacementFailed = true;
            return Refuse();
          }
          catch (Exception exception)
          {
            throw _parent.Failure(_definition, EngineRules.Threw("offset", exception), region, null, exception, EngineRules.IsFault(exception));
          }

          if (step == OffsetStep.Skip)
          {
            _skipped++;
            return true;
          }

          if (step == OffsetStep.StartNext)
            _skipped++;

          _column = column;
          Offset = _driver == Orientation.Vertical ? new Offset(_column, _skipped) : new Offset(_skipped, _column);

          if (_column > Spans.Across(span.Declared.Size, _driver))
          {
            if (_strict)
              throw _parent.Failure(_definition, $"an offset of {EngineRules.Describe(Offset.Size)} does not fit the available space", Spans.Region(_offered[0], _offered.Count, _driver), Offset.Size, null);

            PlacementFailed = true;
            return Refuse();
          }

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

    public Settlement<T> Close()
    {
      if (_phase == Phase.Closed)
        throw Fault("was closed twice");

      try
      {
        if (_held || _phase == Phase.Offset)
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

    public IEnumerable<Plane<TSpace>> Shortfall()
    {
      var kept = PlacementFailed ? 0 : Spans.Along(Advance, _driver);

      for (var index = kept; index < _offered.Count; index++)
        yield return _offered[index];
    }

    private bool TakeInner(Plane<TSpace> span)
    {
      var available = Cut(span, _column, null);

      if (_derived)
      {
        if (!InnerNext(available))
        {
          _innerRefused = true;
          return Refuse();
        }

        _taken++;
        return true;
      }

      var region = InnerRegion(_taken + 1);
      bool take;

      try
      {
        take = _sizeRule!.Take(region, _taken);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (_strict)
          throw EngineRules.AreaFailure(_child, _definition, region, exception);

        PlacementFailed = true;
        return Refuse();
      }
      catch (Exception exception)
      {
        throw EngineRules.AreaFailure(_child, _definition, region, exception);
      }

      if (!take)
      {
        // The region the rule settled on, read before the refused span leaves the list: with
        // nothing taken it is the empty region at that span, which still has a width to settle.
        var settled = _width is null ? InnerRegion(_taken) : default;

        Refuse();

        if (_width is null)
          TrySettleWidth(settled, rowsSettled: true);

        return false;
      }

      _taken++;

      if (_width is int width)
      {
        FeedInner(Cut(available, 0, width));
        return true;
      }

      _pending.Add(available);
      TrySettleWidth(InnerRegion(_taken), rowsSettled: false);
      return true;
    }

    /// <summary>The span in hand was not taken: it is not among those offered, and nothing after it is.</summary>
    private bool Refuse()
    {
      _offered.RemoveAt(_offered.Count - 1);
      _phase = Phase.Finished;
      return false;
    }

    private Plane<ISpace> InnerRegion(int rows)
      => (rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], rows, _driver))
        .Slice(Across(_column))
        .Erased();

    /// <summary>An offset of <paramref name="distance"/> across the driver's axis.</summary>
    private Offset Across(int distance)
      => _driver == Orientation.Vertical ? new Offset(distance, 0) : new Offset(0, distance);

    private void TrySettleWidth(Plane<ISpace> region, bool rowsSettled)
    {
      int? width;

      try
      {
        width = _sizeRule!.Width(region, _taken, rowsSettled);
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException exception)
      {
        if (_strict)
          throw EngineRules.AreaFailure(_child, _definition, region, exception);

        PlacementFailed = true;
        _phase = Phase.Finished;
        return;
      }
      catch (Exception exception)
      {
        throw EngineRules.AreaFailure(_child, _definition, region, exception);
      }

      if (width is not int settled)
        return;

      if (settled > Spans.Across(region.Declared.Size, _driver))
      {
        // Too wide is reported over the rows that were there for it, so a declared 3x3 on a 2x2
        // space says "2x2 available" rather than the one row it had seen when the width came in.
        if (!rowsSettled)
          return;

        var size = _sizeRule.Declared.Height > 0 ? _sizeRule.Declared : new Size(settled, _taken);

        if (_strict)
          throw _child.Failure(_definition, $"an extent of {EngineRules.Describe(size)} does not fit here", region, size, null);

        PlacementFailed = true;
        _phase = Phase.Finished;
        return;
      }

      _width = settled;

      foreach (var pending in _pending)
        FeedInner(Cut(pending, 0, settled));

      _pending.Clear();
    }

    private void FeedInner(Plane<TSpace> span)
    {
      if (_innerRefused)
        return;

      if (!InnerNext(span))
        _innerRefused = true;
    }

    /// <summary>
    /// The inner machine's <c>Next</c>, with a foreign exception classified and wrapped as the
    /// engine wraps one thrown from <c>Project</c>: a failure blaming this node, a fault when the
    /// exception says the code broke rather than the data disagreed.
    /// </summary>
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

    private Plane<TSpace> InnerPlane(int rows)
    {
      if (_innerStart >= _offered.Count)
        return _anchor;

      var region = rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], Math.Min(rows, _offered.Count - _innerStart), _driver);

      return region.Slice(Across(_column));
    }

    private void SettleStreaming()
    {
      if (!_derived)
      {
        if (_width is null && !PlacementFailed)
          TrySettleWidth(InnerRegion(_taken), rowsSettled: true);

        if (PlacementFailed)
        {
          SettleNothing();
          return;
        }

        if (!_sizeRule!.Complete(_taken))
        {
          var region = InnerRegion(_taken);

          if (_strict)
            throw _child.Failure(_definition, $"an extent of {EngineRules.Describe(_sizeRule.Declared)} does not fit here", region, _sizeRule.Declared, null);

          PlacementFailed = true;
          SettleNothing();
          return;
        }
      }

      // An inner that was fed nothing closes over the empty region its rule settled on, cut to the
      // settled width: a discovered block over blank space is 0x0, not 0 rows of the anchor's width.
      if (!_derived && _taken == 0 && _inner is not null)
        _inner = _definition.Build(_child.Within(this, Narrow(InnerPlane(0), _width!.Value), _driver));

      var settlement = InnerClose();
      var declared = !_derived;
      var consumed = declared ? Spans.ToSize(_taken, _width!.Value, _driver) : settlement.Consumed;

      Settle(settlement.Value, consumed, EngineRules.Settled(settlement.Presence, declared, consumed));
    }

    private void ResolveEagerly()
    {
      var region = _offered.Count == 0 ? Spans.Empty(_anchor, _driver) : Spans.Region(_offered[0], _offered.Count, _driver);

      if (!EagerPlacement.TryPlace(_definition, region, _parent, _strict, out var offset, out var inner, out var scope, out var declared))
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

    /// <summary>The same region, <paramref name="width"/> wide across the driver's axis.</summary>
    private Plane<TSpace> Narrow(Plane<TSpace> region, int width)
      => _driver == Orientation.Vertical
        ? region.Slice(new Area(width, region.Area.Height))
        : region.Slice(new Area(region.Width, width));

    private Plane<TSpace> Cut(Plane<TSpace> span, int column, int? width)
      => _driver == Orientation.Vertical
        ? span.Slice(new Offset(column, 0), new Area(width ?? span.Width - column, 1))
        : span.Slice(new Offset(0, column), new Area(1, width ?? span.Declared.Height - column));

    private ProjectionException Fault(string violation)
    {
      var region = _offered.Count == 0 ? _anchor : Spans.Region(_offered[0], _offered.Count, _driver);

      return _child.Failure(_definition, $"{PathRenderer.Describe(_definition)} {violation}; a machine that broke the protocol is a bug in the node, not a shape of data", region, null, null, isFault: true);
    }
  }
}
