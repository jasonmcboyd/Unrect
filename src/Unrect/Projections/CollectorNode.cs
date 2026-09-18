using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A node that reads its region whole once the spans it was fed are known — a cell, a strip, a
  /// block, a header, a table view — rather than consuming spans as they arrive. Every collector has
  /// the same machine: take <see cref="SpanCount"/> spans, refuse the rest, and at close hand the
  /// region they cover to <see cref="Collect"/>. A collector under a declared rule is driven along
  /// whichever axis the rule runs, since the rule bounds it; left to bound itself it is held.
  /// </summary>
  /// <typeparam name="TSpace">The space this projection is written over.</typeparam>
  /// <typeparam name="TResult">What reading the region produces.</typeparam>
  internal abstract class CollectorNode<TSpace, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    private protected CollectorNode(Placement placement)
      : base(placement)
    {
    }

    /// <summary>How many spans the machine takes before it refuses: one for a leaf, the header rows for a header, none for the unit.</summary>
    internal virtual int SpanCount => 1;

    internal sealed override bool Collects => true;

    public sealed override IProjector<TSpace, TResult> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, TResult>(this, scope, SpanCount);

    /// <summary>Reads <paramref name="extent"/> whole — the collector's own act, once the spans it was fed are known.</summary>
    internal abstract Settlement<TResult> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope);
  }
}
