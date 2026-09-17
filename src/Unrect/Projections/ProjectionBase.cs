using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The base for every projection: a base for <em>construction</em>, not behaviour. It carries the
  /// <see cref="Annotations"/> every projection has and the one clone that changes them; what a
  /// projection does with its extent is its own, and what a modifier wraps it in is the modifier's.
  /// </summary>
  public abstract class ProjectionBase : IProjection
  {
    private static readonly Child[] NoChildren = Array.Empty<Child>();

    /// <summary>
    /// Fixes where the projection sits. Every projection has a placement from the moment it exists,
    /// so the engine never has to ask whether one was declared.
    /// </summary>
    /// <param name="placement">Where this projection sits within the space it is handed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    private protected ProjectionBase(Placement placement)
    {
      Annotations = Annotations.Default.WithPlacement(placement);
    }

    /// <inheritdoc/>
    public Annotations Annotations { get; private set; }

    /// <inheritdoc/>
    public string? Name => Annotations.Name;

    /// <inheritdoc/>
    public Placement Placement => Annotations.Placement;

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <summary>No children by default; a composite overrides this to declare its own.</summary>
    public virtual IReadOnlyList<Child> Children => NoChildren;

    /// <summary>A level of the tree by default; only a wrapper overrides this to true.</summary>
    public virtual bool IsWrapper => false;

    /// <summary>Children are the whole truth by default; a layout overrides this to say why they are not.</summary>
    public virtual string? Opacity => null;

    /// <inheritdoc/>
    public string? UnitName => Annotations.UnitName;

    /// <inheritdoc/>
    public bool IsScaffolding => Annotations.IsScaffolding;

    /// <summary>
    /// This projection carrying <paramref name="annotations"/> — a copy, of this same type. The one
    /// way any annotation changes: every modifier that names, labels, marks or places builds the
    /// record it wants and comes through here. A widening that has to build a new projection rather
    /// than clone one — <c>OrBlank</c>, which changes the result type — hands the new leaf the old
    /// one's record the same way, so a leaf does not silently change its own path by being made
    /// tolerant.
    /// </summary>
    internal ProjectionBase With(Annotations annotations)
    {
      var clone = (ProjectionBase)MemberwiseClone();
      clone.Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
      return clone;
    }


  }

  /// <summary>
  /// The base for every projection that reads a <typeparamref name="TResult"/>. Subclasses must be
  /// immutable field bags: the clone-returning operations copy the projection wholesale, so a
  /// subclass that mutates state after construction breaks the guarantee that one projection can be
  /// applied to many spaces concurrently.
  /// </summary>
  /// <typeparam name="TSpace">The space this projection is written over.</typeparam>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public abstract class ProjectionBase<TSpace, TResult> : ProjectionBase, IProjection<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <inheritdoc cref="ProjectionBase(Placement)"/>
    /// <remarks>
    /// Not <c>protected</c>: only this library may derive. The modifier surface casts its result
    /// back to the receiver's own type on the strength of "every projection is one of ours", and an
    /// externally authored subclass would be the one way to make that cast fail at runtime while
    /// compiling cleanly. Closed at the projection rename (owner decision).
    /// </remarks>
    /// <param name="placement">Where this projection sits within the space it is handed.</param>
    private protected ProjectionBase(Placement placement)
      : base(placement)
    {
    }

    /// <inheritdoc/>
    public abstract ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context);

    /// <inheritdoc/>
    IProjection<TSpace, TResult> IProjection<TSpace, TResult>.With(Annotations annotations)
      => (IProjection<TSpace, TResult>)With(annotations);

    /// <summary>
    /// The same reading, tolerating a blank cell — what <c>OrBlank</c> declares. Only a cell leaf
    /// can: a blank is a value in a reading of one cell, and on anything else it is a declaration
    /// error, which is why refusing here is where that error is raised.
    /// <para>
    /// The widening comes from the caller because C# cannot say "this same reading, of
    /// <typeparamref name="TValue"/>"; at run time it is the identity. Everything else — the
    /// placement, the name, the kind a backend's leaf asserts — is the receiver's own, so
    /// <c>Decimal().Right(6).OrBlank()</c> and <c>Decimal().OrBlank().Right(6)</c> declare the same
    /// thing.
    /// </para>
    /// </summary>
    /// <typeparam name="TValue">The nullable form of <typeparamref name="TResult"/>.</typeparam>
    /// <param name="widen">The widening, which is the identity conversion at run time.</param>
    internal virtual IProjection<TSpace, TValue> Tolerating<TValue>(Func<TResult, TValue> widen)
      => throw new ArgumentException(
        "OrBlank reads a blank cell as null, so it belongs on a cell leaf — AsText, or one of a "
        + $"backend's kinded leaves. {ProjectionContext.Describe(this)} is not one.",
        "projection");
  }
}
