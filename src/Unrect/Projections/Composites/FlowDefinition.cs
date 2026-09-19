using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A flow: children laid out one after another along an axis, each starting where the one before
  /// it left off, so the space is divided into bands nobody shares.
  /// </summary>
  internal sealed class FlowDefinition<TSpace, T> : LayoutDefinition<TSpace, T>
    where TSpace : class, ISpace
  {
    public FlowDefinition(Orientation orientation, Layout<TSpace, T> layout, Placement placement, string? description = null)
      : base(layout, placement)
    {
      Orientation = orientation;
      Declared = description;
    }

    private Orientation Orientation { get; }

    /// <summary>
    /// What a factory that desugars into a flow calls itself. A <c>Heading</c> stage is one: a
    /// segment reading <c>VerticalFlow</c> could not be grepped back to the <c>Heading(…)</c> that
    /// produced it.
    /// </summary>
    private string? Declared { get; }

    // A path segment names the factory the user typed, so it can be grepped back to the line.
    public override string Description
      => Declared ?? (Orientation == Orientation.Vertical ? "VerticalFlow" : "HorizontalFlow");

    public override Axes Axis => Orientation.Of();

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Children in declaration order, one open at a time: offer the span to the open child; when it
    /// refuses, close it into its slot, start the next, replay any shortfall into it, and re-offer.
    /// Refuse when the last child has. The sibling note is raised here, because the flow is what
    /// knows both facts.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private const string SiblingNote = "the preceding sibling consumed nothing at this position";

      private readonly FlowDefinition<TSpace, T> _flow;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly Orientation _along;
      private readonly object?[] _values;
      private Plane<TSpace>? _first;
      private int _index;
      private IChildHandle<TSpace>? _current;
      private int _length;
      private int _across;
      private int _previous;
      private bool _finished;

      public Machine(FlowDefinition<TSpace, T> flow, ProjectorScope<TSpace> scope)
      {
        _flow = flow;
        _scope = scope;
        _along = flow.Orientation;
        _values = new object?[flow.Layout.Children.Count];
      }

      public bool Next(Plane<TSpace> span)
      {
        _first ??= span;

        return Offer(span);
      }

      public Settlement<T> Close()
      {
        // The open child is closed; what it hands back may open the next, which is closed in turn;
        // every child never reached is closed on nothing and answers for itself.
        while (_current is not null || _index < _values.Length)
        {
          _current ??= StartChild(_index);
          CloseCurrent();
        }

        T value;

        try
        {
          value = _flow.Layout.Combine(_values);
        }
        catch (CellReadException failure)
        {
          throw _scope.Reading(failure, Extent());
        }
        catch (LayoutShapeException shape)
        {
          throw _scope.Failure(_flow, shape.Message, Extent(), null, shape, isFault: true);
        }

        return new Settlement<T>(value, Spans.ToSize(_length, _across, _along));
      }

      private bool Offer(Plane<TSpace> span)
      {
        while (!_finished)
        {
          _current ??= StartChild(_index);

          if (_current.Next(span))
            return true;

          CloseCurrent();
        }

        return false;
      }

      private IChildHandle<TSpace> StartChild(int index)
      {
        if (index == _values.Length)
        {
          _finished = true;
          throw new InvalidOperationException("A flow started a child past its last.");
        }

        var anchor = _first is Plane<TSpace> first
          ? Spans.EmptyAt(first, _length, _along)
          : _scope.Anchor;

        return _flow.Layout.Runners[index].Start(_scope, _flow.Layout.Children[index], anchor);
      }

      private void CloseCurrent()
      {
        var closed = _current!;
        _current = null;

        object? value;

        try
        {
          value = closed.CloseBoxed();
        }
        catch (ProjectionException failure) when (FollowsAnEmptySibling(failure))
        {
          throw failure.WithNote(SiblingNote);
        }

        _values[_index] = value;
        _previous = Spans.Along(closed.Advance, _along);
        _length += _previous;
        _across = Math.Max(_across, Spans.Across(closed.Advance, _along));
        _index++;

        if (_index == _values.Length)
          _finished = true;

        foreach (var span in closed.Shortfall())
          if (!Offer(span))
            break;
      }

      /// <summary>
      /// A sibling that consumed nothing — an absorbed boundary, most often — leaves this child
      /// reading the very cells that just failed, so it fails the same way for the same reason.
      /// </summary>
      private bool FollowsAnEmptySibling(ProjectionException failure)
        => _index > 0 && _previous == 0 && _first is Plane<TSpace> first && failure.Location.IsAt(first.Origin + Spans.Step(_length, _along));

      private Plane<TSpace> Extent()
        => _first is Plane<TSpace> first
          ? new Plane<TSpace>(first.Space, first.Origin, new Area(Spans.ToSize(_length, Math.Max(_across, first.Width), _along)))
          : _scope.Anchor;
    }
  }
}
