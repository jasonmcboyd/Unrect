using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One declared item applied as many times as the space supports. The separator sits between
  /// items and never before the first; a leading gap is the repeat's own offset.
  /// </summary>
  internal sealed class RepeatDefinition<TSpace, T> : DefinitionNode<TSpace, IReadOnlyList<T>>
    where TSpace : class, ISpace
  {
    public RepeatDefinition(
      IProjectionDefinition<TSpace, T> item,
      IOffsetStrategy? separator,
      Orientation orientation,
      int atLeast,
      UseSite itemSite,
      Placement placement)
      : base(placement)
    {
      Item = item ?? throw new ArgumentNullException(nameof(item));
      Separator = separator;
      Orientation = orientation;
      AtLeast = atLeast;
      ItemSite = itemSite;
      Children = new[] { new Child(item, itemSite) };
    }

    private IProjectionDefinition<TSpace, T> Item { get; }

    /// <summary>What the declaration called the item, for every occurrence of it to be labelled by.</summary>
    private UseSite ItemSite { get; }
    private IOffsetStrategy? Separator { get; }
    private Orientation Orientation { get; }
    private int AtLeast { get; }

    public override string Description => Orientation == Orientation.Vertical ? "VerticalRepeat" : "HorizontalRepeat";

    public override IReadOnlyList<Child> Children { get; }

    /// <summary>
    /// The walk. Every attempt is handed the tail from the cursor, left unsettled, so a repeat over
    /// an extent whose height is still being discovered streams as far as its item lets it: an item
    /// that derives its extent takes a band at a time.
    /// <para>
    /// The honest limit is an item with its OWN declared area: that area's strategy is handed the
    /// tail and asks how tall it is — <c>Record</c>'s full-width row among them — which settles the
    /// extent before the first occurrence.
    /// </para>
    /// </summary>
    /// <summary>Along its orientation — unless its separator has no per-span form, in which case it is held and read as the pull engine reads it.</summary>
    public override Axes Axis => SeparatorStreams ? Orientation.Of() : Axes.None;

    private bool SeparatorStreams => Separator is null || PlacementRules.TryOffsetRule(Separator, Orientation, out _);

    /// <summary>A repeat hands back the item that failed to place, and the gap before it.</summary>
    public override Reach Reach => Reach.Extent;

    public override IProjector<TSpace, IReadOnlyList<T>> Build(ProjectorScope<TSpace> scope)
      => SeparatorStreams
        ? new Machine(this, scope)
        : new SpanCountProjector<TSpace, IReadOnlyList<T>>(this, scope, 1);

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
      private readonly OffsetRule? _separator;
      private Plane<TSpace>? _first;
      private int _offered;
      private int _along;
      private int _across;
      private int _attemptStart;
      private int _mark;
      private IChildHandle<TSpace, T>? _item;
      private bool _separating;
      private bool _absorbed;
      private bool _finished;
      private bool _closed;

      public Machine(RepeatDefinition<TSpace, T> repeat, ProjectorScope<TSpace> scope)
      {
        _repeat = repeat;
        _scope = scope;

        if (repeat.Separator is IOffsetStrategy separator)
          PlacementRules.TryOffsetRule(separator, repeat.Orientation, out _separator);
      }

      private Orientation Along => _repeat.Orientation;

      /// <summary>
      /// From the attempt in progress — its separator and its item — since those are what an
      /// attempt that ends the run hands back. A committed occurrence is final, so its spans are not
      /// held; the walk over a thousand occurrences costs one.
      /// </summary>
      public int? HeldFrom => _finished || _first is null ? null : _attemptStart;

      public bool Next(Plane<TSpace> span)
      {
        if (_closed)
          throw _scope.Context.Failure(_repeat, $"{PathRenderer.Describe(_repeat)} was fed a span after it was closed", span, null, null, isFault: true);

        if (_finished)
          return false;

        if (_first is null)
        {
          _first = span;
          _across = 0;

          if (Spans.Across(span.Declared.Size, Along) == 0)
          {
            _finished = true;
            return false;
          }
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
            var region = Spans.Region(_first!.Value, position + 1, Along).Slice(Spans.Step(_attemptStart, Along)).Erased();
            OffsetStep step;

            try
            {
              step = _separator!.Next(region, position - _attemptStart, out _);
            }
            catch (ProjectionException)
            {
              throw;
            }
            catch (OutOfBoundsException)
            {
              // No room for another separator, so there is no room for another item.
              _finished = true;
              return false;
            }
            catch (Exception exception)
            {
              throw _scope.Context.Failure(ProjectionEngine.Threw("separator", exception), region, exception, ProjectionEngine.IsFault(exception));
            }

            if (step == OffsetStep.Skip)
              return true;

            _separating = false;

            if (step == OffsetStep.StartNext)
            {
              _item = StartItem(position + 1);
              return true;
            }

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
        _closed = true;

        // Closing an item may replay what it did not keep into fresh attempts, which may leave
        // another item open; each replay moves strictly forward, so this ends.
        while (_item is not null && CloseItem())
        {
        }

        if (_absorbed)
          _scope.Context.Report(DiagnosticSeverity.Info, _repeat, RepeatDefinition<TSpace, T>.EndedByTolerance(_values.Count), Extent());

        if (_values.Count < _repeat.AtLeast)
          throw _scope.Context.Failure(_repeat, $"expected at least {_repeat.AtLeast} occurrences but found {_values.Count}", Extent(), null, null);

        return new Settlement<IReadOnlyList<T>>(
          _values,
          Spans.ToSize(_along, _across, Along),
          _values.Count == 0 ? Presence.Empty : Presence.Read);
      }

      private void BeginAttempt(int position)
      {
        _mark = _scope.Context.Diagnostics.Mark();
        _attemptStart = position;

        if (_values.Count > 0 && _separator is not null)
          _separating = true;
        else
          _item = StartItem(position);
      }

      private int _itemStart;

      private IChildHandle<TSpace, T> StartItem(int at)
      {
        _itemStart = at;

        return _scope.Start(
          _repeat.Children[0],
          _repeat.Item,
          _first is Plane<TSpace> first ? Spans.EmptyAt(first, at, Along) : _scope.Anchor,
          occurrence: _values.Count,
          strict: false);
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
          _scope.Context.Diagnostics.Rollback(_mark);
          _finished = true;
          return false;
        }

        // An item that occupies nothing, or advances nowhere, would repeat forever.
        if (item.Consumed.Width == 0 || item.Consumed.Height == 0 || Spans.Along(item.Advance, Along) == 0)
        {
          _absorbed = item.Presence == Presence.Absorbed;
          _scope.Context.Diagnostics.Rollback(_mark);
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

    /// <summary>What a repeat holds is the attempt in progress; see <see cref="Machine.HeldFrom"/>.</summary>
    internal override Reach Retains => Reach.Extent;

    public override ProjectionResult<IReadOnlyList<T>> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      // The across axis, read without settling a discovered extent: a vertical walk asks the width,
      // a horizontal one the height. HasBand probes only the along axis, so without this a band with
      // nothing across it would attempt an item over empty space and trip the productivity guard.
      var acrossExtent = Orientation == Orientation.Vertical
        ? extent.Width
        : extent.Area.Height;

      var values = new List<T>();
      var along = 0;
      var across = 0;
      var absorbed = false;

      while (acrossExtent > 0)
      {
        var mark = context.Diagnostics.Mark();

        if (!TryCollect(extent, context, values, ref along, ref across, ref absorbed))
        {
          // An attempt that is not collected leaves nothing behind — not even what it tolerated on
          // the way to being discarded.
          context.Diagnostics.Rollback(mark);
          break;
        }
      }

      // Reported after that rollback, and it is the only thing left saying so: the tolerated
      // failure's own warning went with the attempt that produced it.
      if (absorbed)
        context.Report(DiagnosticSeverity.Info, this, EndedByTolerance(values.Count), extent);

      if (values.Count < AtLeast)
        throw context.Failure($"expected at least {AtLeast} occurrences but found {values.Count}", extent);

      // Zero occurrences is a statement about the data — the repetition looked and the data held
      // none of these — which an enclosing shape reads as Empty rather than inferring from a zero.
      return new ProjectionResult<IReadOnlyList<T>>(
        values,
        Extent(along, across),
        values.Count == 0 ? Presence.Empty : Presence.Read);
    }

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

    /// <summary>
    /// One attempt: separate, place, project, and collect. False means the repetition is over, and
    /// whatever the attempt did is discarded by the caller — which is why <paramref name="absorbed"/>
    /// travels back out here rather than being reported in place.
    /// </summary>
    private bool TryCollect(Plane<TSpace> extent, ProjectionContext context, List<T> values, ref int along, ref int across, ref bool absorbed)
    {
      // The cursor is tentative until an item is collected, so a separator followed by nothing
      // (a trailing blank band) is not counted as consumed.
      var cursor = along;
      var reach = across;

      if (values.Count > 0 && !TrySeparate(extent.Slice(Step(cursor)), context, ref cursor, ref reach))
        return false;

      // A forward probe: the extent has a band at the cursor, asked one band at a time so a
      // discovered extent advances its scan only as far as the reading has reached.
      if (!HasBand(extent, cursor))
        return false;

      // The tail from the cursor, left unsettled so an item that derives its extent reads only the
      // band it takes.
      var remaining = extent.Slice(Step(cursor));

      // The index belongs to the repeat's own segment; the label belongs to the item, which claims
      // it on the way in. Descend clears the index afterwards, so the item's own children are
      // unaffected. The ordinal is the same occurrence number stamped so it survives that Descend —
      // it is how a decoupled record recovers which body row it is projecting.
      var scope = context.WithIndex(values.Count).WithOrdinal(values.Count).WithUseSite(ItemSite);

      // Only the item's own placement stops the repetition; a failure deeper inside it is an
      // error, so intra-block format drift is loud rather than silently truncating.
      if (!ProjectionEngine.TryApply(Item, remaining, scope, out var applied))
        return false;

      // An item that occupies nothing, or advances nowhere, would repeat forever. This number is
      // the sole decider, and deliberately: presence explains a stop, it never causes one.
      // A boundary under a declared area absorbs and still consumes the extent the placement
      // claimed, so a repeat of item.Optional().Sized(…) goes on collecting tolerated defaults —
      // which is what it did before presence existed, and changing that would be a semantic change
      // wearing a diagnostic's clothes.
      if (applied.Consumed.Width == 0 || applied.Consumed.Height == 0 || Along(applied.Advance) == 0)
      {
        // Read on the way out: why the item that ended the run had nothing to give.
        absorbed = applied.Presence == Presence.Absorbed;
        return false;
      }

      values.Add(applied.Value);
      along = cursor + Along(applied.Advance);
      across = Math.Max(reach, Across(applied.Advance));
      return true;
    }

    private bool TrySeparate(Plane<TSpace> remaining, ProjectionContext context, ref int cursor, ref int reach)
    {
      if (Separator is null)
        return true;

      Offset offset;
      try
      {
        offset = Separator.GetOffset(remaining.Erased());
      }
      catch (ProjectionException)
      {
        throw;
      }
      catch (OutOfBoundsException)
      {
        // No room for another separator, so there is no room for another item.
        return false;
      }
      catch (Exception exception)
      {
        throw context.Failure(ProjectionEngine.Threw("separator", exception), remaining, exception, ProjectionEngine.IsFault(exception));
      }

      if (offset.Width > remaining.Width
        || (offset.Height > 0 && !remaining.HasRow(offset.Height - 1)))
        return false;

      cursor += Along(offset.Size);
      reach = Math.Max(reach, Across(offset.Size));
      return true;
    }

    /// <summary>Whether <paramref name="extent"/> has a band at <paramref name="cursor"/> along the repeat's axis.</summary>
    private bool HasBand(Plane<TSpace> extent, int cursor)
      => Orientation == Orientation.Vertical
        ? extent.HasRow(cursor)
        : cursor < extent.Width;

    private Offset Step(int along) => Orientation == Orientation.Vertical ? new Offset(0, along) : new Offset(along, 0);

    private int Along(Size size) => Orientation == Orientation.Vertical ? size.Height : size.Width;

    private int Across(Size size) => Orientation == Orientation.Vertical ? size.Width : size.Height;

    private Size Extent(int along, int across)
      => Orientation == Orientation.Vertical ? new Size(across, along) : new Size(along, across);
  }
}
