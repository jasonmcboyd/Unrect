using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// An overlay: one extent shared by every child, each finding its own place inside it, with no
  /// cursor between them. Where a flow divides the space into bands, this hands the whole of it to
  /// everyone, so children may overlap and may read the same cells.
  /// </summary>
  internal sealed class OverlayShape<T> : LayoutShape<T>
  {
    public OverlayShape(Layout<T> build, Placement placement)
      : base(build, placement)
    {
    }

    public override string Description => "Overlay";

    protected override LayoutState NewState(ISpace extent, ShapeContext context)
      => new OverlayState(this, extent, context);
  }
}
