using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A declared region of a space together with the projection that turns it into a value. A shape
  /// says where it sits inside the space it is handed (<see cref="Placement"/>) and what to make of
  /// that region; <see cref="ShapeEngine"/> is the only code that applies the placement.
  /// </summary>
  public interface IShape
  {
    /// <summary>
    /// The explicit name given by <c>.Named</c>, or null. The top rung of the naming ladder: when
    /// set, it is what a failure path and every diagnostic call this shape, ahead of any use-site
    /// label or description.
    /// </summary>
    string? Name { get; }

    /// <summary>The structural fallback name — the factory that produced the shape, e.g. <c>"Column(4)"</c>.</summary>
    string Description { get; }

    /// <summary>Where this shape sits, and how much of its extent it declares, within the space it is handed.</summary>
    Placement Placement { get; }

    /// <summary>The shape's declared children, in declaration order; empty for a leaf.</summary>
    IReadOnlyList<IShape> Children { get; }

    /// <summary>
    /// True only for unnamed wrappers (<c>Select</c>, <c>Padded</c>, <c>Until</c>, and the
    /// <c>Else</c>/<c>Optional</c> boundary), which contribute no segment to a failure path;
    /// naming a wrapper makes it opaque and it claims the segment.
    /// </summary>
    bool IsTransparent { get; }
  }

  /// <summary>
  /// A shape that reads a <typeparamref name="TResult"/> — the form a declaration is written and
  /// applied in. The untyped <see cref="IShape"/> above it is what diagnostics and tooling walk,
  /// where the result type is neither known nor needed.
  /// </summary>
  /// <typeparam name="TResult">What projecting this shape's extent produces.</typeparam>
  public interface IShape<TResult> : IShape
  {
    /// <summary>
    /// Projects the shape's <em>resolved</em> extent: the placement has already been applied, so a
    /// projection can neither observe nor re-apply it.
    /// </summary>
    ShapeResult<TResult> Project(ISpace extent, ShapeContext context);

    /// <summary>A copy of this shape named <paramref name="name"/> — see <see cref="IShape.Name"/>.</summary>
    IShape<TResult> WithName(string name);

    /// <summary>A copy of this shape with <paramref name="placement"/> in place of its own.</summary>
    IShape<TResult> WithPlacement(Placement placement);
  }
}
