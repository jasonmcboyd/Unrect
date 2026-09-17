using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A flow: children laid out one after another along an axis, each starting where the one before
  /// it left off, so the space is divided into bands nobody shares.
  /// </summary>
  internal sealed class FlowDefinition<TSpace, T> : LayoutDefinition<TSpace, T>
    where TSpace : class, ISpace
  {
    public FlowDefinition(Orientation orientation, Layout<TSpace, T> layout, Placement placement, string? description = null)
      : base(layout, placement)
    {
      Orientation = orientation;
      Declared = description;
    }

    private Orientation Orientation { get; }

    /// <summary>
    /// What a factory that desugars into a flow calls itself. A <c>Heading</c> stage is one: a
    /// segment reading <c>VerticalFlow</c> could not be grepped back to the <c>Heading(…)</c> that
    /// produced it.
    /// </summary>
    private string? Declared { get; }

    // A path segment names the factory the user typed, so it can be grepped back to the line.
    public override string Description
      => Declared ?? (Orientation == Orientation.Vertical ? "VerticalFlow" : "HorizontalFlow");

    protected override LayoutState<TSpace> NewState(Plane<TSpace> extent, ProjectionContext context)
      => new FlowState<TSpace>(Orientation, extent, context);
  }
}
