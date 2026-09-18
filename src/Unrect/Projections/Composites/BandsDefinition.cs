using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One declared projection applied to each band of a fixed stride, until a whole band is no longer
  /// left. The extent is cut rather than searched: nothing about a band's content decides where the
  /// next one starts, and a trailing part-band is not a band at all.
  /// <para>
  /// It declares no extent of its own — how far the tiling runs is whoever places it decides — so
  /// the stride and the bound stay two separate declarations.
  /// </para>
  /// </summary>
  internal sealed class BandsDefinition<TSpace, T> : DefinitionNode<TSpace, IReadOnlyList<T>>
    where TSpace : class, ISpace
  {
    public BandsDefinition(
      IProjectionDefinition<TSpace, T> each,
      Orientation orientation,
      int stride,
      UseSite eachSite,
      BlankRowStrategy? onBlank,
      Placement placement)
      : base(placement)
    {
      Each = each ?? throw new ArgumentNullException(nameof(each));
      Orientation = orientation;
      Stride = stride;
      EachSite = eachSite;
      OnBlank = onBlank;
      Children = new[] { new Child(each, eachSite) };
    }

    private IProjectionDefinition<TSpace, T> Each { get; }

    /// <summary>What the declaration called the band projection, for every band to be labelled by.</summary>
    private UseSite EachSite { get; }

    private Orientation Orientation { get; }

    /// <summary>How many rows (or columns) one band is.</summary>
    private int Stride { get; }

    /// <summary>How a fully-blank band is treated, or null to project every band there is.</summary>
    private BlankRowStrategy? OnBlank { get; }

    public override string Description => Orientation == Orientation.Vertical ? "VerticalBands" : "HorizontalBands";

    public override IReadOnlyList<Child> Children { get; }

    public override Axes Axis => Orientation.Of();

    /// <summary>A tiler must see a whole band before it can say the band is blank, and hands an incomplete one back.</summary>
    public override Reach Reach => base.Reach.Join(Reach.Spans(Stride));

    public override IProjector<TSpace, IReadOnlyList<T>> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// Gathers <c>Stride</c> spans into a band; a complete band is either the blank policy's or its
    /// child's, which is started, fed the band and closed. A blank band under <c>Stop</c> ends the
    /// run and is not the tiler's; an incomplete band at the end of the feed is not a band and is
    /// handed back.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, IReadOnlyList<T>>
    {
      private readonly BandsDefinition<TSpace, T> _tiler;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly List<T> _values = new List<T>();
      private readonly List<Plane<TSpace>> _band = new List<Plane<TSpace>>();
      private Plane<TSpace>? _first;
      private int _bands;
      private int _cursor;
      private int _across;
      private bool _finished;

      public Machine(BandsDefinition<TSpace, T> tiler, ProjectorScope<TSpace> scope)
      {
        _tiler = tiler;
        _scope = scope;
      }

      private Orientation Along => _tiler.Orientation;

      public bool Next(Plane<TSpace> span)
      {
        if (_finished)
          return false;

        if (_first is null)
        {
          _first = span;
          _across = Spans.Across(span.Area.Size, Along);
        }

        if (_across == 0)
        {
          _finished = true;
          return false;
        }

        _band.Add(span);

        if (_band.Count < _tiler.Stride)
          return true;

        var band = Spans.Region(_band[0], _tiler.Stride, Along);

        if (_tiler.OnBlank is BlankRowStrategy onBlank && IsBlank(band))
        {
          if (onBlank.IsStop)
          {
            // The blank band is not the tiler's: the spans gathered for it were offered but are
            // not kept, and the parent reads them back off the settlement.
            _finished = true;
            return false;
          }

          _tiler.ReportBlank(onBlank, band, _scope);
        }
        else
        {
          var child = _scope.Start(_tiler.Children[0], _tiler.Each, Spans.Empty(band, Along), occurrence: _bands);
          var open = true;

          foreach (var s in _band)
            if (open && !child.Next(s))
              open = false;

          _values.Add(child.Close().Value);
        }

        _band.Clear();
        _bands++;
        _cursor += _tiler.Stride;
        return true;
      }

      public Settlement<IReadOnlyList<T>> Close()
      {
        _values.TrimExcess();

        // The cursor is bands VISITED times the stride: a band the policy omitted was still cut out
        // of the extent; an incomplete band at the end was not.
        return new Settlement<IReadOnlyList<T>>(
          _values,
          Spans.ToSize(_cursor, _across, Along),
          _values.Count == 0 ? Presence.Empty : Presence.Read);
      }
    }

    internal override Reach Retains => Reach.Spans(Stride);

    internal void ReportBlank(BlankRowStrategy onBlank, Plane<TSpace> band, ProjectorScope<TSpace> scope)
    {
      var noun = Stride == 1 ? "row" : "band";
      var at = scope.Locate(band).A1;

      if (onBlank.IsFault)
        throw scope.Failure($"the {noun} at {at} is blank, which is not allowed here", band, null, isFault: true);

      if (onBlank.Diagnostic is DiagnosticSeverity severity)
        scope.Report(severity, this, $"the {noun} at {at} is blank; it was skipped", band);
    }

    /// <summary>Whether every cell of a cut band is blank. The band is measured, so its extent is free.</summary>
    private static bool IsBlank(Plane<TSpace> band)
    {
      var area = band.Area;

      for (var row = 0; row < area.Height; row++)
        for (var column = 0; column < area.Width; column++)
          if (!band[column, row].IsBlank)
            return false;

      return true;
    }
  }
}
