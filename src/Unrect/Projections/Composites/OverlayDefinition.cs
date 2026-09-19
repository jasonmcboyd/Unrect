using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// An overlay: one extent shared by every child, each finding its own place inside it, with no
  /// cursor between them. Where a flow divides the space into bands, this hands the whole of it to
  /// everyone, so children may overlap and may read the same cells.
  /// </summary>
  internal sealed class OverlayDefinition<TSpace, T> : LayoutDefinition<TSpace, T>
    where TSpace : class, ISpace
  {
    public OverlayDefinition(Layout<TSpace, T> layout, Placement placement)
      : base(layout, placement)
    {
    }

    public override string Description => "Overlay";

    /// <summary>An overlay streams along whatever axis every child streams along: its children run concurrently on the same spans.</summary>
    public override Axes Axis
    {
      get
      {
        var axis = Axes.Either;

        foreach (var child in Children)
          axis &= child.Definition.Axis;

        return axis.OrEither();
      }
    }

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Every span to every open child, each behind its own placement machine from the overlay's
    /// origin; a child that refuses is closed into its slot; refuse when all have closed. Consumed
    /// is the box that encloses wherever the children reached.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, T>
    {
      private readonly OverlayDefinition<TSpace, T> _overlay;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly object?[] _values;
      private readonly IChildHandle<TSpace>?[] _open;
      private Plane<TSpace>? _first;
      private bool _started;
      private int _width;
      private int _height;

      public Machine(OverlayDefinition<TSpace, T> overlay, ProjectorScope<TSpace> scope)
      {
        _overlay = overlay;
        _scope = scope;
        _values = new object?[overlay.Layout.Children.Count];
        _open = new IChildHandle<TSpace>?[_values.Length];
      }

      public bool Next(Plane<TSpace> span)
      {
        _first ??= span;
        StartAll(Spans.Empty(span, _scope.Driver));

        var taken = false;

        for (var index = 0; index < _open.Length; index++)
        {
          if (_open[index] is not IChildHandle<TSpace> child)
            continue;

          if (child.Next(span))
            taken = true;
          else
            CloseChild(index);
        }

        return taken;
      }

      public Settlement<T> Close()
      {
        StartAll(_scope.Anchor);

        for (var index = 0; index < _open.Length; index++)
          if (_open[index] is not null)
            CloseChild(index);

        T value;

        try
        {
          value = _overlay.Layout.Combine(_values);
        }
        catch (CellReadException failure)
        {
          throw _scope.Reading(failure, Extent());
        }
        catch (LayoutShapeException shape)
        {
          throw _scope.Failure(_overlay, shape.Message, Extent(), null, shape, isFault: true);
        }

        return new Settlement<T>(value, new Size(_width, _height));
      }

      private void StartAll(Plane<TSpace> anchor)
      {
        if (_started)
          return;

        _started = true;

        for (var index = 0; index < _open.Length; index++)
          _open[index] = _overlay.Layout.Runners[index].Start(_scope, _overlay.Layout.Children[index], anchor);
      }

      private void CloseChild(int index)
      {
        var child = _open[index]!;
        _open[index] = null;
        _values[index] = child.CloseBoxed();
        _width = Math.Max(_width, child.Advance.Width);
        _height = Math.Max(_height, child.Advance.Height);
      }

      private Plane<TSpace> Extent()
        => _first is Plane<TSpace> first
          ? new Plane<TSpace>(first.Space, first.Origin, new Area(Math.Max(_width, first.Width), _height))
          : _scope.Anchor;
    }
  }
}
