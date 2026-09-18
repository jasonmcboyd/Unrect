using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One application of a definition under the push interpreter: the loop that offers a source's
  /// row spans to the root handle in order, the trim that releases what no open machine may still
  /// read, and the result. Everything else a machine needs is the scope, which stands on its own.
  /// </summary>
  internal static class PushSession<TSpace>
    where TSpace : class, ISpace
  {

    /// <summary>
    /// Applies <paramref name="definition"/> to <paramref name="space"/> by pushing its rows at the
    /// machine the definition builds. A space that is an <see cref="IRowFeed"/> is fed as its rows
    /// arrive, and after every row the feed is told what the open machines still hold, so it may
    /// drop the rest; any other space retains its rows and is simply cut into them.
    /// </summary>
    internal static AppliedResult<T> Apply<T>(IProjectionDefinition<TSpace, T> definition, TSpace space, SessionScope<TSpace> scope)
    {
      if (definition is null)
        throw new ArgumentNullException(nameof(definition));
      if (space is null)
        throw new ArgumentNullException(nameof(space));

      var whole = Plane<TSpace>.Of(space);
      var root = scope.Start(new Child(definition, default), definition, scope.Anchor);

      if (space is IRowFeed feed)
      {
        var width = whole.Width;

        // Row-indexed rather than driven off what the feed has loaded: a direct read inside a
        // machine may have loaded rows ahead of the offer, and every one of them is still offered.
        for (var row = 0; Load(feed, row, definition, whole, scope); row++)
        {
          var span = whole.Slice(new Offset(0, row), new Area(width, 1));

          if (!root.Next(span))
            break;

          Trim(feed, root, definition, row, span, scope);
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

    /// <summary>
    /// Has the feed load <paramref name="row"/>, or says the source is exhausted. A source that
    /// throws — the disk, a file replaced mid-read — is a fault, never a statement about the data.
    /// </summary>
    private static bool Load<T>(IRowFeed feed, int row, IProjectionDefinition<TSpace, T> definition, Plane<TSpace> whole, ProjectorScope<TSpace> scope)
    {
      while (feed.Loaded <= row)
      {
        bool more;

        try
        {
          more = feed.Advance();
        }
        catch (ProjectionException)
        {
          throw;
        }
        catch (Exception exception)
        {
          var at = row < whole.Area.Height ? whole.Slice(new Offset(0, row), new Area(whole.Width, 1)) : whole;

          throw scope.Failure(definition, $"the source threw {exception.GetType().Name}: {exception.Message}", at, null, exception, isFault: true);
        }

        if (!more)
          return false;
      }

      return true;
    }

    /// <summary>
    /// Releases every row before the oldest one still needed, asked of the root and answered for
    /// the whole tree, then checks the cap: more rows held than the feed allows is a fault naming
    /// the innermost machine holding that oldest row — an enclosing boundary holds whatever its
    /// child holds, so blaming it would name the wrapper for the leaf's reach.
    /// </summary>
    private static void Trim(IRowFeed feed, IChildHandle<TSpace> root, IProjectionDefinition definition, int current, Plane<TSpace> span, ProjectorScope<TSpace> scope)
    {
      var hold = root.Retained(current);
      var oldest = hold?.Row ?? current;

      feed.Release(oldest);

      if (feed.Cap is int cap && feed.Retained > cap)
      {
        var who = hold is Hold held ? PathRenderer.Describe(held.Holder) : "the declaration";

        throw scope.Failure(
          hold?.Holder ?? definition,
          $"{who} is holding {feed.Retained} rows, from row {oldest + 1} through row {current + 1}, more than the {cap} the source allows: "
          + "the declaration asks for more than a forward pass can keep. Raise the source's buffer cap, or bound the shape that holds",
          span,
          null,
          null,
          isFault: true);
      }
    }
  }

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
