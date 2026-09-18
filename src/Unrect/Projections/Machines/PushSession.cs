using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One application of a definition under the push interpreter: the driver that offers a
  /// source's spans to the root machine in order and closes it. The buffer is the source itself in
  /// this phase — every source is retaining — so a hold is bookkeeping a child keeps over the spans
  /// it was offered, and replay re-offers the same planes.
  /// </summary>
  internal sealed class PushSession<TSpace>
    where TSpace : class, ISpace
  {
    private readonly List<IRetaining> _open = new List<IRetaining>();

    private PushSession(Orientation driver) => Driver = driver;

    internal Orientation Driver { get; }

    /// <summary>
    /// Applies <paramref name="definition"/> to <paramref name="space"/> by pushing its rows at the
    /// machine the definition builds. A space that is an <see cref="IRowFeed"/> is fed as its rows
    /// arrive, and after every row the feed is told what the open machines still hold, so it may
    /// drop the rest; any other space retains its rows and is simply cut into them.
    /// </summary>
    internal static AppliedResult<T> Apply<T>(IProjectionDefinition<TSpace, T> definition, TSpace space, ProjectionContext context)
    {
      if (definition is null)
        throw new ArgumentNullException(nameof(definition));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var session = new PushSession<TSpace>(Orientation.Vertical);
      var whole = Plane<TSpace>.Of(space);
      var scope = new SessionScope<TSpace>(session, context, Spans.Empty(whole, Orientation.Vertical), Orientation.Vertical);
      var root = scope.Start(new Child(definition, default), definition, scope.Anchor);

      if (space is IRowFeed feed)
      {
        var width = whole.Width;

        while (feed.Advance())
        {
          var row = feed.Loaded - 1;
          var span = whole.Slice(new Offset(0, row), new Area(width, 1));

          if (!root.Next(span))
            break;

          session.Trim(feed, row, span, context);
        }
      }
      else
      {
        foreach (var span in Spans.Of(whole, Orientation.Vertical))
          if (!root.Next(span))
            break;
      }

      var settlement = root.Close();

      return new AppliedResult<T>(settlement.Value, root.Offset, settlement.Consumed, settlement.Presence);
    }

    internal void Opened(IRetaining machine) => _open.Add(machine);

    internal void Closed(IRetaining machine) => _open.Remove(machine);

    /// <summary>
    /// Releases every row before the oldest an open machine may still read, then checks the cap:
    /// more rows held than the feed allows is a fault naming the machine holding the oldest — the
    /// declaration asks for more than a forward pass can keep, which no tolerance boundary may
    /// absorb as an absent section.
    /// </summary>
    private void Trim(IRowFeed feed, int current, Plane<TSpace> span, ProjectionContext context)
    {
      var oldest = current;
      IRetaining? holder = null;

      // The holder named is the innermost machine holding the oldest row: an enclosing boundary
      // holds whatever its child holds, so blaming it would name the wrapper for the leaf's reach.
      foreach (var machine in _open)
      {
        if (machine.RetainFrom(current) is int from && from <= oldest)
        {
          oldest = from;
          holder = machine;
        }
      }

      feed.Release(oldest);

      if (feed.Cap is int cap && feed.Retained > cap)
      {
        var definition = holder?.Definition;
        var who = definition is null ? "the declaration" : PathRenderer.Describe(definition);

        throw context.Failure(
          definition ?? (IProjectionDefinition)_open[0].Definition,
          $"{who} is holding {feed.Retained} rows, from row {oldest + 1} through row {current + 1}, more than the {cap} the source allows: "
          + "the declaration asks for more than a forward pass can keep. Raise the source's buffer cap, or bound the shape that holds",
          span,
          null,
          null,
          isFault: true);
      }
    }
  }

  /// <summary>A machine that may still read rows it was offered, and says which.</summary>
  internal interface IRetaining
  {
    IProjectionDefinition Definition { get; }

    /// <summary>The first row this machine may still read, or null when it holds none.</summary>
    int? RetainFrom(int current);
  }

  internal sealed class SessionScope<TSpace> : ProjectorScope<TSpace>
    where TSpace : class, ISpace
  {
    internal SessionScope(PushSession<TSpace> session, ProjectionContext context, Plane<TSpace> anchor, Orientation driver)
    {
      Session = session;
      Context = context;
      Anchor = anchor;
      Driver = driver;
    }

    internal PushSession<TSpace> Session { get; }

    /// <summary>The axis spans arrive along here: the session's at the root, a held node's own where it is re-driven.</summary>
    internal override Orientation Driver { get; }

    internal override ProjectionContext Context { get; }

    internal override Plane<TSpace> Anchor { get; }

    internal override ProjectorScope<TSpace> At(ProjectionContext context, Plane<TSpace> anchor, Orientation driver)
      => new SessionScope<TSpace>(Session, context, anchor, driver);

    internal override ChildProjector<TSpace, T> Start<T>(Child edge, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, int? occurrence = null, bool strict = true, bool inheritSite = false)
    {
      var parent = Context;

      if (occurrence is int index)
        parent = parent.WithIndex(index).WithOrdinal(index);

      // A transparent wrapper's inner is labelled by whatever site was waiting for the wrapper —
      // the wrapper contributed no segment and claims no site — exactly as the pull engine hands
      // its context on unchanged.
      if (!inheritSite)
        parent = parent.WithUseSite(edge.Site);

      return new ChildProjector<TSpace, T>(this, parent, definition, anchor, strict);
    }

    internal override Settlement<T> Drive<T>(IProjector<TSpace, T> machine, Plane<TSpace> region, Orientation? along)
    {
      foreach (var span in Spans.Of(region, along))
        if (!machine.Next(span))
          break;

      return machine.Close();
    }
  }

  /// <summary>A started child seen without its result type: what a layout or a repeat feeds and closes.</summary>
  internal interface IChildHandle<TSpace>
    where TSpace : class, ISpace
  {
    bool Next(Plane<TSpace> span);

    object? CloseBoxed();

    Offset Offset { get; }

    /// <summary>What the child kept, offset included, from where it was started.</summary>
    Size Advance { get; }

    /// <summary>What the child itself consumed, offset excluded — a repeat's productivity guard reads this.</summary>
    Size Consumed { get; }

    Presence Presence { get; }

    bool PlacementFailed { get; }

    /// <summary>The spans offered to the child that it did not keep, oldest first — for the parent to feed to the successor.</summary>
    IEnumerable<Plane<TSpace>> Shortfall();
  }
}
