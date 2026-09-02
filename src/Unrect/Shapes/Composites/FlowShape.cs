using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A flow: children laid out one after another along an axis, each starting where the one before
  /// it left off, so the space is divided into bands nobody shares.
  /// </summary>
  internal sealed class FlowShape<T> : LayoutShape<T>
  {
    public FlowShape(Orientation orientation, Layout<T> build, Placement placement, string? description = null)
      : base(build, placement)
    {
      Orientation = orientation;
      Declared = description;
    }

    private Orientation Orientation { get; }

    /// <summary>
    /// What a factory that desugars into a flow calls itself. <c>Under</c> is one: a segment reading
    /// <c>VerticalFlow</c> could not be grepped back to the <c>.Under(…)</c> that produced it.
    /// </summary>
    private string? Declared { get; }

    // A path segment names the factory the user typed, so it can be grepped back to the line.
    public override string Description
      => Declared ?? (Orientation == Orientation.Vertical ? "VerticalFlow" : "HorizontalFlow");

    protected override LayoutState NewState(ISpace extent, ShapeContext context)
      => new FlowState(this, Orientation, extent, context);
  }
}
