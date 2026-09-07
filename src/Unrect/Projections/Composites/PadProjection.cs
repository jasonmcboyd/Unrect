using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Insets its extent and applies the inner projection to what is left, then reports the whole
  /// thing as consumed. Padding shrinks the inside; an offset shifts the outside — which is why
  /// this is a wrapper projection rather than a placement, and why the two compose without
  /// interfering.
  /// </summary>
  internal sealed class PadProjection<TResult> : ProjectionBase<TResult>
  {
    public PadProjection(IProjection<TResult> inner, int left, int top, int right, int bottom, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Left = left;
      Top = top;
      Right = right;
      Bottom = bottom;
      Children = new IProjection[] { inner };
    }

    private IProjection<TResult> Inner { get; }
    private int Left { get; }
    private int Top { get; }
    private int Right { get; }
    private int Bottom { get; }

    public override string Description => "Padded";

    public override IReadOnlyList<IProjection> Children { get; }

    public override bool IsTransparent => Name is null;

    public override ProjectionResult<TResult> Project(ISpace extent, ProjectionContext context)
    {
      var size = extent.Area.Size;
      var width = size.Width - Left - Right;
      var height = size.Height - Top - Bottom;

      // Blames itself explicitly: an unnamed pad is transparent, so the context belongs to its
      // parent and would otherwise take the blame for the padding.
      if (width < 0 || height < 0)
        throw context.Failure(
          this,
          $"a padding of {Left} left, {Top} top, {Right} right, {Bottom} bottom does not fit an extent of {size.Width}x{size.Height}",
          extent,
          null,
          null);

      // The context advances with the subspace so error locations inside the padding stay absolute
      // — transparency in the path must not mean absence from the coordinate arithmetic.
      var applied = ProjectionEngine.Apply(
        Inner,
        extent.GetSubspace(new Offset(Left, Top), new Area(width, height)),
        context.Advance(new Offset(Left, Top)));

      return new ProjectionResult<TResult>(
        applied.Value,
        applied.Advance + new Size(Left + Right, Top + Bottom));
    }
  }
}
