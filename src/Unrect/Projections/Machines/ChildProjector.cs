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
  internal sealed class ChildProjector<TSpace, T> : IProjector<TSpace, T>, IChildHandle<TSpace>
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
    private readonly ProjectionContext _parent;
    private readonly ProjectionContext _child;
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

    internal ChildProjector(SessionScope<TSpace> scope, ProjectionContext parent, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, bool strict)
    {
      _scope = scope;
      _parent = parent;
      _definition = definition;
      _anchor = anchor;
      _strict = strict;
      _driver = scope.Driver;
      _child = ProjectionContext.Skipped(definition) ? parent.Blaming(definition) : parent.Descend(definition);
      _held = !definition.Axis.Streams(_driver)
        || !PlacementRules.TryStream(definition.Placement, _driver, out _offsetRule, out _sizeRule, out _derived);
      Reach = _held ? Reach.Extent : definition.Reach;
    }

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
          var step = _offsetRule!.Next(region, _offered.Count - 1, out var column);

          if (step == OffsetStep.Skip)
          {
            _skipped++;
            return true;
          }

          if (step == OffsetStep.StartNext)
            _skipped++;

          _column = column;
          Offset = new Offset(_column, _skipped);

          if (_column > Spans.Across(span.Declared.Size, _driver))
          {
            if (_strict)
              throw _parent.Failure(_definition, $"an offset of {ProjectionEngine.Describe(Offset.Size)} does not fit the available space", Spans.Region(_offered[0], _offered.Count, _driver), Offset.Size, null);

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
        _inner = _definition.Start(_scope.At(_child, Spans.Empty(Cut(span, _column, null), _driver)));
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

      if (!_sizeRule!.Take(region, _taken))
      {
        Refuse();

        if (_width is null)
          TrySettleWidth(InnerRegion(_taken), rowsSettled: true);

        return false;
      }

      _taken++;
      _pending.Add(available);

      if (_width is null)
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
        .Slice(new Offset(_column, 0))
        .Erased();

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
          throw ProjectionEngine.AreaFailure(_child, _definition, region, exception);

        PlacementFailed = true;
        _phase = Phase.Finished;
        return;
      }
      catch (Exception exception)
      {
        throw ProjectionEngine.AreaFailure(_child, _definition, region, exception);
      }

      if (width is not int settled)
        return;

      if (settled > region.Width)
      {
        var size = _sizeRule.Declared.Height > 0 ? _sizeRule.Declared : new Size(settled, _taken);

        if (_strict)
          throw _child.Failure(_definition, $"an extent of {ProjectionEngine.Describe(size)} does not fit here", region, size, null);

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
        ProjectionEngine.IsFault(exception));

    private Plane<TSpace> InnerPlane(int rows)
    {
      if (_innerStart >= _offered.Count)
        return _anchor;

      var region = rows == 0 ? Spans.Empty(_offered[_innerStart], _driver) : Spans.Region(_offered[_innerStart], Math.Min(rows, _offered.Count - _innerStart), _driver);

      return region.Slice(new Offset(_column, 0));
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
            throw _child.Failure(_definition, $"an extent of {ProjectionEngine.Describe(_sizeRule.Declared)} does not fit here", region, _sizeRule.Declared, null);

          PlacementFailed = true;
          SettleNothing();
          return;
        }
      }

      var settlement = InnerClose();
      var declared = !_derived;
      var consumed = declared ? Spans.ToSize(_taken, _width!.Value, _driver) : settlement.Consumed;

      Settle(settlement.Value, consumed, ProjectionEngine.Settled(settlement.Presence, declared, consumed));
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

      var machine = _definition.Start(_scope.At(scope, Spans.Empty(inner, _driver)));
      Settlement<T> settlement;

      try
      {
        settlement = _scope.Drive(machine, inner, _definition.Axis.Along(_driver));
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

      Settle(settlement.Value, consumed, ProjectionEngine.Settled(settlement.Presence, declared, consumed));
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

    private Plane<TSpace> Cut(Plane<TSpace> span, int column, int? width)
      => _driver == Orientation.Vertical
        ? span.Slice(new Offset(column, 0), new Area(width ?? span.Width - column, 1))
        : span.Slice(new Offset(0, column), new Area(1, width ?? span.Declared.Height - column));

    private ProjectionException Fault(string violation)
    {
      var region = _offered.Count == 0 ? _anchor : Spans.Region(_offered[0], _offered.Count, _driver);

      return _child.Failure(_definition, $"{ProjectionContext.Describe(_definition)} {violation}; a machine that broke the protocol is a bug in the node, not a shape of data", region, null, null, isFault: true);
    }
  }
}
