using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Unrect.Projections;
using Unrect.Spreadsheets;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// The streaming door against the eager one: the same declaration over the same synthetic sheet,
  /// read whole into memory or driven forward one row at a time over a cursor. The monotone rows
  /// are the common case — a table walked top to bottom, nothing held; the band rows are the
  /// declaration that holds its extent, which is what a forward pass has to keep.
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Streaming")]
  public class Streaming
  {
    private static readonly IProjectionDefinition<ICellSpace, IReadOnlyList<StreamedRow>> Rows = Table<StreamedRow>();

    // One range over a band, swept five times — once per column read. A block lambda reads its
    // extent at random, so the whole band is held for the pass: the access pattern a forward pass
    // pays for, and the one a HorizontalFlow of five children over a band produces.
    private static readonly IProjectionDefinition<ICellSpace, long> Band = Range(Extent(StreamingSpaces.Columns, StreamingSpaces.BandRows), block =>
    {
      long sum = 0;

      for (var child = 0; child < 5; child++)
        for (var row = 0; row < block.Height; row++)
          sum += block[child, row].GetHashCode();

      return sum;
    });

    private ICellSpace _grid = default!;

    [GlobalSetup]
    public void Setup() => _grid = StreamingSpaces.Grid();

    [Benchmark(Baseline = true)]
    public int Monotone_Eager() => Rows.Map(_grid).Count;

    [Benchmark]
    public int Monotone_Streamed()
    {
      using var book = StreamingSpaces.Book();

      return Rows.Map(book.Sheet("Data")).Count;
    }

    [Benchmark]
    public int Monotone_SecondPass()
    {
      // What a second declaration over an already-open book costs: another pass over a fresh
      // cursor, with the string table already warm.
      using var book = StreamingSpaces.Book();
      Rows.Map(book.Sheet("Data"));

      return Rows.Map(book.Sheet("Data")).Count;
    }

    [Benchmark]
    public long Band_Eager() => Band.Map(_grid);

    [Benchmark]
    public long Band_Streamed()
    {
      using var book = StreamingSpaces.Book();

      return Band.Map(book.Sheet("Data"));
    }
  }
}
