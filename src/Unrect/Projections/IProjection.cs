using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A declared region of a space together with the projection that turns it into a value. A
  /// projection says where it sits inside the space it is handed (<see cref="Placement"/>) and what
  /// to make of that region; <see cref="ProjectionEngine"/> is the only code that applies the
  /// placement.
  /// </summary>
  public interface IProjection
  {
    /// <summary>
    /// The explicit name given by <c>.Named</c>, or null. The top rung of the naming ladder: when
    /// set, it is what a failure path and every diagnostic call this projection, ahead of any
    /// use-site label or description.
    /// </summary>
    string? Name { get; }

    /// <summary>The structural fallback name — the factory that produced the projection, e.g. <c>"Column(4)"</c>.</summary>
    string Description { get; }

    /// <summary>Where this projection sits, and how much of its extent it declares, within the space it is handed.</summary>
    Placement Placement { get; }

    /// <summary>The projection's declared children, in declaration order; empty for a leaf.</summary>
    IReadOnlyList<IProjection> Children { get; }

    /// <summary>
    /// True only for unnamed wrappers (<c>Select</c>, <c>Padded</c>, <c>Until</c>, and the
    /// <c>Else</c>/<c>Optional</c> boundary), which contribute no segment to a failure path;
    /// naming a wrapper or marking it a unit boundary makes it opaque and it claims the segment.
    /// </summary>
    bool IsTransparent { get; }

    /// <summary>
    /// True when <c>.AsUnit</c> gave this projection a unit label: its path segment is that label
    /// rather than its description or use-site name (joined as <c>label:name</c> when it is also
    /// <c>.Named</c>), and it is opaque even where it would otherwise be transparent. Folding a
    /// path is a separate mark — <c>.AsScaffolding</c>, on the parts to drop.
    /// </summary>
    bool IsUnitBoundary { get; }
  }

  /// <summary>
  /// A projection together with the space it reads: <typeparamref name="TSpace"/> is what the
  /// declaration is written over, and what <c>Map</c> must be handed.
  /// <para>
  /// It is the type the whole declaration agrees on. A file names its space once, and everything
  /// built there — leaves, layouts, matchers, the lot — is a projection over that space, so
  /// composing two of them is an ordinary type check rather than a bookkeeping exercise.
  /// </para>
  /// <para>
  /// <b>A shared helper states its minimum in a constraint, not in its return type.</b> Write it as
  /// a generic method — <c>static IProjection&lt;TSpace, decimal&gt; Total&lt;TSpace&gt;() where
  /// TSpace : class, ISheetCells</c> — and it composes into any file whose space can answer it,
  /// instantiated at that file's own space. A helper that named a space outright would hand back a
  /// projection over that space and nothing else, which is a different and much smaller thing.
  /// </para>
  /// <para>
  /// <typeparamref name="TResult"/> is what projecting produces, and the whole of what a caller gets
  /// back: by the time <see cref="Project"/> runs the placement is resolved, so a projection can
  /// neither observe nor re-apply where it sits.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this projection is written over.</typeparam>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public interface IProjection<TSpace, TResult> : IProjection
    where TSpace : class, ISpace
  {
    /// <summary>
    /// Projects the projection's <em>resolved</em> extent — the region the engine cut for it, with
    /// the placement already applied.
    /// </summary>
    ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context);

    /// <summary>A copy of this projection named <paramref name="name"/> — see <see cref="IProjection.Name"/>.</summary>
    IProjection<TSpace, TResult> WithName(string name);

    /// <summary>A copy of this projection with <paramref name="placement"/> in place of its own.</summary>
    IProjection<TSpace, TResult> WithPlacement(Placement placement);
  }
}
