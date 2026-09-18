using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The unit: a projection that always accepts, reads nothing, consumes nothing, and says so with
  /// <see cref="Presence.Empty"/>. It is what a flow can be composed with without changing — the
  /// identity element the layout algebra was missing while a zero-consuming child was
  /// indistinguishable from a repetition running out.
  /// <para>
  /// Not vocabulary, and not a factory on <c>Projection</c>: no document is shaped like nothing, and
  /// vocabulary follows documents. What it exists for is that the laws close — "delete an empty
  /// child" and "flatten nested flows" are sound rewrites only if the thing being deleted can be
  /// named — so it is available to the law tests and to whatever mints one while rewriting later.
  /// </para>
  /// </summary>
  internal sealed class NothingDefinition<TSpace, T> : DefinitionNode<TSpace, T>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// The one of them. A projection is an immutable value applied to many spaces, and this one has
    /// no state to vary, so there is nothing for a second instance to be.
    /// </summary>
    public static readonly IProjectionDefinition<TSpace, T> Instance = new NothingDefinition<TSpace, T>();

    private NothingDefinition()
      : base(Placement.Default)
    {
    }

    public override string Description => "Nothing";

    public override IProjector<TSpace, T> Build(ProjectorScope<TSpace> scope)
      => new SpanCountProjector<TSpace, T>(this, scope, 0);

    internal override bool Collects => true;


    internal override Settlement<T> Collect(Plane<TSpace> extent, ProjectorScope<TSpace> scope)
      => new Settlement<T>(default!, new Size(0, 0), Presence.Empty);
  }
}
