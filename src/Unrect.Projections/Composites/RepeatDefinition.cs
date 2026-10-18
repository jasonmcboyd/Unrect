using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One declared item applied as many times as the space supports. The separator sits between
  /// items and never before the first; a leading gap is the repeat's own offset.
  /// </summary>
  public sealed class RepeatDefinition<TSpace, T> : DefinitionNode<TSpace, IReadOnlyList<T>>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// A repeat: <paramref name="item"/> applied again and again along <paramref name="orientation"/>, each occurrence's extent discovered from the item, until one finds nothing.
    /// <para>Fluent: <c>VerticalRepeat(item, separatedBy: …, atLeast: 1)</c>.</para>
    /// </summary>
    /// <param name="item">The pattern repeated.</param>
    /// <param name="separator">What lies between occurrences, when it is content; null for blank spans alone, which every occurrence steps over anyway.</param>
    /// <param name="orientation">Which way the occurrences follow one another.</param>
    /// <param name="atLeast">The fewest occurrences that is not a failure; not negative.</param>
    /// <param name="itemSite">How the item was declared, for its path segment; <c>default</c> for kind-and-position.</param>
    /// <param name="placement">Where the run starts.</param>
    public RepeatDefinition(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy<TSpace>? separator,
      Orientation orientation,
      int atLeast,
      UseSite itemSite,
      Placement<TSpace> placement)
      : base(placement)
    {
      Item = item ?? throw new ArgumentNullException(nameof(item));
      Separator = separator;
      Orientation = orientation;
      AtLeast = atLeast >= 0 ? atLeast : throw new ArgumentOutOfRangeException(nameof(atLeast), atLeast, "A repeat cannot require a negative number of occurrences.");
      ItemSite = itemSite;
      Children = new[] { new Child(item, itemSite) };
    }

    private IProjectionDefinition<TSpace, T> Item { get; }

    /// <summary>What the declaration called the item, for every occurrence of it to be labelled by.</summary>
    private UseSite ItemSite { get; }
    private IOffsetStrategy<TSpace>? Separator { get; }
    private Orientation Orientation { get; }
    private int AtLeast { get; }

    /// <inheritdoc/>
    public override string Description => Orientation == Orientation.Vertical ? "VerticalRepeat" : "HorizontalRepeat";

    /// <inheritdoc/>
    public override IReadOnlyList<Child> Children { get; }

    /// <summary>Along its orientation: a repeat walks occurrence by occurrence, each a band along it.</summary>
    public override Axes Axis => Orientation.Of();

    internal override Orientation? LeadsChildren(Orientation session) => Orientation;

    private bool SeparatorStreams => Separator is null || Separator.Begin(Orientation).Incremental;

    /// <summary>A separator with no per-span form is asked over the whole gap, so the repeat must have its extent first.</summary>
    internal override string? Holds => SeparatorStreams ? null : "its separator has no per-span form";

    /// <inheritdoc/>
    public override IProjector<TSpace, IReadOnlyList<T>> Build(ProjectorScope<TSpace> scope) => new Machine(this, scope);

    /// <summary>
    /// The walk, one span at a time. With no item open: after a committed occurrence the separator
    /// takes the gap; then an item is started non-strictly, so a placement that cannot find its
    /// anchor is a refusal read off the handle rather than a thrown failure. With an item open: the
    /// span is offered; a refusal closes the item, and it commits — advancing the run — or ends the
    /// run: a placement that failed, or an occurrence that occupied nothing. Nothing an ended run
    /// took past its last committed occurrence is the repeat's, so the parent reads it back off the
    /// settlement.
    /// </summary>
    private sealed class Machine : IProjector<TSpace, IReadOnlyList<T>>, IHolding
    {
      private readonly RepeatDefinition<TSpace, T> _repeat;
      private readonly ProjectorScope<TSpace> _scope;
      private readonly List<T> _values = new List<T>();
      private readonly List<Plane<TSpace>>? _gathered;
      private readonly IOffsetStrategy<TSpace>? _streamedSeparator;
      private ISizeScan<TSpace>? _separator;
      private Plane<TSpace>? _first;
      private int _offered;
      private int _along;
      private int _across;
      private int _attemptStart;
      private int _itemStart;
      private int _mark;
      private IChildHandle<TSpace, T>? _item;
      private bool _separating;
      private bool _absorbed;
      private bool _finished;

      public Machine(RepeatDefinition<TSpace, T> repeat, ProjectorScope<TSpace> scope)
      {
        _repeat = repeat;
        _scope = scope;

        // A separator with no per-span form is asked over the whole gap, which is known only once
        // every span is in: the spans are gathered, and the walk runs at Close over the gathered
        // extent with the separator's own answer for each gap. One with a per-span form is begun
        // afresh for every gap, since a scan may carry state from one span to the next.
        if (repeat.Separator is IOffsetStrategy<TSpace> separator)
        {
          if (separator.Begin(repeat.Orientation).Incremental)
            _streamedSeparator = separator;
          else
            _gathered = new List<Plane<TSpace>>();
        }
      }

      private Orientation Along => _repeat.Orientation;

      /// <summary>
      /// From the attempt in progress — its separator and its item — since those are what an
      /// attempt that ends the run hands back. A committed occurrence is final, so its spans are not
      /// held; the walk over a thousand occurrences costs one.
      /// </summary>
      public int? HeldFrom => _gathered is not null ? 0 : _finished || _first is null ? null : _attemptStart;

      public bool Next(Plane<TSpace> span)
      {
        if (_finished)
          return false;

        if (_first is null)
        {
          _first = span;
          _across = 0;

          if (Spans.Across(span.Extent, Along) == 0)
          {
            _finished = true;
            return false;
          }
        }

        if (_gathered is not null && _separator is null)
        {
          _gathered.Add(span);
          return true;
        }

        var position = _offered;
        _offered++;

        if (Offer(span, position))
          return true;

        _offered--;
        return false;
      }

      /// <summary>
      /// The walk for one span at <paramref name="position"/> — its index among the spans the
      /// repeat has been offered, which a replayed span keeps from its first offering.
      /// </summary>
      private bool Offer(Plane<TSpace> span, int position)
      {
        while (!_finished)
        {
          if (_item is null && !_separating)
            BeginAttempt(position);

          if (_item is null)
          {
            // The gap between occurrences, taken tentatively: it is the repeat's only if an
            // occurrence follows it.
            var region = Spans.Region(_first!.Value, position + 1, Along).Slice(Spans.Step(_attemptStart, Along));
            bool gap;

            try
            {
              gap = _separator!.Take(region, position - _attemptStart);
            }
            catch (OutOfBoundsException)
            {
              // No room for another separator, so there is no room for another item.
              _finished = true;
              return false;
            }
            catch (Exception exception) when (exception is not ProjectionException)
            {
              throw _scope.Failure(EngineRules.Threw("separator", exception), region, exception, EngineRules.IsFault(exception));
            }

            if (gap)
              return true;

            _separating = false;
            _item = StartItem(position);
          }

          if (_item.Next(span))
            return true;

          // The item refused this span: it commits and the span goes to a fresh attempt, or it
          // ended the run and the span is not the repeat's.
          if (!CloseItem())
            return false;
        }

        return false;
      }

      public Settlement<IReadOnlyList<T>> Close()
      {
        if (_gathered is not null && _first is Plane<TSpace> first)
        {
          _separator = new EagerSeparator(_repeat.Separator!, Spans.Region(first, _gathered.Count, Along), Along);

          foreach (var span in _gathered)
            if (!Next(span))
              break;
        }

        // Closing an item may replay what it did not keep into fresh attempts, which may leave
        // another item open; each replay moves strictly forward, so this ends.
        while (_item is not null && CloseItem())
        {
        }

        if (_absorbed)
          _scope.Report(DiagnosticSeverity.Info, _repeat, RepeatDefinition<TSpace, T>.EndedByTolerance(_values.Count), Extent());

        if (_values.Count < _repeat.AtLeast)
          throw _scope.Failure(_repeat, $"expected at least {_repeat.AtLeast} occurrences but found {_values.Count}", Extent(), null, null);

        return new Settlement<IReadOnlyList<T>>(_values, Spans.ToSize(_along, _across, Along));
      }

      private void BeginAttempt(int position)
      {
        _mark = _scope.Diagnostics.Mark();
        _attemptStart = position;

        if (_values.Count > 0 && (_streamedSeparator is not null || _separator is not null))
        {
          _separating = true;

          if (_streamedSeparator is not null)
            _separator = _streamedSeparator.Begin(_repeat.Orientation);
        }
        else
        {
          _item = StartItem(position);
        }
      }

      private IChildHandle<TSpace, T> StartItem(int at)
      {
        _itemStart = at;

        return _scope.Start(
          _repeat.Children[0],
          _repeat.Item,
          _first is Plane<TSpace> first ? Spans.EmptyAt(first, at, Along) : _scope.Anchor,
          occurrence: _values.Count,
          strict: false,
          leadingBlanks: Along);
      }

      /// <summary>
      /// Closes the open item: true when it committed and the walk goes on, false when it ended the
      /// run. A committed item that kept fewer spans than it took — a held item, placed at its close
      /// — hands the rest back, and they are re-offered here to the attempts that follow.
      /// </summary>
      private bool CloseItem()
      {
        var item = _item!;
        _item = null;

        var settlement = item.Close();

        if (item.PlacementFailed)
        {
          _scope.Diagnostics.Rollback(_mark);
          _finished = true;
          return false;
        }

        // An item that occupies nothing, or advances nowhere, would repeat forever.
        if (item.Consumed.Width == 0 || item.Consumed.Height == 0 || Spans.Along(item.Advance, Along) == 0)
        {
          _absorbed = item.Absorbed;
          _scope.Diagnostics.Rollback(_mark);
          _finished = true;
          return false;
        }

        _values.Add(settlement.Value);
        _along = _itemStart + Spans.Along(item.Advance, Along);
        _across = Math.Max(_across, Spans.Across(item.Advance, Along));

        var position = _along;

        foreach (var span in item.Shortfall())
          if (!Offer(span, position++))
            return false;

        return !_finished;
      }

      private Plane<TSpace> Extent()
        => _first is Plane<TSpace> first ? Spans.Region(first, _offered, Along) : _scope.Anchor;
    }

    /// <summary>
    /// A separator whose scan answers only over its whole extent, asked the way the whole-extent
    /// walk asked it: settled over the gap from the attempt's start to the end of the repeat's
    /// extent, once per attempt, its answer then stepped through span by span — each span in the
    /// gap taken, the one after refused. No room for its answer is no room for another item.
    /// </summary>
    private sealed class EagerSeparator : ISizeScan<TSpace>
    {
      private readonly IOffsetStrategy<TSpace> _strategy;
      private readonly Plane<TSpace> _whole;
      private readonly Orientation _along;
      private int _start = -1;
      private Offset _offset;

      public EagerSeparator(IOffsetStrategy<TSpace> strategy, Plane<TSpace> whole, Orientation along)
      {
        _strategy = strategy;
        _whole = whole;
        _along = along;
      }

      public bool Incremental => true;

      public bool Take(Plane<TSpace> region, int row)
      {
        var start = _along == Orientation.Vertical
          ? region.Origin.Row - _whole.Origin.Row
          : region.Origin.Column - _whole.Origin.Column;

        if (start != _start)
        {
          var remaining = _whole.Slice(Spans.Step(start, _along));
          var offset = Scans.FoldOffset(_strategy.Begin(_along), remaining, _along);

          if (offset.Column > remaining.Width || offset.Row > remaining.Height)
            throw new OutOfBoundsException();

          _start = start;
          _offset = offset;
        }

        return row < Spans.Along(_offset, _along);
      }

      public int? Across(Plane<TSpace> region, int taken, bool final) => Spans.Across(_offset, _along);

      /// <summary>The gap, once the span after it was refused; the spans running out inside it is no room for another item. The machine reads only <see cref="Take"/>; this is the contract kept, reached by nothing today.</summary>
      public int Along(Plane<TSpace> region, int taken)
        => _start >= 0 && taken >= Spans.Along(_offset, _along) ? taken : throw new OutOfBoundsException();

      public Size? Required => null;
    }

    /// <summary>What a repeat holds is the attempt in progress; see <see cref="Machine.HeldFrom"/>.</summary>
    internal override Reach Retains => Reach.Extent;

    /// <summary>
    /// The guided form of a documented trap: a repetition that ran out of productivity on an item
    /// whose failure had just been absorbed. It cannot tell "no more of these" from "one of these
    /// was broken", so the declaration almost certainly means something else.
    /// <para>
    /// It says why the run ended; it does not end it. Only the productivity guard does that, so an
    /// absorbed item that still consumed a declared extent goes on repeating and says nothing here
    /// — the trap needs both halves, the tolerance and the standstill.
    /// </para>
    /// <para>
    /// The occurrence is bracketed as the paths render it, so it lines up with the
    /// <c>VerticalRepeat[n]</c> segments a reader is already looking at.
    /// </para>
    /// </summary>
    internal static string EndedByTolerance(int occurrence)
      => $"the repetition ended at occurrence [{occurrence}]: the item's failure was absorbed by a tolerance "
       + "boundary, and a tolerated item cannot drive a repetition — drop the boundary, or declare "
       + "atLeast: 0 if an empty run is the concern";
  }
}
