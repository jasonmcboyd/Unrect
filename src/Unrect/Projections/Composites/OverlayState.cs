using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// An overlay in progress. There is no cursor to keep: every child is handed the overlay's whole
  /// extent and finds its own place in it, so all that accumulates is how far out the children
  /// reached.
  /// </summary>
  internal sealed class OverlayState : LayoutState
  {
    private int _width;
    private int _height;

    public OverlayState(IProjection owner, ISpace extent, ProjectionContext context)
      : base(owner, extent, context)
    {
    }

    /// <summary>
    /// The bounding box of where the children landed, measured from the overlay's origin — not the
    /// last child's extent, and not the sum of them.
    /// </summary>
    public override Size Consumed => new Size(_width, _height);

    public override string DeclaredNothing => NothingDeclared("an overlay");

    public override T Next<T>(IProjection<T> projection, string? declared)
    {
      // Children are independent: the same extent and the same unadvanced context every time, so
      // each child's own placement decides where it lands and the engine records its true offset.
      // They may overlap and may read the same cells, because they read rather than paint.
      Admit(projection, default);

      var applied = ProjectionEngine.Apply(projection, Extent, Context.WithUseSite(UseSite.From(declared, Count + 1)));

      _width = Math.Max(_width, applied.Advance.Width);
      _height = Math.Max(_height, applied.Advance.Height);
      Took(applied.Presence);

      return applied.Value;
    }
  }
}
