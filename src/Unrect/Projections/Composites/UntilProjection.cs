using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// Bounds its extent at a landmark and applies the inner projection to what comes before it.
  /// Where an offset says where a projection starts by content, this says where it ends by content.
  /// <para>
  /// It is a wrapper projection rather than an area strategy for one decisive reason: a strategy
  /// has no context, so it could not record the <c>Info</c> that <c>orEnd</c> owes the caller. The
  /// arithmetic underneath is the same "rows up to the landmark by full width" a strategy would do.
  /// </para>
  /// </summary>
  internal sealed class UntilProjection<TResult> : ProjectionBase<TResult>
  {
    public UntilProjection(IProjection<TResult> inner, Landmark landmark, bool orEnd, Placement placement)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Landmark = landmark ?? throw new ArgumentNullException(nameof(landmark));
      OrEnd = orEnd;
      Children = new IProjection[] { inner };
    }

    private IProjection<TResult> Inner { get; }
    private Landmark Landmark { get; set; }
    private bool OrEnd { get; set; }

    private bool IsVertical => Landmark.Orientation == Orientation.Vertical;

    public override string Description => IsVertical ? "Until" : "UntilColumn";

    public override IReadOnlyList<IProjection> Children { get; }

    /// <summary>Like a pad: a bound the user wrote as part of a projection is not a level of the tree.</summary>
    public override bool IsTransparent => Name is null;

    /// <summary>
    /// Replaces the landmark rather than nesting, so <c>Until(A).Until(B)</c> ends at B: a
    /// projection has one end. The axis comes with the landmark, so this is also how a row bound is
    /// replaced by a column one.
    /// </summary>
    public IProjection<TResult> WithLandmark(Landmark landmark, bool orEnd)
    {
      // Cloned rather than rebuilt so the name and placement already on this wrapper survive, the
      // same way ProjectionBase clones itself for Named and Sized.
      var clone = (UntilProjection<TResult>)MemberwiseClone();

      clone.Landmark = landmark;
      clone.OrEnd = orEnd;

      return clone;
    }

    public override ProjectionResult<TResult> Project(ISpace extent, ProjectionContext context)
    {
      var size = extent.Area.Size;
      var found = Landmark.Find(extent);
      var limit = found ?? (IsVertical ? size.Height : size.Width);

      // A missing end is a disagreement about the shape of the data, not a bug in the reading code,
      // so it is absorbable — and it is blamed on the projection being bounded, because "Until" is
      // not what the user was looking for.
      if (found is null && !OrEnd)
        throw context.Failure(ProjectionContext.Through(this), $"{Landmark.Description} exists to end this projection", extent, null, null);

      // Declared alternation rather than tolerance after a failure, so Info rather than Warning.
      if (found is null)
        context.Report(DiagnosticSeverity.Info, this, $"{Landmark.Description} exists to end this projection, so it ran to the end of the space", extent);

      var applied = ProjectionEngine.Apply(Inner, extent.GetSubspace(Bound(limit, size)), context);

      // The bound is consumed whether or not the inner projection used it all, exactly as a
      // declared area is: that is what puts the next sibling ON the landmark rather than somewhere
      // before it. Across the axis, only what the inner projection reached — bounding rows must not
      // claim columns.
      return new ProjectionResult<TResult>(applied.Value, Consumed(limit, applied.Advance));
    }

    private Area Bound(int limit, Size size)
      => IsVertical ? new Area(size.Width, limit) : new Area(limit, size.Height);

    private Size Consumed(int limit, Size advance)
      => IsVertical ? new Size(advance.Width, limit) : new Size(limit, advance.Height);
  }
}
