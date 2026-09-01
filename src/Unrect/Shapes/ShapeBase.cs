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

    protected ShapeBase(Placement placement)
    {
      Placement = placement ?? throw new ArgumentNullException(nameof(placement));
    }

    public string? Name { get; private set; }
    public Placement Placement { get; private set; }

    public abstract string Description { get; }
    public virtual IReadOnlyList<IShape> Children => NoChildren;
    public virtual bool IsTransparent => false;

    public abstract ShapeResult<TResult> Project(ISpace extent, ShapeContext context);

    public ShapeResult<object?> ProjectUntyped(ISpace extent, ShapeContext context)
    {
      var result = Project(extent, context);
      return new ShapeResult<object?>(result.Value, result.Consumed);
    }

    public IShape<TResult> WithName(string name)
    {
      if (name is null)
        throw new ArgumentNullException(nameof(name));

      var clone = Clone();
      clone.Name = name;
      return clone;
    }

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
