using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// The base for every shape. <see cref="WithName"/> and <see cref="WithPlacement"/> clone the
  /// shape, so subclasses must be immutable field bags — a subclass that mutates state after
  /// construction breaks the guarantee that one shape can be applied to many spaces concurrently.
  /// </summary>
  public abstract class ShapeBase<TResult> : IShape<TResult>
  {
    private static readonly IShape[] NoChildren = System.Array.Empty<IShape>();

    /// <summary>
    /// Fixes where the shape sits. Every shape has a placement from the moment it exists, so the
    /// engine never has to ask whether one was declared.
    /// </summary>
    /// <param name="placement">Where this shape sits within the space it is handed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="placement"/> is null.</exception>
    protected ShapeBase(Placement placement)
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

    /// <inheritdoc/>
    public abstract ShapeResult<TResult> Project(ISpace extent, ShapeContext context);

    /// <inheritdoc/>
    public IShape<TResult> WithName(string name)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      var clone = Clone();
      clone.Name = name;
      return clone;
    }

    /// <inheritdoc/>
    public IShape<TResult> WithPlacement(Placement placement)
    {
      if (placement is null)
        throw new ArgumentNullException(nameof(placement));

      var clone = Clone();
      clone.Placement = placement;
      return clone;
    }

    private ShapeBase<TResult> Clone() => (ShapeBase<TResult>)MemberwiseClone();
  }
}
