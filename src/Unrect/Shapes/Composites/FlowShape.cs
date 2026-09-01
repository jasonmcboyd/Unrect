using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A flow: children laid out one after another along an axis, each starting where the one before
  /// it left off, so the space is divided into bands nobody shares.
  /// </summary>
  internal sealed class FlowShape<T> : LayoutShape<T>
  {
    public FlowShape(Orientation orientation, Layout<T> build, Placement placement)
      : base(build, placement)
    {
      Orientation = orientation;
    }

    private Orientation Orientation { get; }

    // A path segment names the factory the user typed, so it can be grepped back to the line.
    public override string Description => Orientation == Orientation.Vertical ? "VerticalFlow" : "HorizontalFlow";

    protected override LayoutState NewState(ISpace extent, ShapeContext context)
      => new FlowState(this, Orientation, extent, context);
  }
}
