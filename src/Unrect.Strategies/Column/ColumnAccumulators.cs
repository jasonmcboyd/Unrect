using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The column side's definitional fold, the counterpart of <see cref="Scans"/>: what an
  /// <see cref="IRowMajorColumnStrategy"/>'s eager reading means, written once so each implementation
  /// can say it in a line rather than in a loop of its own.
  /// </summary>
  internal static class ColumnAccumulators
  {
    /// <summary>
    /// How many columns <paramref name="accumulator"/> selects over <paramref name="space"/>: the
    /// rows taken into account in order, stopping as soon as the answer is settled or the rows run
    /// out.
    /// </summary>
    internal static int Fold(IColumnAccumulator accumulator, ISpace space)
    {
      for (var row = 0; !accumulator.IsSettled && row < space.Area.Height; row++)
        accumulator.Include(space, row);

      return accumulator.Count;
    }
  }
}
