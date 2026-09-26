using System;

using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// The leading columns in which every cell satisfies the predicate. Asked column by column it
  /// reads the column; asked a row at a time, as a discovered block's width is under a row driver,
  /// it accumulates: a failing cell in column c rules out c and every column after it.
  /// </summary>
  internal sealed class TakeWhileAllColumnStrategy<TSpace> : IRowMajorColumnStrategy<TSpace>
    where TSpace : class, ISpace
  {
    public Orientation Along => Orientation.Horizontal;

    public TakeWhileAllColumnStrategy(Func<Point<TSpace>, bool> predicate)
    {
      Predicate = predicate;
    }

    internal Func<Point<TSpace>, bool> Predicate { get; }

    public IColumnAccumulator<TSpace> BeginColumns(int width) => new Accumulator(Predicate, width);

    public ILineScan<TSpace> Begin() => new Scanning.RowMajorColumns<TSpace>(this, (space, column) =>
    {
      for (var row = 0; space.HasRow(row); row++)
        if (!Predicate(space[column, row]))
          return false;

      return true;
    });

    private sealed class Accumulator : IColumnAccumulator<TSpace>
    {
      public Accumulator(Func<Point<TSpace>, bool> predicate, int width)
      {
        Predicate = predicate;
        Count = width;
      }

      public int Count { get; private set; }

      public bool IsSettled => Count == 0;

      private Func<Point<TSpace>, bool> Predicate { get; }

      public void Include(Plane<TSpace> space, int row)
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
