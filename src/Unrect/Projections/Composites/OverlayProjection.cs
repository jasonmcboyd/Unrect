using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// An overlay: one extent shared by every child, each finding its own place inside it, with no
  /// cursor between them. Where a flow divides the space into bands, this hands the whole of it to
  /// everyone, so children may overlap and may read the same cells.
  /// </summary>
  internal sealed class OverlayProjection<TSpace, T> : LayoutProjection<TSpace, T>
    where TSpace : class, ISpace
  {
    public OverlayProjection(Layout<TSpace, T> build, Placement placement)
      : base(build, placement)
    {
    }

    public override string Description => "Overlay";

    protected override LayoutState<TSpace> NewState(Plane<TSpace> extent, ProjectionContext context)
      => new OverlayState<TSpace>(this, extent, context);
  }
}
