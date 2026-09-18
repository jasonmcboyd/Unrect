using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>The engine's scope: a position in the run plus the driver, the anchor and the handle children report to.</summary>
  internal sealed class SessionScope<TSpace> : ProjectorScope<TSpace>
    where TSpace : class, ISpace
  {
    internal SessionScope(TreePosition position, DiagnosticCollector diagnostics, Plane<TSpace> anchor, Orientation driver, IChildRegistry<TSpace>? owner)
      : base(position, diagnostics)
    {
      Anchor = anchor;
      Driver = driver;
      Owner = owner;
    }

    /// <summary>The scope a run over <paramref name="space"/> starts from: the root position, a fresh collector, rows as the driver.</summary>
    internal static SessionScope<TSpace> Root(TSpace space)
      => new SessionScope<TSpace>(TreePosition.Root, new DiagnosticCollector(), Spans.Empty(Plane<TSpace>.Of(space), Orientation.Vertical), Orientation.Vertical, null);

    /// <summary>The handle children started here report to; null at the root.</summary>
    internal IChildRegistry<TSpace>? Owner { get; }

    /// <summary>The axis spans arrive along here: the session's at the root, a held node's own where it is re-driven.</summary>
    internal override Orientation Driver { get; }

    internal override Plane<TSpace> Anchor { get; }

    private protected override ProjectorScope<TSpace> Derive(TreePosition position)
      => new SessionScope<TSpace>(position, Diagnostics, Anchor, Driver, Owner);

    internal override ProjectorScope<TSpace> At(Plane<TSpace> anchor, Orientation driver)
      => new SessionScope<TSpace>(Position, Diagnostics, anchor, driver, Owner);

    internal override ProjectorScope<TSpace> Within(IChildRegistry<TSpace> owner, Plane<TSpace> anchor, Orientation driver)
      => new SessionScope<TSpace>(Position, Diagnostics, anchor, driver, owner);

    internal override IChildHandle<TSpace, T> Start<T>(Child edge, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> anchor, int? occurrence = null, bool strict = true, bool inheritSite = false)
    {
      ProjectorScope<TSpace> parent = this;

      if (occurrence is int index)
        parent = parent.WithIndex(index).WithOrdinal(index);

      // A transparent wrapper's inner is labelled by whatever site was waiting for the wrapper —
      // the wrapper contributed no segment and claims no site.
      if (!inheritSite)
        parent = parent.WithUseSite(edge.Site);

      var child = new ChildProjector<TSpace, T>(this, parent, definition, anchor, strict);
      Owner?.Opened(child);
      return child;
    }

    internal override Settlement<T> Drive<T>(IProjector<TSpace, T> machine, Plane<TSpace> region, Orientation? along)
    {
      foreach (var span in Spans.Of(region, along))
        if (!machine.Next(span))
          break;

      return machine.Close();
    }
  }
}
