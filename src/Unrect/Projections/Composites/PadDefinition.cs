using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Insets its extent and applies the inner projection to what is left, then reports the whole
  /// thing as consumed. Padding shrinks the inside; an offset shifts the outside — which is why
  /// this is a wrapper projection rather than a placement, and why the two compose without
  /// interfering.
  /// </summary>
  internal sealed class PadDefinition<TSpace, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    public PadDefinition(IProjectionDefinition<TSpace, TResult> inner, int left, int top, int right, int bottom, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Left = left;
      Top = top;
      Right = right;
      Bottom = bottom;
      Children = new[] { new Child(inner, default) };
    }

    private IProjectionDefinition<TSpace, TResult> Inner { get; }
    private int Left { get; }
    private int Top { get; }
    private int Right { get; }
    private int Bottom { get; }

    public override string Description => "Padded";

    public override IReadOnlyList<Child> Children { get; }

    public override bool IsWrapper => true;

    public override Axes Axis => Inner.Axis.OrEither();

    /// <summary>A pad withholds its bottom padding until it can tell it from the inner's rows, so it may hand one span back.</summary>
    public override Reach Reach => base.Reach.Join(Reach.Spans(Bottom + 1));

    public override IProjector<TSpace, TResult> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Skip <c>Top</c> spans; inset every span by <c>Left</c> and <c>Right</c>; forward to the inner,
    /// keeping the last <c>Bottom</c> spans back so they can be the bottom padding when the inner
    /// refuses or the feed ends. Written for a vertical driver; a horizontal pad is held and
    /// re-driven as its inner is.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, TResult>
    {
      private readonly PadDefinition<TSpace, TResult> _pad;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly Queue<Plane<TSpace>> _withheld = new Queue<Plane<TSpace>>();
      private Plane<TSpace>? _first;
      private int _offered;
      private int _skipped;
      private ChildProjector<TSpace, TResult>? _inner;
      private bool _innerRefused;
      private int _padded;
      private bool _finished;
      private bool _closed;

      public Machine(PadDefinition<TSpace, TResult> pad, ProjectorScope<TSpace> scope)
      {
        _pad = pad;
        _scope = scope;
      }

      private int Top => _scope.Driver == Orientation.Vertical ? _pad.Top : _pad.Left;
      private int Bottom => _scope.Driver == Orientation.Vertical ? _pad.Bottom : _pad.Right;
      private int Leading => _scope.Driver == Orientation.Vertical ? _pad.Left : _pad.Top;
      private int Trailing => _scope.Driver == Orientation.Vertical ? _pad.Right : _pad.Bottom;

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Context.Failure(_pad, $"{PathRenderer.Describe(_pad)} was fed a span after it was closed", span, null, null, isFault: true);

        if (_finished)
          return false;

        _first ??= span;
        _offered++;

        if (_skipped < Top)
        {
          _skipped++;
          return true;
        }

        if (_innerRefused)
        {
          // The bottom padding: the spans after the inner's, as many as were declared.
          if (_padded < Bottom)
          {
            _padded++;
            return true;
          }

          _finished = true;
          return false;
        }

        _withheld.Enqueue(span);

        while (_withheld.Count > Bottom)
        {
          var next = _withheld.Dequeue();

          if (!Feed(next))
          {
            // The inner refused `next`, which is the first span of the bottom padding; the ones
            // withheld behind it follow, and whatever is left is not the pad's.
            _innerRefused = true;
            _padded = 1;

            while (_padded < Bottom && _withheld.Count > 0)
            {
              _withheld.Dequeue();
              _padded++;
            }

            if (_padded == Bottom)
              _finished = true;

            return true;
          }
        }

        return true;
      }

      public Settlement<TResult> Close()
      {
        _closed = true;

        var extent = _first is Plane<TSpace> first ? Spans.Region(first, _offered, _scope.Driver) : _scope.Anchor;
        var across = Spans.Across(extent.Declared.Size, _scope.Driver);

        // The withheld spans are the bottom padding; fewer than declared means the padding does not fit.
        if (!_innerRefused && (_withheld.Count < Bottom || across - Leading - Trailing < 0) || _skipped < Top)
          throw DoesNotFit(extent);

        var inner = _inner ?? StartInner(extent);
        var settlement = inner.Close();
        var advance = new Size(inner.Advance.Width + _pad.Left + _pad.Right, inner.Advance.Height + _pad.Top + _pad.Bottom);

        return new Settlement<TResult>(settlement.Value, advance, inner.Presence);
      }

      private bool Feed(Plane<TSpace> span)
      {
        _inner ??= StartInner(span);

        var across = Spans.Across(span.Declared.Size, _scope.Driver);

        if (across - Leading - Trailing < 0)
          throw DoesNotFit(Spans.Region(_first!.Value, _offered, _scope.Driver));

        var inset = _scope.Driver == Orientation.Vertical
          ? span.Slice(new Offset(Leading, 0), new Area(across - Leading - Trailing, 1))
          : span.Slice(new Offset(0, Leading), new Area(1, across - Leading - Trailing));

        return _inner.Next(inset);
      }

      private ChildProjector<TSpace, TResult> StartInner(Plane<TSpace> at)
        => _scope.Start(_pad.Children[0], _pad.Inner, Spans.Empty(at, _scope.Driver), inheritSite: true);

      private ProjectionException DoesNotFit(Plane<TSpace> extent)
      {
        var size = extent.Declared.Size;

        return _scope.Context.Failure(
          _pad,
          $"a padding of {_pad.Left} left, {_pad.Top} top, {_pad.Right} right, {_pad.Bottom} bottom does not fit an extent of {size.Width}x{size.Height}",
          extent,
          null,
          null);
      }
    }

    internal override Reach Retains => Reach.Spans(Bottom + 1);

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;
      var width = size.Width - Left - Right;
      var height = size.Height - Top - Bottom;

      // Blames itself explicitly: an unnamed pad is transparent, so the context belongs to its
      // parent and would otherwise take the blame for the padding.
      if (width < 0 || height < 0)
        throw context.Failure(
          this,
          $"a padding of {Left} left, {Top} top, {Right} right, {Bottom} bottom does not fit an extent of {size.Width}x{size.Height}",
          extent,
          null,
          null);

      // The context is handed on unchanged: a location comes from the plane, which carries the
      // sheet's own coordinates, so the inset region already knows where it is.
      var applied = ProjectionEngine.Apply(
        Inner,
        extent.Slice(new Offset(Left, Top), new Area(width, height)),
        context);

      return new ProjectionResult<TResult>(
        applied.Value,
        applied.Advance + new Size(Left + Right, Top + Bottom),
        applied.Presence);
    }
  }
}
