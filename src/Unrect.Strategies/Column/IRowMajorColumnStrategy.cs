using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A column strategy whose answer is built up a row at a time, and so can share one forward walk
  /// with a row rule instead of running as a second pass over the band the row rule found.
  /// <see cref="IColumnStrategy.SelectColumns"/> is <em>defined</em> as
  /// <c>ColumnAccumulators.Fold(BeginColumns(space.Area.Width), space)</c>, which is how an
  /// implementation is expected to spell it, so that it states its rule once. The definition is a
  /// convention rather than an inherited body for the reason given on
  /// <see cref="IIncrementalRowStrategy"/>: netstandard2.0 has no default interface members.
  /// </summary>
  internal interface IRowMajorColumnStrategy : IColumnStrategy
  {
    /// <summary>
    /// An accumulator over an extent <paramref name="width"/> columns wide, positioned before row 0.
    /// The width is given rather than read from a space because the caller may be discovering the
    /// height of the very extent being measured, and an <see cref="Area"/> is one struct.
    /// </summary>
    IColumnAccumulator BeginColumns(int width);
  }
}
