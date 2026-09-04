using System;
using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Column <c>c</c> is included when at least one of its cells satisfies the predicate, and columns
  /// are taken while that holds contiguously from 0.
  /// <para>
  /// Read row-major rather than column-major, which is the same answer for one forward pass instead
  /// of one pass per column, and — the reason for the shape — asks for the height only as the walk's
  /// terminal condition. A column-major scan asks for the full height of every column it reads,
  /// which against a lazily bounded space resolves the whole bound before anything has consumed it.
  /// </para>
  /// <para>
  /// The walk lives in the accumulator rather than in <c>SelectColumns</c>, so the same steps serve
  /// a rows-then-columns extent measuring its width and its height in one pass.
  /// </para>
  /// </summary>
  internal sealed class TakeWhileAnyColumnStrategy : IRowMajorColumnStrategy
  {
    public TakeWhileAnyColumnStrategy(Func<CellValue, bool> predicate)
    {
      Predicate = predicate;
    }

    private Func<CellValue, bool> Predicate { get; }

    public IColumnAccumulator BeginColumns(int width) => new Accumulator(Predicate, width);

    private sealed class Accumulator : IColumnAccumulator
    {
      private readonly bool[] _matched;

      public Accumulator(Func<CellValue, bool> predicate, int width)
      {
        Predicate = predicate;
        _matched = new bool[width];
      }

      /// <summary>The leading run of matched columns — the answer so far.</summary>
      public int Count { get; private set; }

      /// <summary>A later row can only extend the run, so it is settled once it spans the full width.</summary>
      public bool IsSettled => Count == _matched.Length;

      private Func<CellValue, bool> Predicate { get; }

      public void Include(ISpace space, int row)
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
