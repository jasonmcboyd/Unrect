using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The leading columns in which every cell satisfies the predicate. Asked column by column it
  /// reads the column; asked a row at a time, as a discovered block's width is under a row driver,
  /// it accumulates: a failing cell in column c rules out c and every column after it.
  /// </summary>
  internal sealed class TakeWhileAllColumnStrategy : IRowMajorColumnStrategy
  {
    public TakeWhileAllColumnStrategy(Func<Point<ISpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    internal Func<Point<ISpace>, bool> Predicate { get; }

    public IColumnAccumulator BeginColumns(int width) => new Accumulator(Predicate, width);

    public IColumnScan Begin() => new Scanning.RowMajorColumns(this, (space, column) =>
    {
      for (var row = 0; space.HasRow(row); row++)
        if (!Predicate(space[column, row]))
          return false;

      return true;
    });

    private sealed class Accumulator : IColumnAccumulator
    {
      public Accumulator(Func<Point<ISpace>, bool> predicate, int width)
      {
        Predicate = predicate;
        Count = width;
      }

      public int Count { get; private set; }

      public bool IsSettled => Count == 0;

      private Func<Point<ISpace>, bool> Predicate { get; }

      public void Include(Plane<ISpace> space, int row)
      {
        for (var column = 0; column < Count; column++)
        {
          if (!Predicate(space[column, row]))
          {
            Count = column;
            break;
          }
        }
      }
    }
  }
}
