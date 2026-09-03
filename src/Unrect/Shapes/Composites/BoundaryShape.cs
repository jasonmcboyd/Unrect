using System;
using System.Collections.Generic;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A tolerance boundary: it applies the shape it wraps and, if anything inside fails, absorbs the
  /// failure, records a <see cref="DiagnosticSeverity.Warning"/> describing what actually went
  /// wrong, and supplies a filler — either a fallback shape or a constant value.
  /// <para>
  /// It behaves like a catch block. Its own placement is resolved before it can catch anything, so
  /// tolerance goes innermost: <c>x.After(anchor).Optional()</c> absorbs a missing anchor, while
  /// <c>x.Optional().After(anchor)</c> does not.
  /// </para>
  /// </summary>
  internal sealed class BoundaryShape<T> : ShapeBase<T>
  {
    public BoundaryShape(
      IShape<T> inner,
      IShape<T>? fallback,
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
      Children = fallback is null ? new IShape[] { inner } : new IShape[] { inner, fallback };
    }

    private IShape<T> Inner { get; }
    private IShape<T>? Fallback { get; }
    private T FallbackValue { get; }

    /// <summary>What the declaration called the fallback, so a stand-in names itself as written.</summary>
    private UseSite FallbackSite { get; }

    public override string Description { get; }

    public override IReadOnlyList<IShape> Children { get; }

    public override bool IsTransparent => Name is null;

    public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
    {
      var mark = context.Diagnostics.Mark();

      try
      {
        var applied = ShapeEngine.Apply(Inner, extent, context);
        return new ShapeResult<T>(applied.Value, applied.Advance);
      }
      // A projection that broke rather than disagreed is a bug in the reading code, not a shape of
      // data to tolerate, so it passes straight through with its location intact.
      catch (ShapeException failure) when (!failure.IsFault)
      {
        // Whatever the failed attempt tolerated along the way goes with it; what replaces it all is
        // the one warning saying which shape failed, where, and why.
        context.Diagnostics.Rollback(mark);
        context.Report(DiagnosticSeverity.Warning, failure);

        if (Fallback is null)
          // Nothing was read, so nothing was consumed: a following sibling starts where this began.
          return new ShapeResult<T>(FallbackValue, new Size(0, 0));

        try
        {
          var applied = ShapeEngine.Apply(Fallback, extent, context.WithUseSite(FallbackSite));
          return new ShapeResult<T>(applied.Value, applied.Advance);
        }
        catch (ShapeException fallbackFailure)
        {
          // Losing the primary failure here would hide the interesting half of the story.
          throw fallbackFailure.WithNote(
            $"it stands in for {failure.Subject}, which failed too: {failure.Problem}");
        }
      }
    }
  }
}
