using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Column <c>c</c> is included when every one of its cells satisfies the predicate, and columns are
  /// taken while that holds contiguously from 0.
  /// <para>
  /// Read row-major, for the reasons given on <see cref="TakeWhileAnyColumnStrategy"/>. The early
  /// exit is that strategy's dual: the answer starts at the full width and only ever falls, so it is
  /// settled once it reaches zero, where the "any" answer starts at zero and is settled once it
  /// reaches the full width.
  /// </para>
  /// </summary>
  internal sealed class TakeWhileAllColumnStrategy : IRowMajorColumnStrategy
  {
    public TakeWhileAllColumnStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public IColumnAccumulator BeginColumns(int width) => new Accumulator(Predicate, width);

    public int SelectColumns(ISpace space)
      => ColumnAccumulators.Fold(BeginColumns(space.Area.Width), space);

    private sealed class Accumulator : IColumnAccumulator
    {
      public Accumulator(Func<CellValue, bool> predicate, int width)
      {
        Predicate = predicate;
        Count = width;
      }

      /// <summary>The leading run of columns no row has ruled out yet — the answer so far.</summary>
      public int Count { get; private set; }

      /// <summary>Nothing is left to rule out once the run is empty, so zero is where it settles.</summary>
      public bool IsSettled => Count == 0;

      private Func<CellValue, bool> Predicate { get; }

      public void Include(ISpace space, int row)
      {
        // A failing cell in column c rules out c and every column after it, and no later row can
        // bring one back — so columns at or past the answer are never read again.
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
