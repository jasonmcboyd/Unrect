using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// One cell, read as what it says. The total canonical leaf: every space renders every cell, so
  /// the only thing that can go wrong is that there is nothing there — which
  /// <c>OrBlank</c> turns from a failure into a null.
  /// </summary>
  /// <typeparam name="TSpace">The space the leaf is declared over.</typeparam>
  /// <typeparam name="TResult">What the leaf hands back — <c>string</c>, or <c>string?</c> once it tolerates a blank.</typeparam>
  internal sealed class TextProjection<TSpace, TResult> : ProjectionBase<TSpace, TResult>
    where TSpace : class, ISpace
  {
    internal TextProjection(Placement placement, bool blankIsNull, Func<string, TResult> read)
      : base(placement)
    {
      BlankIsNull = blankIsNull;
      Read = read;
    }

    /// <summary>
    /// Whether a blank cell reads as null instead of failing — what <c>OrBlank</c> declares, and
    /// quietly: the declaration said this cell may be absent, so its absence is the answer rather
    /// than something to report.
    /// </summary>
    private bool BlankIsNull { get; }

    private Func<string, TResult> Read { get; }

    public override string Description => BlankIsNull ? "AsText?" : "AsText";

    public override ProjectionResult<TResult> Project(Plane<TSpace> extent, ProjectionContext context)
    {
      var size = extent.Area.Size;

      if (size.Width != 1 || size.Height != 1)
        throw context.Failure($"an AsText must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

      var text = extent[0, 0].AsText();

      if (text is null)
        return BlankIsNull
          ? new ProjectionResult<TResult>(default!, size)
          : throw context.Failure($"expected a value at {context.Locate(extent).A1}, found a blank cell", extent);

      return new ProjectionResult<TResult>(Read(text), size);
    }

    internal override IProjection<TSpace, TValue> Tolerating<TValue>(Func<TResult, TValue> widen)
      => (IProjection<TSpace, TValue>)new TextProjection<TSpace, TValue>(Placement, blankIsNull: true, text => widen(Read(text))).With(Annotations);
  }
}
