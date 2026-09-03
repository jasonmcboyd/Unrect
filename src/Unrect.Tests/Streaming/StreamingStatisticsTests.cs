using System;
using System.Linq;

using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The counters, and the invariants that make them worth reading.
  /// <para>
  /// One counter is deliberately absent, and it is why this class exists. An earlier prototype
  /// reported <c>Rewinds</c>, documented as "backward reaches" and computed as
  /// <c>Reopens + SpareOpens</c>; on a run with 2,932 genuine backward reaches — every one of them
  /// served cheaply — it reported <c>1</c>. A number that answers a different question from the one
  /// its name asks is worse than no number, so every counter here is pinned to a scripted access
  /// pattern with an exact expected value rather than to "greater than zero".
  /// </para>
  /// </summary>
  public class StreamingStatisticsTests
  {
    private static SheetStore Store(ReaderPool pool, int rows, int columns, int chunkRows, int windowChunks) =>
      new SheetStore(pool, 0, "Data", rows, columns, chunkRows, windowChunks);

    // --- The pool's arithmetic --------------------------------------------------------------------

    [Fact]
    public void OpensAccountForEveryExpensiveEvent()
    {
      // Opens == 1 + SpareOpens + Reopens on a workbook whose parked reader was adopted: the parked
      // one, the spares, and the ones thrown away and paid for again. Nothing else can open a file.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Adopt(pool.OpenParked(), 0, 0);

      foreach (var row in new[] { 500, 100, 20, 900, 5, 700 })
        pool.Return(pool.Borrow(0, row, out _));

      var stats = pool.Snapshot();

      Assert.Equal(1 + stats.SpareOpens + stats.Reopens, stats.Opens);
      Assert.Equal(stats.Opens, source.Opens);
    }

    [Fact]
    public void WarmHitsNeverExceedSpareOpens()
    {
      // A warm hit IS a spare open — the same event, already paid for. More hits than opens would
      // mean the pool was crediting itself for work it never did.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 3, warmReaders: true);

      pool.Adopt(pool.OpenParked(), 0, 0);

      for (var reach = 0; reach < 10; reach++)
      {
        pool.Return(pool.Borrow(0, 100 + reach, out _));
        pool.Return(pool.Borrow(0, 3, out _));
      }

      var stats = pool.Snapshot();

      Assert.True(stats.WarmHits <= stats.SpareOpens, $"{stats.WarmHits} hits against {stats.SpareOpens} spare opens");
    }

    [Fact]
    public void CheapRewindsCountReachesRatherThanRows()
    {
      // The counter that replaced Rewinds, and the one whose direction is counter-intuitive: it goes
      // UP when the pool is doing its job. Five backward reaches served by a parked reader are five
      // cheap rewinds, not five problems.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(0, 500, out _));
      pool.Return(pool.Borrow(0, 0, out _));      // a second reader, parked at the top

      for (var reach = 0; reach < 5; reach++)
        pool.Return(pool.Borrow(0, 10 + reach, out _));

      var stats = pool.Snapshot();

      Assert.Equal(5, stats.CheapRewinds);
      Assert.Equal(0, stats.Reopens);
      Assert.Equal(2, stats.Opens);
    }

    [Fact]
    public void RowsPerReaderIsTheTravelOfEachReader()
    {
      // A list rather than the prototype's pre-rendered string, so a test can read one reader's
      // travel and ToString can still render the line a human wants.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 3, warmReaders: false);

      pool.Return(pool.Borrow(0, 400, out _));
      pool.Return(pool.Borrow(0, 100, out _));

      var stats = pool.Snapshot();

      Assert.Equal(3, stats.RowsPerReader.Count);
      Assert.Equal(new long[] { 400, 100, 0 }, stats.RowsPerReader.ToArray());
      Assert.Equal(500, stats.RowsPerReader.Sum());
    }

    // --- The store's arithmetic --------------------------------------------------------------------

    [Fact]
    public void RowsSkippedIsInvariantUnderTheNumberOfReaders()
    {
      // Window sizing owns repositioning; the pool owns opens. They are orthogonal knobs, and this
      // is the assertion that says so — the same script over one, two and three readers moves the
      // same number of rows past the reader, however differently it pays for them.
      var skipped = new long[3];
      var loads = new long[3];
      var reopens = new long[3];

      for (var readers = 1; readers <= 3; readers++)
      {
        var source = FakeRowSource.Of(rows: 400, columns: 2);
        using var pool = new ReaderPool(source, readers, warmReaders: false);
        var store = Store(pool, 400, 2, chunkRows: 10, windowChunks: 4);

        for (var row = 0; row < 400; row++) _ = store.GetCell(0, row, row, 1);
        for (var row = 0; row < 50; row++) _ = store.GetCell(0, row, row, 1);
        for (var row = 350; row < 400; row++) _ = store.GetCell(0, row, row, 1);

        skipped[readers - 1] = store.Snapshot().RowsSkipped;
        loads[readers - 1] = store.Snapshot().ChunkLoads;
        reopens[readers - 1] = pool.Snapshot().Reopens;
      }

      Assert.Equal(skipped[0], skipped[1]);
      Assert.Equal(skipped[1], skipped[2]);
      Assert.Equal(loads[0], loads[1]);
      Assert.Equal(loads[1], loads[2]);

      // ...while what the pool paid for that repositioning is exactly what changed.
      Assert.Equal(1, reopens[0]);
      Assert.Equal(0, reopens[1]);
      Assert.Equal(0, reopens[2]);
    }

    [Fact]
    public void RowsMaterialisedCountsRowsAdaptedIntoCells()
    {
      var source = FakeRowSource.Of(rows: 95, columns: 3);
      using var pool = new ReaderPool(source, 2, warmReaders: false);
      var store = Store(pool, 95, 3, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 95; row++)
        _ = store.GetCell(0, row, row, 1);

      var stats = store.Snapshot();

      Assert.Equal(95, stats.RowsMaterialised);
      Assert.Equal(10, stats.ChunkLoads);       // nine full chunks and a five-row tail
    }

    [Fact]
    public void TheWindowIsReportedInBothChunksAndRows()
    {
      var source = FakeRowSource.Of(rows: 500, columns: 2);
      using var pool = new ReaderPool(source, 1, warmReaders: false);
      var store = Store(pool, 500, 2, chunkRows: 25, windowChunks: 6);

      var stats = store.Snapshot();

      Assert.Equal(25, stats.ChunkRows);
      Assert.Equal(6, stats.WindowChunks);
      Assert.Equal(150, stats.WindowRows);
    }

    // --- The one-line forms -------------------------------------------------------------------------

    [Fact]
    public void SheetStatisticsRenderTheirCountersInOneLine()
    {
      // The prototype's diagnostic line was genuinely good for reading a run, so it survives — minus
      // the counters that did not. It is pinned against a run that has something to say: a
      // seven-chunk band swept twice through a six-chunk window, so every counter on the line is a
      // number somebody would act on rather than a zero.
      var source = FakeRowSource.Of(rows: 200, columns: 2);
      using var pool = new ReaderPool(source, 2, warmReaders: false);
      var store = Store(pool, 200, 2, chunkRows: 10, windowChunks: 6);

      for (var pass = 0; pass < 2; pass++)
        for (var offset = 0; offset < 70; offset++)
          _ = store.GetCell(0, 50 + offset, 50, 70);

      var rendered = store.Snapshot().ToString();

      Assert.Equal(
        "'Data' chunk 10r x 6 (60 rows) | loads 14 (reloads 7) | evictions 8 | overruns 1 | " +
        "rows read 140 skipped 100 | resident 6 chunks / 2,880B (peak 6 / 2,880B)",
        rendered);

      Assert.DoesNotContain("rewinds", rendered);       // that word belongs to the pool, not a sheet
    }

    [Fact]
    public void PoolStatisticsRenderTheirCountersInOneLine()
    {
      var source = FakeRowSource.Of(rows: 100, columns: 2);
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      pool.Return(pool.Borrow(0, 10, out _));

      var rendered = pool.Snapshot().ToString();

      Assert.Equal(
        "readers 1/2 | opens 1 | reopens 0 | spare opens 1 (warm 0, waited 0ms) | " +
        "cheap rewinds 0 | per reader 10/0",
        rendered);
    }

    [Fact]
    public void PoolStatisticsRenderEveryReadersTravel()
    {
      // The list, rendered. RowsPerReader is a list rather than the prototype's pre-baked string so
      // a test can read one reader's travel; ToString is where the string comes back.
      var source = FakeRowSource.Of(rows: 1000, columns: 2);
      using var pool = new ReaderPool(source, 2, warmReaders: false);

      for (var pass = 0; pass < 2; pass++)
        for (var offset = 0; offset < 70; offset++)
          pool.Return(pool.Borrow(0, 50 + offset, out _));

      var stats = pool.Snapshot();

      Assert.Equal("readers 2/2 | opens 2 | reopens 0 | spare opens 2 (warm 0, waited 0ms) | " +
                   $"cheap rewinds {stats.CheapRewinds} | per reader {stats.RowsPerReader[0]}/{stats.RowsPerReader[1]}",
                   stats.ToString());
    }
  }
}
