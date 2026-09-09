using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A tolerance boundary: it applies the projection it wraps and, if anything inside fails,
  /// absorbs the failure, records a <see cref="DiagnosticSeverity.Warning"/> describing what
  /// actually went wrong, and supplies a filler — either a fallback projection or a constant value.
  /// <para>
  /// It behaves like a catch block. Its own placement is resolved before it can catch anything, so
  /// tolerance goes innermost: <c>x.On(anchor).Optional()</c> absorbs a missing anchor, while
  /// <c>x.Optional().On(anchor)</c> does not.
  /// </para>
  /// </summary>
  internal sealed class BoundaryProjection<T> : ProjectionBase<T>
  {
    public BoundaryProjection(
      IProjection<T> inner,
      IProjection<T>? fallback,
      T fallbackValue,
      Placement placement,
      string description,
      UseSite fallbackSite = default)
      : base(placement)
    {
      Inner = inner ?? throw new ArgumentNullException(nameof(inner));
      Fallback = fallback;
      FallbackValue = fallbackValue;
      Description = description;
      FallbackSite = fallbackSite;
      Children = fallback is null ? new IProjection[] { inner } : new IProjection[] { inner, fallback };
    }

    private IProjection<T> Inner { get; }
    private IProjection<T>? Fallback { get; }
    private T FallbackValue { get; }

    /// <summary>What the declaration called the fallback, so a stand-in names itself as written.</summary>
    private UseSite FallbackSite { get; }

    public override string Description { get; }

    public override IReadOnlyList<IProjection> Children { get; }

    public override bool IsTransparent => Name is null;

    public override ProjectionResult<T> Project(ISpace extent, ProjectionContext context)
    {
      var mark = context.Diagnostics.Mark();

      try
      {
        var applied = ProjectionEngine.Apply(Inner, extent, context);
        return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
      }
      // A projection that broke rather than disagreed is a bug in the reading code, not a
      // projection of data to tolerate, so it passes straight through with its location intact.
      catch (ProjectionException failure) when (!failure.IsFault)
      {
        // Whatever the failed attempt tolerated along the way goes with it; what replaces it all is
        // the one warning saying which projection failed, where, and why.
        context.Diagnostics.Rollback(mark);
        context.Report(DiagnosticSeverity.Warning, failure);

        if (Fallback is null)
          // Nothing was read, so the boundary itself consumes nothing and a following sibling
          // starts where this began — unless the placement declared an area or a padding, which is
          // consumed in full whatever the projection made of it. Absorbed therefore travels with a
          // non-zero extent perfectly legitimately, and that is exactly why it is carried beside
          // the number rather than inferred from it: the extent says how much room the declaration
          // claimed, the presence says nobody looked inside it.
          return new ProjectionResult<T>(FallbackValue, new Size(0, 0), Presence.Absorbed);

        try
        {
          // A fallback that ran is a projection like any other, so the presence is its own: what
          // stood in was read, or was itself empty, and the boundary has no better answer.
          var applied = ProjectionEngine.Apply(Fallback, extent, context.WithUseSite(FallbackSite));
          return new ProjectionResult<T>(applied.Value, applied.Advance, applied.Presence);
        }
        catch (ProjectionException fallbackFailure)
        {
          // Losing the primary failure here would hide the interesting half of the story.
          throw fallbackFailure.WithNote(
            $"it stands in for {failure.Subject}, which failed too: {failure.Problem}");
        }
      }
    }
  }
}
