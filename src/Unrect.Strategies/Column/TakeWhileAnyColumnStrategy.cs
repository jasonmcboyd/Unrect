using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The leading columns in which some cell satisfies the predicate. Asked column by column it
  /// reads the column; asked a row at a time it accumulates, and settles once every column has
  /// matched.
  /// </summary>
  internal sealed class TakeWhileAnyColumnStrategy : IRowMajorColumnStrategy
  {
    public TakeWhileAnyColumnStrategy(Func<Point<ISpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    internal Func<Point<ISpace>, bool> Predicate { get; }

    public IColumnAccumulator BeginColumns(int width) => new Accumulator(Predicate, width);

    public IColumnScan Begin() => new Scanning.RowMajorColumns(this, (space, column) =>
    {
      for (var row = 0; space.HasRow(row); row++)
        if (Predicate(space[column, row]))
          return true;

      return false;
    });

    private sealed class Accumulator : IColumnAccumulator
    {
      private readonly bool[] _matched;

      public Accumulator(Func<Point<ISpace>, bool> predicate, int width)
      {
        Predicate = predicate;
        _matched = new bool[width];
      }

      public int Count { get; private set; }

      public bool IsSettled => Count == _matched.Length;

      private Func<Point<ISpace>, bool> Predicate { get; }

      public void Include(Plane<ISpace> space, int row)
      {
        for (var column = Count; column < _matched.Length; column++)
        {
          if (!_matched[column] && Predicate(space[column, row]))
            _matched[column] = true;
        }

        while (Count < _matched.Length && _matched[Count])
          Count++;
      }
    }
  }
}
