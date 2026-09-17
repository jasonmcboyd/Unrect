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
  /// cref="ProjectionBase{TSpace, TResult}"/> supplies it. That is the whole reason this non-generic half
  /// exists, and it is why the modifier surface is written once instead of once per demand.
  /// </para>
  /// <para>
  /// The clone-returning operations (<see cref="Renamed"/>, <see cref="Replaced"/>) hand back a
  /// copy of the same runtime type, so a modifier's cast back to <c>TProjection</c> cannot fail.
  /// The wrapper-returning ones hand back a new projection, which is a <see
  /// cref="ProjectionBase{TSpace, TResult}"/> for the same result type and therefore converts to any
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

    /// <summary>A level of the tree by default; only a wrapper overrides this to true.</summary>
    public virtual bool IsWrapper => false;

    /// <summary>Children are the whole truth by default; a layout overrides this to say why they are not.</summary>
    public virtual string? Opacity => null;

    /// <inheritdoc/>
    public string? UnitName { get; private set; }

    /// <inheritdoc/>
    public bool IsScaffolding { get; private set; }

    /// <summary>This projection marked internal plumbing — a copy, of this same type.</summary>
    internal ProjectionBase AsScaffolding()
    {
      var clone = Clone();
      clone.IsScaffolding = true;
      return clone;
    }

    /// <summary>This projection named <paramref name="name"/> — a copy, of this same type.</summary>
    internal ProjectionBase Renamed(string name)
    {
      var clone = Clone();
      clone.Name = name;
      return clone;
    }

    /// <summary>
    /// This projection marked a path boundary with kind label <paramref name="name"/> — a copy, of
    /// this same type. The label is the collapsed path's segment. Independent of
    /// <see cref="Name"/>, so a boundary that is also <c>.Named</c> renders <c>label:name</c>.
    /// </summary>
    internal ProjectionBase AsUnitBoundary(string name)
    {
      var clone = Clone();
      clone.UnitName = name;
      return clone;
    }

    /// <summary>
    /// <paramref name="rewritten"/> given this projection's naming — its <see cref="Name"/> and its
    /// unit marks — and handed back for the caller to return.
    /// <para>
    /// The clone-returning modifiers keep all of it for free. A widening that has to build a new
    /// projection rather than clone one — <c>OrBlank</c>, which changes the result type — is the one
    /// place it has to be carried across by hand, and a leaf that carried only the name would
    /// silently change its own path by being made tolerant. Mutation is safe here and nowhere else:
    /// <paramref name="rewritten"/> was constructed by the caller one line ago and has not escaped.
    /// </para>
    /// </summary>
    internal TProjection Naming<TProjection>(TProjection rewritten)
      where TProjection : ProjectionBase
    {
      rewritten.Name = Name;
      rewritten.UnitName = UnitName;
      rewritten.IsScaffolding = IsScaffolding;

      return rewritten;
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

    /// <summary>
    /// This projection below <paramref name="captions"/>, as one vertical flow — the structure a
    /// <c>Heading</c> stage builds directly. The captions are the heading rows, read and discarded;
    /// this projection is the section they announce.
    /// </summary>
    internal abstract IProjection WithHeadings(IProjection[] captions);

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
    public IProjection<TSpace, TResult> WithName(string name)
      => (IProjection<TSpace, TResult>)Renamed(name ?? throw new ArgumentNullException(nameof(name)));

    /// <inheritdoc/>
    public IProjection<TSpace, TResult> WithPlacement(Placement placement)
      => (IProjection<TSpace, TResult>)Replaced(placement ?? throw new ArgumentNullException(nameof(placement)));

    internal sealed override IProjection Inset(int left, int top, int right, int bottom)
      => new PadProjection<TSpace, TResult>(this, left, top, right, bottom, Placement.Default);

    /// <summary>
    /// Refuses a second end rather than replacing the first, so <c>Until(A).Until(B)</c> is not a
    /// declaration at all: a projection has one end, and the axis comes with the landmark, so a
    /// column bound over a row bound is a second end too. A wrapper in between makes the outer bound
    /// nest, which is a different declaration and a legal one.
    /// </summary>
    internal sealed override IProjection BoundedBy(Landmark landmark, bool orEnd)
    {
      if (this is UntilProjection<TSpace, TResult> bounded)
        throw bounded.AlreadyEnded(landmark);

      return new UntilProjection<TSpace, TResult>(this, landmark, orEnd, Placement.Default);
    }

    internal sealed override IProjection WithHeadings(IProjection[] captions)
      => new FlowProjection<TSpace, TResult>(
        Orientation.Vertical,
        cursor =>
        {
          // declared: null at both sites, and it is mandatory. Left to the compiler, the naming
          // ladder would read the argument text from inside HERE and label every caption 'caption'
          // and the section 'projection' — identifiers the user never wrote. Capture reads the
          // immediate call site, so a helper has to opt out.
          foreach (var caption in captions)
            cursor.Next((IProjection<TSpace, string>)caption, declared: null);

          return cursor.Next(this, declared: null);
        },
        Placement.Default,
        description: "Heading");

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

    internal sealed override IProjection Otherwise(IProjection fallback, string? declared)
      => new BoundaryProjection<TSpace, TResult>(
        this,
        fallback as IProjection<TSpace, TResult>
          ?? throw new ArgumentException(
            $"A fallback must read what the projection it stands in for reads ({typeof(TResult).Name}).",
            nameof(fallback)),
        default!,
        Placement.Default,
        "Else",
        UseSite.From(declared, null));
  }
}
