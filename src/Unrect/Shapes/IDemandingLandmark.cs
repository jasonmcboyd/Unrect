using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// EXPERIMENT (typed-spaces): a row matcher that can only look at a space offering at least
  /// <typeparamref name="TSpace"/> — <c>RowWithFormula()</c> and its kind.
  /// <para>
  /// It deliberately does <em>not</em> derive from <see cref="IRowLandmark"/>. If it did, every plain
  /// lift (<c>On</c>, <c>Below</c>, <c>Until</c>) would still accept it and the demand would be lost
  /// at the one site the experiment exists to protect; keeping the two families apart is what lets
  /// <c>shape.On(RowWithFormula())</c> <em>infer</em> the demand and hand back a demanding shape with
  /// nothing annotated. The lowering is how the strategy calculus, which knows nothing of
  /// capabilities, still runs it.
  /// </para>
  /// <para>
  /// A matcher only locates; a lift decides what absence means. Absence of the <em>capability</em> is
  /// a different thing from absence of a match, and the whole point of the typed layer is that the
  /// pairing which would produce it no longer compiles.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space this matcher must be able to look at.</typeparam>
  public interface IRowLandmark<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The matcher as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IRowLandmark Landmark { get; }
  }

  /// <summary>
  /// EXPERIMENT (typed-spaces): the column twin of <see cref="IRowLandmark{TSpace}"/>.
  /// </summary>
  /// <typeparam name="TSpace">The space this matcher must be able to look at.</typeparam>
  public interface IColumnLandmark<in TSpace>
    where TSpace : class, ISpace
  {
    /// <summary>The matcher as the strategy calculus takes it, its demand discharged by the lift.</summary>
    IColumnLandmark Landmark { get; }
  }
}
