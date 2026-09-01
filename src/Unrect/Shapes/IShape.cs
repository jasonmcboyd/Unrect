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
    string? Name { get; }
    string Description { get; }
    Placement Placement { get; }
    IReadOnlyList<IShape> Children { get; }

    /// <summary>
    /// True only for unnamed wrappers (<c>Select</c>, <c>Padded</c>, <c>Until</c>, and the
    /// <c>Else</c>/<c>Optional</c> boundary), which contribute no segment to a failure path;
    /// naming a wrapper makes it opaque and it claims the segment.
    /// </summary>
    bool IsTransparent { get; }
  }

  public interface IShape<TResult> : IShape
  {
    /// <summary>
    /// Projects the shape's <em>resolved</em> extent: the placement has already been applied, so a
    /// projection can neither observe nor re-apply it.
    /// </summary>
    ShapeResult<TResult> Project(ISpace extent, ShapeContext context);

    IShape<TResult> WithName(string name);
    IShape<TResult> WithPlacement(Placement placement);
  }
}
