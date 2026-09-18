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
    private PushSession(Orientation driver) => Driver = driver;

    internal Orientation Driver { get; }

    /// <summary>Applies <paramref name="definition"/> to <paramref name="space"/> by pushing its rows at the machine the definition builds.</summary>
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

      foreach (var span in Spans.Of(whole, Orientation.Vertical))
        if (!root.Next(span))
          break;

      var settlement = root.Close();

      return new AppliedResult<T>(settlement.Value, root.Offset, settlement.Consumed, settlement.Presence);
    }
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

    private PushSession<TSpace> Session { get; }

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
