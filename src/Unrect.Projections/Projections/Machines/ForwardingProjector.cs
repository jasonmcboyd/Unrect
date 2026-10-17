using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The machine a transparent wrapper is: its one child started behind the wrapper's own
  /// placement, every span forwarded, and the child's settlement handed up — through
  /// <see cref="Finish"/>, where a wrapper that changes the value does so.
  /// </summary>
  internal abstract class ForwardingProjector<TSpace, TInner, TResult> : IProjector<TSpace, TResult>
    where TSpace : class, ISpace
  {
    private Plane<TSpace>? _first;
    private int _offered;

    protected ForwardingProjector(IProjectionDefinition owner, ProjectorScope<TSpace> scope, Child edge, IProjectionDefinition<TSpace, TInner> inner)
    {
      Owner = owner;
      Scope = scope;
      Child = scope.Start(edge, inner, scope.Anchor, inheritSite: true);
    }

    protected IProjectionDefinition Owner { get; }

    protected ProjectorScope<TSpace> Scope { get; }

    protected IChildHandle<TSpace, TInner> Child { get; }

    /// <summary>The region this wrapper was offered, for a failure to be located at.</summary>
    protected Plane<TSpace> Extent
      => _first is Plane<TSpace> first ? Spans.Region(first, _offered, Scope.Driver) : Scope.Anchor;

    public bool Next(Plane<TSpace> span)
    {
      _first ??= span;
      _offered++;

      return Child.Next(span);
    }

    public Settlement<TResult> Close()
    {
      var settlement = Child.Close();

      return new Settlement<TResult>(Finish(settlement.Value), Child.Advance, Child.Absorbed);
    }

    /// <summary>The wrapper's own value from the child's; the identity for a wrapper that adds nothing.</summary>
    protected abstract TResult Finish(TInner value);
  }
}
