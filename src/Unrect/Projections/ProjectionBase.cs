using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The base for every projection, and the seam a modifier dispatches through.
  /// <para>
  /// A modifier keeps whatever demand its receiver carries by handing back the receiver's own type
  /// (<c>TProjection Named&lt;TProjection&gt;(this TProjection, string)</c>), and a method generic
  /// in the projection's type can only see it as an <see cref="IProjection"/>. So the operations
  /// that need to know the <em>result</em> type live here, where <see
  /// cref="ProjectionBase{TResult}"/> supplies it. That is the whole reason this non-generic half
  /// exists, and it is why the modifier surface is written once instead of once per demand.
  /// </para>
  /// <para>
  /// The clone-returning operations (<see cref="Renamed"/>, <see cref="Replaced"/>) hand back a
  /// copy of the same runtime type, so a modifier's cast back to <c>TProjection</c> cannot fail.
  /// The wrapper-returning ones hand back a new projection, which is a <see
  /// cref="ProjectionBase{TResult}"/> for the same result type and therefore converts to any
  /// <em>interface</em> the receiver was seen through — but not to a projection class of its own,
  /// which is what a modifier's cast checks.
  /// </para>
  /// </summary>
  public abstract class ProjectionBase : IProjection
  {
    private static readonly IProjection[] NoChildren = Array.Empty<IProjection>();

    /// <summary>
    /// Fixes where the projection sits. Every projection has a placement from the moment it exists,
    /// so the engine never has to ask whether one was declared.
    /// </summary>
    /// <param name="placement">Where this projection sits within the space it is handed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    private protected ProjectionBase(Placement placement)
    {
      Placement = placement ?? throw new ArgumentNullException(nameof(placement));
    }

    /// <inheritdoc/>
    public string? Name { get; private set; }

    /// <inheritdoc/>
    public Placement Placement { get; private set; }

    /// <inheritdoc/>
    public abstract string Description { get; }

    /// <summary>No children by default; a composite overrides this to declare its own.</summary>
    public virtual IReadOnlyList<IProjection> Children => NoChildren;

    /// <summary>Opaque by default; only an unnamed wrapper overrides this to true.</summary>
    public virtual bool IsTransparent => false;

    /// <summary>This projection named <paramref name="name"/> — a copy, of this same type.</summary>
    internal ProjectionBase Renamed(string name)
    {
      var clone = Clone();
      clone.Name = name;
      return clone;
    }

    /// <summary>This projection placed by <paramref name="placement"/> — a copy, of this same type.</summary>
    internal ProjectionBase Replaced(Placement placement)
    {
      var clone = Clone();
      clone.Placement = placement;
      return clone;
    }

    /// <summary>This projection inset on each side — the wrapper <c>Padded</c> declares.</summary>
    internal abstract IProjection Inset(int left, int top, int right, int bottom);

    /// <summary>This projection bounded at <paramref name="landmark"/> — the wrapper <c>Until</c> declares.</summary>
    internal abstract IProjection BoundedBy(Landmark landmark, bool orEnd);

    /// <summary>This projection below <paramref name="captions"/> — the flow <c>Under</c> declares.</summary>
    internal abstract IProjection Beneath(IProjection<string>[] captions);

    /// <summary>
    /// This projection with <paramref name="fallback"/> to stand in for it — the boundary
    /// <c>Else</c> declares. The fallback must read what this projection reads, which the
    /// modifier's own type inference has already required of every caller that named a projection
    /// type.
    /// </summary>
    internal abstract IProjection Otherwise(IProjection fallback, string? declared);

    private ProjectionBase Clone() => (ProjectionBase)MemberwiseClone();
  }

  /// <summary>
  /// The base for every projection that reads a <typeparamref name="TResult"/>. Subclasses must be
  /// immutable field bags: the clone-returning operations copy the projection wholesale, so a
  /// subclass that mutates state after construction breaks the guarantee that one projection can be
  /// applied to many spaces concurrently.
  /// </summary>
  /// <typeparam name="TResult">What projecting this projection's extent produces.</typeparam>
  public abstract class ProjectionBase<TResult> : ProjectionBase, IProjection<TResult>
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
    public abstract ProjectionResult<TResult> Project(ISpace extent, ProjectionContext context);

    /// <inheritdoc/>
    public IProjection<TResult> WithName(string name)
      => (IProjection<TResult>)Renamed(name ?? throw new ArgumentNullException(nameof(name)));

    /// <inheritdoc/>
    public IProjection<TResult> WithPlacement(Placement placement)
      => (IProjection<TResult>)Replaced(placement ?? throw new ArgumentNullException(nameof(placement)));

    internal sealed override IProjection Inset(int left, int top, int right, int bottom)
      => new PadProjection<TResult>(this, left, top, right, bottom, Placement.Default);

    /// <summary>
    /// Replaces an existing bound rather than nesting inside it, so <c>Until(A).Until(B)</c> ends
    /// at B: a projection has one end. The axis comes with the landmark, so this is also how a row
    /// bound is replaced by a column one.
    /// </summary>
    internal sealed override IProjection BoundedBy(Landmark landmark, bool orEnd)
      => this is UntilProjection<TResult> bounded
        ? bounded.WithLandmark(landmark, orEnd)
        : new UntilProjection<TResult>(this, landmark, orEnd, Placement.Default);

    internal sealed override IProjection Beneath(IProjection<string>[] captions)
      => new FlowProjection<TResult>(
        Orientation.Vertical,
        cursor =>
        {
          // declared: null at both sites, and it is mandatory. Left to the compiler, the naming
          // ladder would read the argument text from inside HERE and label every caption 'caption'
          // and the section 'projection' — identifiers the user never wrote. Capture reads the
          // immediate call site, so a helper has to opt out.
          foreach (var caption in captions)
            cursor.Next(caption, declared: null);

          return cursor.Next(this, declared: null);
        },
        Placement.Default,
        description: "Under");

    internal sealed override IProjection Otherwise(IProjection fallback, string? declared)
      => new BoundaryProjection<TResult>(
        this,
        fallback as IProjection<TResult>
          ?? throw new ArgumentException(
            $"A fallback must read what the projection it stands in for reads ({typeof(TResult).Name}).",
            nameof(fallback)),
        default!,
        Placement.Default,
        "Else",
        UseSite.From(declared, null));
  }
}
