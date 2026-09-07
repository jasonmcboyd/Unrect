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
    /// naming a wrapper makes it opaque and it claims the segment.
    /// </summary>
    bool IsTransparent { get; }
  }

  /// <summary>
  /// EXPERIMENT (typed-spaces): a projection together with the <em>demand</em> it makes of the
  /// space it is applied to. <typeparamref name="TSpace"/> names the least capable space this
  /// declaration can run on; it appears in no member, so it is a phantom — the whole of its job is
  /// to be checked at the composition sites and at <c>Map</c>.
  /// <para>
  /// Contravariance is the load-bearing choice: a demand is an input, so a projection demanding
  /// less runs wherever more is offered. <see cref="IProjection{TResult}"/> — every projection
  /// written today — derives from <c>IProjection&lt;ISpace, TResult&gt;</c>, and variance therefore
  /// makes it an <c>IProjection&lt;IFormulaSpace, TResult&gt;</c> too, with no ceremony.
  /// Composition unifies to the most demanding child; the reverse conversion does not exist, which
  /// is what makes applying a formula-reading declaration to a plain grid a compile error rather
  /// than a fault.
  /// </para>
  /// <para>
  /// <typeparamref name="TResult"/> is invariant, unlike the <c>out T</c> of the experiment note:
  /// it is returned inside <see cref="ProjectionResult{TResult}"/>, which is an ordinary invariant
  /// type. That is what a projection has always been, so nothing is lost.
  /// </para>
  /// <para>
  /// Every instance is produced by this library and implements <see cref="IProjection{TResult}"/>,
  /// which is what licenses the one cast the typed layer makes (see
  /// <c>ProjectionExtensions.Plain</c>): the engine and the composites are written against the
  /// untyped form throughout, exactly as before, and the demand lives only in the static type.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The least capable space this projection can be applied to.</typeparam>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public interface IProjection<in TSpace, TResult> : IProjection
    where TSpace : class, ISpace
  {
  }

  /// <summary>
  /// A projection that reads a <typeparamref name="TResult"/> — the form a declaration is written
  /// and applied in. The untyped <see cref="IProjection"/> above it is what diagnostics and tooling
  /// walk, where the result type is neither known nor needed.
  /// <para>
  /// It demands nothing of its space beyond <see cref="ISpace"/>, which is what
  /// <c>IProjection&lt;ISpace, TResult&gt;</c> says, so it runs everywhere.
  /// </para>
  /// </summary>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public interface IProjection<TResult> : IProjection<ISpace, TResult>
  {
    /// <summary>
    /// Projects the projection's <em>resolved</em> extent: the placement has already been applied,
    /// so a projection can neither observe nor re-apply it.
    /// </summary>
    ProjectionResult<TResult> Project(ISpace extent, ProjectionContext context);

    /// <summary>A copy of this projection named <paramref name="name"/> — see <see cref="IProjection.Name"/>.</summary>
    IProjection<TResult> WithName(string name);

    /// <summary>A copy of this projection with <paramref name="placement"/> in place of its own.</summary>
    IProjection<TResult> WithPlacement(Placement placement);
  }
}
