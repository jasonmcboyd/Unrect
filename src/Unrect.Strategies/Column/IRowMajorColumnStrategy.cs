using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// A column strategy whose answer is built up a row at a time, and so can share one forward walk
  /// with a row rule instead of running as a second pass over the band the row rule found.
  /// <see cref="IColumnStrategy.SelectColumns"/> is defined here as the fold of
  /// <see cref="BeginColumns"/>: a strategy states its rule once, and the eager reading agrees by
  /// construction rather than by agreement.
  /// </summary>
  internal interface IRowMajorColumnStrategy : IColumnStrategy
  {
    /// <summary>
    /// An accumulator over an extent <paramref name="width"/> columns wide, positioned before row 0.
    /// The width is given rather than read from a space because the caller may be discovering the
    /// height of the very extent being measured, and an <see cref="Area"/> is one struct.
    /// </summary>
    IColumnAccumulator BeginColumns(int width);

    /// <inheritdoc />
    int IColumnStrategy.SelectColumns(ISpace space)
    {
      var accumulator = BeginColumns(space.Area.Width);

      for (var row = 0; !accumulator.IsSettled && row < space.Area.Height; row++)
        accumulator.Include(space, row);

      return accumulator.Count;
    }
  }
}
