using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Spreadsheets;

using static Unrect.Shapes.Shape;

namespace Unrect.Benchmarks
{
  /// <summary>
  /// Reading a sheet through a window instead of holding all of it: what streaming costs, and what
  /// the two knobs that govern it are worth.
  ///
  /// <para>Three pairs, and the pairs are the point. Each is two rows that differ in exactly one
  /// thing, and each must stay in this family so both halves are measured on the same machine in the
  /// same run — a ratio across matrix legs would be a ratio between two different CPUs.</para>
  /// <list type="bullet">
  ///   <item><b>Eager vs windowed</b> — the headline. The same declaration over the same rows, once
  ///     against a materialised grid and once through a window.</item>
  ///   <item><b>Window fits vs window half the band</b> — the sizing law. A window must be at least
  ///     as tall as the tallest extent open at one time, and a band sweep is open across the whole
  ///     band. This pair is that law's trend line.</item>
  ///   <item><b>One reader vs three</b> — the pool. Backward reaches with nothing parked behind them
  ///     against the same reaches with a pool to serve them.</item>
  /// </list>
  ///
  /// <para><b>Read the adversarial pair with its caveat.</b> The fixture's source opens for free,
  /// where a spreadsheet's costs about five CPU-bound seconds. So those two rows measure the
  /// repositioning half of the pool's value and not the open half — which is the half that made
  /// warming worth building, is a property of ExcelDataReader, and is deliberately measured nowhere
  /// in CI.</para>
  ///
  /// <para>Every row returns a checksum of what it read. Both fidelity bugs found while building
  /// this rig produced entirely plausible timings of the wrong thing.</para>
  /// </summary>
  [MemoryDiagnoser]
  [BenchmarkCategory("Streaming")]
  public class Streaming
  {
    private static readonly IShape<IReadOnlyList<StreamedRow>> Table = TableRows<StreamedRow>();

    // One range over a band, swept five times — once per column read. Each sweep walks the band
    // end to end, so the whole band is open across all five: the access pattern the window has to be
    // sized for, and the one a HorizontalFlow of five children over a band produces.
    //
    // A flow is not used here because its children would have to divide the band's WIDTH between
    // them, which changes what is being measured; five passes over one extent is the same demand on
    // the window with nothing else in the way.
    private static readonly IShape<long> Band = Range(Extent(StreamingSpaces.Columns, StreamingSpaces.BandRows), block =>
    {
      long sum = 0;

      for (var child = 0; child < 5; child++)
        for (var row = 0; row < block.Height; row++)
          sum += block[child, row].GetHashCode();

      return sum;
    });

    private ISpace _grid = default!;
    private ISpace _resident = default!;
    private ReaderPool _residentPool = default!;

    [GlobalSetup]
    public void Setup()
    {
      _grid = StreamingSpaces.Grid();

      // A window large enough to hold the whole sheet, read once here: the warm-reuse row measures
      // what a second declaration over an already-open sheet costs, which is the property that makes
      // holding a workbook open worth doing.
      _residentPool = StreamingSpaces.Pool();
      _resident = StreamingSpaces.Windowed(_residentPool, windowRows: StreamingSpaces.Rows + 1);
      Table.Map(_resident);
    }

    [GlobalCleanup]
    public void Cleanup() => _residentPool.Dispose();

    /// <summary>The baseline: the whole sheet in memory, read as an array.</summary>
    [Benchmark(Baseline = true)]
    public int Monotone_Eager() => Table.Map(_grid).Count;

    /// <summary>
    /// The headline. The same declaration through a window that holds a fraction of the sheet, with
    /// a fresh store each operation so every row is genuinely streamed rather than found resident.
    /// </summary>
    [Benchmark]
    public int Monotone_Windowed()
    {
      using var pool = StreamingSpaces.Pool();

      return Table.Map(StreamingSpaces.Windowed(pool)).Count;
    }

    /// <summary>
    /// A second pass over a sheet already read, with the window big enough to have kept it. No
    /// reader is opened and no row is re-read — the warm reuse that makes
    /// <c>Workbook.Sheet(name)</c> idempotent worth relying on.
    /// </summary>
    [Benchmark]
    public int Monotone_Resident() => Table.Map(_resident).Count;

    /// <summary>
    /// Five children sweeping a band that fits inside the window: every chunk loads once, whatever
    /// order the children read in. Measured at this size, 118 loads and no reloads.
    /// </summary>
    [Benchmark]
    public long Band_WindowFits()
    {
      using var pool = StreamingSpaces.Pool();

      return Band.Map(StreamingSpaces.Windowed(pool, windowRows: StreamingSpaces.BandRows * 2));
    }

    /// <summary>
    /// The same sweep with a window half the band. Every child walks rows the window has already
    /// thrown away, so the chunks reload — the collapse the sizing law exists to prevent. Measured
    /// at this size, 590 loads of which 472 are reloads, against the 118 and 0 above.
    /// </summary>
    [Benchmark]
    public long Band_WindowTooSmall()
    {
      using var pool = StreamingSpaces.Pool();

      return Band.Map(StreamingSpaces.Windowed(pool, windowRows: StreamingSpaces.BandRows / 2));
    }

    /// <summary>
    /// Top, bottom, top, bottom, with one reader and so nothing parked behind to serve the reaches.
    /// Measured: 47 reopens and 11.9 million rows skipped to get back each time.
    /// </summary>
    [Benchmark]
    public long Adversarial_OneReader() => ReachBack(maxReaders: 1);

    /// <summary>
    /// The same reaches with a pool that can leave a reader parked at each end. Measured: 24 reopens
    /// and 6.7 million rows skipped, with 44 of the reaches served cheaply by a parked reader.
    /// </summary>
    [Benchmark]
    public long Adversarial_Pooled() => ReachBack(maxReaders: 3);

    private static long ReachBack(int maxReaders)
    {
      using var pool = StreamingSpaces.Pool(maxReaders);

      // The smallest window on purpose: the two ends cannot both be resident, so each turn really
      // does have to go back for what the last one evicted. With a window that holds both, this
      // measures memory reads and says nothing about the pool.
      var space = StreamingSpaces.Windowed(pool, StreamingSpaces.SmallestWindowRows);
      long sum = 0;

      for (var turn = 0; turn < StreamingSpaces.ReachTurns; turn++)
      {
        var top = turn % 2 == 0;

        for (var row = 0; row < StreamingSpaces.ReachRows; row++)
          sum += space[0, top ? row + 1 : StreamingSpaces.Rows - 1 - row].GetHashCode();
      }

      return sum;
    }
  }
}
