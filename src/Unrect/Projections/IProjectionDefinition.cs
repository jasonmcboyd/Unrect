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
  public interface IProjectionDefinition
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

    /// <summary>
    /// The projection's declared children, in declaration order, each with the use site it was
    /// written at; empty for a leaf. Complete unless <see cref="Opacity"/> says otherwise.
    /// </summary>
    IReadOnlyList<Child> Children { get; }

    /// <summary>
    /// Everything a declaration wrote on this projection that is not its structure — <see
    /// cref="Name"/>, <see cref="UnitName"/>, <see cref="IsScaffolding"/> and <see cref="Placement"/>
    /// as one record. The four members above read through it.
    /// </summary>
    Annotations Annotations { get; }

    /// <summary>
    /// The label <c>.AsUnit</c> gave this projection, or null. When set, its path segment is that
    /// label rather than its description or use-site name (joined as <c>label:name</c> when it is
    /// also <c>.Named</c>), and a wrapper carrying one claims a segment it would otherwise not.
    /// Folding a path is a separate mark — <see cref="IsScaffolding"/>, on the parts to drop.
    /// </summary>
    string? UnitName { get; }

    /// <summary>
    /// True when <c>.AsScaffolding</c> marked this projection a composition's internal plumbing: a
    /// collapsed path drops its segment and carries only its occurrence index up onto the nearest
    /// segment that was kept.
    /// </summary>
    bool IsScaffolding { get; }

    /// <summary>
    /// True for a projection the declaration did not write as a level of its own — <c>Select</c>,
    /// <c>Padded</c>, <c>Until</c>, and the <c>Else</c>/<c>Optional</c> boundary. A structural
    /// fact, and only that: whether a path skips it is the renderer's rule (an unnamed wrapper with
    /// no unit label contributes no segment), not the projection's.
    /// </summary>
    bool IsWrapper { get; }

    /// <summary>
    /// Null when <see cref="Children"/> is the whole truth. A sentence when it is not — a
    /// composite whose children exist only while it runs, which would otherwise read as a leaf to
    /// anything walking a declaration without a space. A renderer shows the sentence in the
    /// children's place.
    /// </summary>
    string? Opacity { get; }
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
  /// a generic method — <c>static IProjectionDefinition&lt;TSpace, decimal&gt; Total&lt;TSpace&gt;() where
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
  public interface IProjectionDefinition<TSpace, TResult> : IProjectionDefinition
    where TSpace : class, ISpace
  {
    /// <summary>
    /// Projects the projection's <em>resolved</em> extent — the region the engine cut for it, with
    /// the placement already applied.
    /// </summary>
    ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context);

    /// <summary>
    /// A copy of this projection carrying <paramref name="annotations"/> in place of its own — what
    /// <c>.Named</c>, <c>.AsUnit</c>, <c>.AsScaffolding</c> and every placement stage build.
    /// </summary>
    IProjectionDefinition<TSpace, TResult> With(Annotations annotations);
  }
}
