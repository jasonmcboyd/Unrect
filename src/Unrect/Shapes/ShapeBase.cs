using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The base for every shape, and the seam a modifier dispatches through.
  /// <para>
  /// A modifier keeps whatever demand its receiver carries by handing back the receiver's own type
  /// (<c>TShape Named&lt;TShape&gt;(this TShape, string)</c>), and a method generic in the shape's
  /// type can only see it as an <see cref="IShape"/>. So the operations that need to know the
  /// <em>result</em> type live here, where <see cref="ShapeBase{TResult}"/> supplies it. That is the
  /// whole reason this non-generic half exists, and it is why the modifier surface is written once
  /// instead of once per demand.
  /// </para>
  /// <para>
  /// The clone-returning operations (<see cref="Renamed"/>, <see cref="Replaced"/>) hand back a copy
  /// of the same runtime type, so a modifier's cast back to <c>TShape</c> cannot fail. The
  /// wrapper-returning ones hand back a new shape, which is a <see cref="ShapeBase{TResult}"/> for
  /// the same result type and therefore converts to any <em>interface</em> the receiver was seen
  /// through — but not to a shape class of its own, which is what a modifier's cast checks.
  /// </para>
  /// </summary>
  public abstract class ShapeBase : IShape
  {
    private static readonly IShape[] NoChildren = Array.Empty<IShape>();

    /// <summary>
    /// Fixes where the shape sits. Every shape has a placement from the moment it exists, so the
    /// engine never has to ask whether one was declared.
    /// </summary>
    /// <param name="placement">Where this shape sits within the space it is handed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    private protected ShapeBase(Placement placement)
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
    public virtual IReadOnlyList<IShape> Children => NoChildren;

    /// <summary>Opaque by default; only an unnamed wrapper overrides this to true.</summary>
    public virtual bool IsTransparent => false;

    /// <summary>This shape named <paramref name="name"/> — a copy, of this same type.</summary>
    internal ShapeBase Renamed(string name)
    {
      var clone = Clone();
      clone.Name = name;
      return clone;
    }

    /// <summary>This shape placed by <paramref name="placement"/> — a copy, of this same type.</summary>
    internal ShapeBase Replaced(Placement placement)
    {
      var clone = Clone();
      clone.Placement = placement;
      return clone;
    }

    /// <summary>This shape inset on each side — the wrapper <c>Padded</c> declares.</summary>
    internal abstract IShape Inset(int left, int top, int right, int bottom);

    /// <summary>This shape bounded at <paramref name="landmark"/> — the wrapper <c>Until</c> declares.</summary>
    internal abstract IShape BoundedBy(Landmark landmark, bool orEnd);

    /// <summary>This shape below <paramref name="captions"/> — the flow <c>Under</c> declares.</summary>
    internal abstract IShape Beneath(IShape<string>[] captions);

    /// <summary>
    /// This shape with <paramref name="fallback"/> to stand in for it — the boundary <c>Else</c>
    /// declares. The fallback must read what this shape reads, which the modifier's own type
    /// inference has already required of every caller that named a shape type.
    /// </summary>
    internal abstract IShape Otherwise(IShape fallback, string? declared);

    private ShapeBase Clone() => (ShapeBase)MemberwiseClone();
  }

  /// <summary>
  /// The base for every shape that reads a <typeparamref name="TResult"/>. Subclasses must be
  /// immutable field bags: the clone-returning operations copy the shape wholesale, so a subclass
  /// that mutates state after construction breaks the guarantee that one shape can be applied to
  /// many spaces concurrently.
  /// </summary>
  /// <typeparam name="TResult">What projecting this shape's extent produces.</typeparam>
  public abstract class ShapeBase<TResult> : ShapeBase, IShape<TResult>
  {
    /// <inheritdoc cref="ShapeBase(Placement)"/>
    /// <param name="placement">Where this shape sits within the space it is handed.</param>
    protected ShapeBase(Placement placement)
      : base(placement)
    {
    }

    /// <inheritdoc/>
    public abstract ShapeResult<TResult> Project(ISpace extent, ShapeContext context);

    /// <inheritdoc/>
    public IShape<TResult> WithName(string name)
      => (IShape<TResult>)Renamed(name ?? throw new ArgumentNullException(nameof(name)));

    /// <inheritdoc/>
    public IShape<TResult> WithPlacement(Placement placement)
      => (IShape<TResult>)Replaced(placement ?? throw new ArgumentNullException(nameof(placement)));

    internal sealed override IShape Inset(int left, int top, int right, int bottom)
      => new PadShape<TResult>(this, left, top, right, bottom, Placement.Default);

    /// <summary>
    /// Replaces an existing bound rather than nesting inside it, so <c>Until(A).Until(B)</c> ends at
    /// B: a shape has one end. The axis comes with the landmark, so this is also how a row bound is
    /// replaced by a column one.
    /// </summary>
    internal sealed override IShape BoundedBy(Landmark landmark, bool orEnd)
      => this is UntilShape<TResult> bounded
        ? bounded.WithLandmark(landmark, orEnd)
        : new UntilShape<TResult>(this, landmark, orEnd, Placement.Default);

    internal sealed override IShape Beneath(IShape<string>[] captions)
      => new FlowShape<TResult>(
        Orientation.Vertical,
        cursor =>
        {
          // declared: null at both sites, and it is mandatory. Left to the compiler, the naming
          // ladder would read the argument text from inside HERE and label every caption 'caption'
          // and the section 'shape' — identifiers the user never wrote. Capture reads the immediate
          // call site, so a helper has to opt out.
          foreach (var caption in captions)
            cursor.Next(caption, declared: null);

          return cursor.Next(this, declared: null);
        },
        Placement.Default,
        description: "Under");

    internal sealed override IShape Otherwise(IShape fallback, string? declared)
      => new BoundaryShape<TResult>(
        this,
        fallback as IShape<TResult>
          ?? throw new ArgumentException(
            $"A fallback must read what the shape it stands in for reads ({typeof(TResult).Name}).",
            nameof(fallback)),
        default!,
        Placement.Default,
        "Else",
        UseSite.From(declared, null));
  }
}
