using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// A line matcher — a row's or a column's, the landmark says which — that can only look at a
  /// space offering at least <typeparamref name="TSpace"/>: <c>RowWithFormula()</c> and its kind.
  /// <para>
  /// It deliberately does <em>not</em> derive from <see cref="ILineLandmark"/>. If it did, every
  /// plain lift (<c>On</c>, <c>Below</c>, <c>Until</c>) would still accept it and the demand would
  /// be lost at the one site the typed layer exists to protect; keeping the two families apart is
  /// what lets <c>projection.On(RowWithFormula())</c> <em>infer</em> the demand and hand back a
  /// demanding projection with nothing annotated. The lowering is how the strategy calculus, which
  /// knows nothing of capabilities, still runs it.
  /// </para>
  /// <para>
  /// A matcher only locates; a lift decides what absence means. Absence of the <em>capability</em>
  /// is a different thing from absence of a match, and the whole point of the typed layer is that
  /// the pairing which would produce it no longer compiles.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this matcher must be able to look at.</typeparam>
  public interface ILineLandmark<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The matcher as the strategy calculus takes it, its demand discharged by the lift.</summary>
    ILineLandmark Landmark { get; }
  }

  internal static partial class Demanding
  {
    internal static ILineLandmark<TSpace> Landmark<TSpace>(ILineLandmark landmark)
      where TSpace : class, ISpace
      => new DemandedLandmark<TSpace>(landmark);

    private sealed class DemandedLandmark<TSpace> : ILineLandmark<TSpace>
      where TSpace : class, ISpace
    {
      internal DemandedLandmark(ILineLandmark landmark) => Landmark = landmark;

      public ILineLandmark Landmark { get; }
    }
  }
}
