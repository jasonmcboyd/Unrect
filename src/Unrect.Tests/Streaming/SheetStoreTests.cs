using System;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The window: how a sheet is cut into chunks, how many are held, and which one is dropped when
  /// one too many is wanted.
  /// <para>
  /// Everything here goes through the row-source seam. That is what the seam is for — a chunk
  /// boundary, an eviction and a re-read are all arrangeable in a few lines against a synthetic
  /// source, and none of them is arrangeable at all against a workbook.
  /// </para>
  /// </summary>
  public class SheetStoreTests
  {
    /// <summary>A store over a sheet of coordinate cells: every cell reads as its own <c>"column,row"</c>.</summary>
    private static SheetStore Store(
      int rows,
      int columns,
      int chunkRows,
      int windowChunks,
      int maxReaders = 2,
      int? readableRows = null)
    {
      var source = new FakeRowSource(new FakeSheet("Data", rows, columns, readableRows));
      var pool = new ReaderPool(source, maxReaders, warmReaders: false);

      return new SheetStore(pool, 0, "Data", rows, columns, chunkRows, windowChunks);
    }

    // --- The chunk constant ---------------------------------------------------------------------

    [Fact]
    public void BytesPerCell_IsTheSizeOfACellValue()
    {
      // The regression guard the spec asks for by name. The constant was 8 while Cell was a
      // class; leaving it at 8 after the struct merge would have tripled every chunk and sent a
      // default 8-column chunk (196,608 bytes) straight to the Large Object Heap that the 64 KB
      // target exists to avoid. This is the assertion that makes the next representation change
      // announce itself.
      Assert.Equal(SheetStore.BytesPerCell, Unsafe.SizeOf<Cell>());
    }

    [Theory]
    [InlineData(1, 2730)]
    [InlineData(2, 1365)]
    [InlineData(8, 341)]
    [InlineData(64, 42)]
    [InlineData(1024, 2)]
    public void DefaultChunkRows_FillsThe64KilobyteTarget(int columns, int expected)
    {
      // 65,536 / (24 * columns), which is the whole rule: a chunk is a fixed number of BYTES, so a
      // wider sheet gets fewer rows in one.
      Assert.Equal(expected, SheetStore.DefaultChunkRows(columns));
      Assert.True(expected * columns * SheetStore.BytesPerCell <= SheetStore.TargetChunkBytes);
    }

    [Fact]
    public void DefaultChunkRows_NeverDropsBelowOneRow()
    {
      // A sheet too wide for one row to fit the target still gets a row: a chunk cannot hold less
      // than that, and the alternative — zero — would be a store that can hold nothing.
      Assert.Equal(1, SheetStore.DefaultChunkRows(10000));
      Assert.Equal(1, SheetStore.DefaultChunkRows(int.MaxValue));
      Assert.Equal(1, SheetStore.DefaultChunkRows(0));
      Assert.Equal(1, SheetStore.DefaultChunkRows(-1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(64)]
    [InlineData(1024)]
    [InlineData(3541)]
    [InlineData(3542)]
    [InlineData(20000)]
    public void AChunkStaysOffTheLargeObjectHeap_OrIsASingleRow(int columns)
    {
      // The guarantee, stated with the condition that actually bounds it. Chunks are allocated and
      // dropped continuously as the window slides, so one on the Large Object Heap would trade a
      // bounded heap for a fragmenting one — but a chunk cannot hold less than a row, and one row
      // of 3,542 columns is already 85,008 bytes. Past about 3,541 columns the sheet itself
      // decides, and nothing the chunk maths can do will help. That is documented, not a bug.
      var rows = SheetStore.DefaultChunkRows(columns);
      var bytes = (long)rows * columns * SheetStore.BytesPerCell;

      Assert.True(
        bytes <= 85000 || rows == 1,
        $"{columns} columns: {rows} rows is {bytes} bytes, over the LOH threshold and more than one row.");
    }

    [Theory]
    [InlineData(8192, 341, 25)]     // the default window, at the default width of an 8-column sheet
    [InlineData(1024, 341, 4)]      // rounds up to 3, then the floor applies
    [InlineData(1, 341, 4)]         // a window of one row is still four chunks
    [InlineData(4096, 1024, 4)]     // exactly four
    [InlineData(10240, 1024, 10)]
    public void WindowChunksFor_RoundsUpToWholeChunksAndNeverGoesBelowFour(int windowRows, int chunkRows, int expected)
    {
      // The knob is rows because the sizing law is stated in rows; chunks are an implementation
      // detail a caller should never have to do arithmetic in. Four is the floor because below it
      // eviction thrashes on its own, whatever the declaration does.
      Assert.Equal(expected, SheetStore.WindowChunksFor(windowRows, chunkRows));
      Assert.Equal(4, SheetStore.MinimumWindowChunks);
    }

    // --- Reading ---------------------------------------------------------------------------------

    [Fact]
    public void ACellReadsAsItself_AcrossChunkBoundaries()
    {
      // The (row - chunk * ChunkRows) arithmetic, which is the one place an off-by-one would show
      // up as quietly reading the wrong row rather than as a crash. Rows 9/10 and 19/20 straddle
      // chunk boundaries at ChunkRows = 10.
      var store = Store(rows: 100, columns: 4, chunkRows: 10, windowChunks: 4);

      Assert.Equal("0,0", store.GetCell(0, 0).AsText());
      Assert.Equal("3,9", store.GetCell(3, 9).AsText());
      Assert.Equal("0,10", store.GetCell(0, 10).AsText());
      Assert.Equal("2,19", store.GetCell(2, 19).AsText());
      Assert.Equal("2,20", store.GetCell(2, 20).AsText());
      Assert.Equal("3,99", store.GetCell(3, 99).AsText());
    }

    [Fact]
    public void AChunkOverAShortSheetIsBlankWhereTheSourceRanOut()
    {
      // No pre-fill: default(Cell) IS Blank since the struct merge, so a freshly allocated
      // chunk is already an all-blank band and the rows a short sheet never yields are right
      // without a second pass over them. This is the observable form of "the fill loop was doing
      // nothing" — a sheet claiming 20 rows and yielding 14.
      var store = Store(rows: 20, columns: 3, chunkRows: 10, windowChunks: 4, readableRows: 14);

      Assert.Equal("0,13", store.GetCell(0, 13).AsText());
      Assert.True(store.GetCell(0, 14).IsBlank);
      Assert.True(store.GetCell(2, 19).IsBlank);

      // One chunk, four rows in it: the store read what there was and stopped.
      var stats = store.Snapshot();
      Assert.Equal(1, stats.ChunkLoads);
      Assert.Equal(4, stats.RowsMaterialised);
    }

    // --- A sheet that will not say how big it is -------------------------------------------------
    //
    // Some xlsx files carry no dimension element. The index is growable, so the store no longer
    // needs the row count to hold the sheet — the workbook measures it instead, which is why these
    // go through Workbook.Over rather than building a store directly: the measure is where a sheet
    // is vended, and only a synthetic source can report no dimension at all.

    /// <summary>A workbook over <paramref name="source"/>, warming off so the counts are deterministic.</summary>
    private static Workbook Book(FakeRowSource source, int windowRows = 8192, int chunkRows = 0) =>
      Workbook.Over(
        source,
        new WorkbookOptions { WarmReaders = false, WindowRows = windowRows, ChunkRows = chunkRows });

    [Fact]
    public void ASheetThatReportsNoDimensionIsMeasuredByReadingIt()
    {
      // The extent is discovered rather than declared, and it is a real extent when it arrives: the
      // space is exactly as big as the sheet turned out to be, so a projection sees the same thing
      // it would have seen had the file described itself.
      using var book = Book(new FakeRowSource(new FakeSheet("Ledger", 25, 3) { ReportsDimension = false }));

      var sheet = book.Sheet("Ledger");

      Assert.Equal(3, sheet.Area.Size.Width);
      Assert.Equal(25, sheet.Area.Size.Height);
      Assert.Equal("0,0", sheet.AsText(0, 0));
      Assert.Equal("2,24", sheet.AsText(2, 24));
    }

    [Fact]
    public void AMeasuredSheetIsStillReadAWindowAtATime()
    {
      // Measuring settles the extent; it must not settle the memory. The window still bounds what
      // is held, every row is still materialised exactly once, and the survey pass itself
      // materialises nothing — it counted rows and dropped them.
      using var book = Book(new FakeRowSource(new FakeSheet("Ledger", 100, 2) { ReportsDimension = false }), windowRows: 40, chunkRows: 10);
      var sheet = book.Sheet("Ledger");

      for (var row = 0; row < 100; row++)
        _ = sheet.AsText(0, row);

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(100, stats.RowsMaterialised);
      Assert.Equal(10, stats.ChunkLoads);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.True(stats.PeakResidentChunks <= stats.WindowChunks);
    }

    [Fact]
    public void ReadingPastTheEndOfAMeasuredSheetIsOutOfBounds()
    {
      // The contract every space keeps, kept here too: the end found by measuring is the end, and
      // running off it is an ordinary bounds condition a declaration may recover from — not a blank
      // row, which would let a scan run on past the sheet.
      using var book = Book(new FakeRowSource(new FakeSheet("Ledger", 6, 2) { ReportsDimension = false }));
      var sheet = book.Sheet("Ledger");

      Assert.Throws<OutOfBoundsException>(() => sheet.AsText(0, 6));
      Assert.Throws<OutOfBoundsException>(() => sheet.AsText(2, 0));
    }

    [Fact]
    public void TheRowsTheSurveyReadAreReportedAsRowsMeasured()
    {
      // The survey is a forward pass over the whole file that the caller never asked for, and it
      // moves none of the sheet's other counters — it materialises nothing, loads no chunk and
      // touches no window — so without this number its cost is invisible. Reported where the
      // sheet's other costs are read, and zero for the sheets that described themselves, which is
      // nearly all of them: a column of zeroes is not worth the width, so ToString omits it there.
      using var book = Book(new FakeRowSource(
        new FakeSheet("Surveyed", 25, 3) { ReportsDimension = false },
        new FakeSheet("Declared", 25, 3)));

      _ = book.Sheet("Surveyed");
      _ = book.Sheet("Declared");

      // Twenty-five rows counted and dropped, against zero rows materialised: the pass cost time
      // and no memory, which is exactly the distinction the counter exists to draw.
      var surveyed = book.Statistics("Surveyed")!.Value;
      Assert.Equal(25, surveyed.RowsMeasured);
      Assert.Equal(0, surveyed.RowsMaterialised);

      // A sheet whose reader answered was never surveyed, so it owes nothing to report.
      Assert.Equal(0, book.Statistics("Declared")!.Value.RowsMeasured);
    }

    [Fact]
    public void ASheetThatYieldsNoRowsMeasuresEmpty()
    {
      // The degenerate case reads as empty rather than as anything else. A sheet nobody can size
      // and nobody can read is 0 by 0, which is what the eager path makes of the same file.
      using var book = Book(new FakeRowSource(new FakeSheet("Ledger", 0, 0) { ReportsDimension = false }));

      var sheet = book.Sheet("Ledger");

      Assert.Equal(0, sheet.Area.Size.Height);
      Assert.Throws<OutOfBoundsException>(() => sheet.AsText(0, 0));
    }

    // --- The window ------------------------------------------------------------------------------

    [Fact]
    public void AWalkDownASheetTallerThanTheWindowReadsEveryRowExactlyOnce()
    {
      // The headline property: bounded memory costs nothing on a monotone walk. Every row is
      // materialised once, no chunk is ever wanted twice, and the resident set never exceeds the
      // budget — which is the whole promise of the window.
      var store = Store(rows: 1000, columns: 2, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 1000; row++)
        _ = store.GetCell(0, row);

      var stats = store.Snapshot();

      Assert.Equal(1000, stats.RowsMaterialised);
      Assert.Equal(100, stats.ChunkLoads);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(0, stats.RowsSkipped);
      Assert.True(stats.PeakResidentChunks <= stats.WindowChunks);
    }

    [Fact]
    public void EveryChunkLoadedIsEitherResidentOrEvicted()
    {
      // The accounting identity the window rests on. If it ever failed, the store would be either
      // holding chunks it thinks it dropped or reporting a budget it is not keeping.
      var store = Store(rows: 500, columns: 2, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 500; row++)
        _ = store.GetCell(0, row);

      var stats = store.Snapshot();

      Assert.Equal(stats.ChunkLoads - stats.Evictions, stats.ResidentChunks);
      Assert.Equal(4, stats.ResidentChunks);
      Assert.Equal(4, stats.PeakResidentChunks);
    }

    [Fact]
    public void ARowTheWindowHasMovedPastIsLoadedAgain()
    {
      // The cost model, made concrete: a second pass over rows the window has dropped is a reload,
      // and it is exactly one for one chunk. This is the number a caller reads to find out that a
      // declaration reaches backwards.
      var store = Store(rows: 500, columns: 2, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 500; row++)
        _ = store.GetCell(0, row);

      Assert.Equal(0, store.Snapshot().ChunkReloads);

      _ = store.GetCell(0, 0);

      Assert.Equal(1, store.Snapshot().ChunkReloads);
    }

    [Fact]
    public void ARowStillResidentIsNotLoadedAgain()
    {
      // The other half: re-reading inside the window is free. Without this, "hold a window" would
      // mean nothing — every read would be a load.
      var store = Store(rows: 500, columns: 2, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 40; row++)
        _ = store.GetCell(0, row);

      var loads = store.Snapshot().ChunkLoads;

      for (var repeat = 0; repeat < 5; repeat++)
        for (var row = 0; row < 40; row++)
          _ = store.GetCell(0, row);

      Assert.Equal(loads, store.Snapshot().ChunkLoads);
      Assert.Equal(0, store.Snapshot().ChunkReloads);
    }

    [Fact]
    public void SweepingABandThatFitsTheWindowLoadsEachChunkOnce_WhateverTheOrder()
    {
      // The residency law. A HorizontalFlow over a band reads it once per child, and the order the
      // children read in is not something the store gets to choose — so a band inside the budget
      // must survive being swept forwards, backwards and at random. Plain LRU is precisely wrong
      // here, which is why the locus exists.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);
      const int BandTop = 50;
      const int BandHeight = 30;   // three chunks, inside a four-chunk budget

      // The band, announced once — which is what a placement does, and the whole of what the store
      // is ever told about why a read is happening.
      store.Sweeping(BandTop, BandHeight);

      void Sweep(Func<int, int> order)
      {
        for (var index = 0; index < BandHeight; index++)
          _ = store.GetCell(0, BandTop + order(index));
      }

      Sweep(index => index);                        // forwards
      Sweep(index => BandHeight - 1 - index);       // backwards
      Sweep(index => (index * 7) % BandHeight);     // scattered

      var stats = store.Snapshot();

      Assert.Equal(3, stats.ChunkLoads);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(0, stats.Evictions);
    }

    // --- The union rule, at its four boundaries ----------------------------------------------------
    //
    // `Anchor` takes one decision per announcement: a band that OVERLAPS the open one grows it by
    // union, a band clear of it REPLACES it. Everything the store does about residency follows from
    // that one line — `Overlaps(from, to) => from < _locusTo && to > _locusFrom` — and the four cases
    // below are its boundaries, announced directly rather than provoked through a declaration.
    //
    // Why directly: through the engine an announcement is a consequence of a placement, so a test
    // that arranges two particular bands is really a test of the declaration that produced them, and
    // it breaks when the declaration's shape changes for unrelated reasons. These four are about the
    // rule, so they state the rule's inputs.

    [Fact]
    public void AnAdjacentBandReplacesTheOpenOneRatherThanUnioningWithIt()
    {
      // (a) The half-open boundary, and the case the overlap test was written for: [0,40) and
      // [40,80) share no row — 40 is past the end of the first — so the second REPLACES the first.
      //
      // Unioning them instead would make a 80-row locus against a 40-row budget, which cannot be
      // held: the store would count an overrun and give up the locus entirely, on a pair of bands
      // each of which fits perfectly. A monotone walk announces exactly this pair at every step, so
      // the wrong answer here is not an edge case — it is the whole walk, and it read as hundreds of
      // overruns before the rule said `from < _locusTo` rather than `from <= _locusTo`.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Sweeping(0, 40);
      store.Sweeping(40, 40);

      for (var row = 40; row < 80; row++)
        _ = store.GetCell(0, row);

      var stats = store.Snapshot();

      Assert.Equal(0, stats.WindowOverruns);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(4, stats.ChunkLoads);
      Assert.Equal(0, stats.Evictions);
    }

    [Fact]
    public void AContainedBandGrowsToItsParentRatherThanShrinkingTheLocusToItself()
    {
      // (b) The residency law, stated at the rule rather than through a declaration: a child extent
      // announced inside its parent's band must not shrink the locus to itself. A HorizontalFlow's
      // children each announce a sub-band of the band the flow opened, and the flow still needs the
      // whole of it — so [0,40) then [10,20) unions back to [0,40).
      //
      // Observed through eviction, because that is the only thing the locus governs. Six chunks fit;
      // the band takes four and three reads elsewhere want three more, so one chunk is evicted. With
      // the parent's band held, the victim comes from outside it and the band survives; had the locus
      // shrunk to [10,20), chunk 0 would have been the oldest unprotected chunk and re-reading row 0
      // would cost a reload.
      var store = Store(rows: 400, columns: 2, chunkRows: 10, windowChunks: 6);

      store.Sweeping(0, 40);
      store.Sweeping(10, 10);

      for (var row = 0; row < 40; row++)
        _ = store.GetCell(0, row);

      foreach (var row in new[] { 100, 110, 120 })
        _ = store.GetCell(0, row);

      _ = store.GetCell(0, 0);

      var stats = store.Snapshot();

      Assert.Equal(0, stats.WindowOverruns);
      Assert.Equal(1, stats.Evictions);
      Assert.Equal(0, stats.ChunkReloads);
    }

    [Fact]
    public void TheSameBandAnnouncedTwiceIsTheIdentity()
    {
      // (c) The case every transparent wrapper produces: Optional, Padded, Until, WithColumnLabels,
      // a unit and a Select all place the same region again, so one band arrives as many times as it
      // has wrappers. The union of a band with itself is itself — nothing grows, nothing is given up,
      // and nothing is counted.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Sweeping(0, 40);
      store.Sweeping(0, 40);
      store.Sweeping(0, 40);

      for (var pass = 0; pass < 3; pass++)
        for (var row = 0; row < 40; row++)
          _ = store.GetCell(0, row);

      var stats = store.Snapshot();

      Assert.Equal(0, stats.WindowOverruns);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(4, stats.ChunkLoads);
      Assert.Equal(0, stats.Evictions);
    }

    [Fact]
    public void AnOversizedBandAnnouncedTwiceCountsOneOverrun()
    {
      // (d), first half — and the reason the counter is worth reading at all. A band that cannot be
      // held is one broken sizing law however many placements name it, and a wrapped shape names it
      // once per wrapper. A counter that ticked per announcement would report the declaration's
      // SPELLING, which is not something a caller can act on.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Sweeping(0, 50);
      store.Sweeping(0, 50);
      store.Sweeping(0, 50);

      Assert.Equal(1, store.Snapshot().WindowOverruns);
    }

    [Fact]
    public void AndTwoDifferentOversizedBandsCountTwo()
    {
      // (d), second half: the dedup is on the band, not on the condition. Two declarations each
      // sweeping more than the window can hold are two things to fix, and a caller raising
      // WindowRows wants to know that.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Sweeping(0, 50);
      store.Sweeping(100, 50);

      Assert.Equal(2, store.Snapshot().WindowOverruns);
    }

    [Fact]
    public void AndTheDedupIsAgainstCONSECUTIVEAnnouncementsSoABandThatFitsEndsTheRun()
    {
      // (d), the corner between the two halves above, and the one a reader is likeliest to guess
      // wrong. The dedup is against the run of announcements, not against the oversized band ever
      // seen: a band that FITS, arriving between two announcements of the same oversized one, ends
      // the run, so the second announcement is a fresh overrun.
      //
      // The reason is that something else was swept in between — the declaration went away and came
      // back, which is two occasions on which the window was too small, not one. `Anchor`'s fitting
      // branch clears `_oversizedFrom`/`_oversizedTo` to say so.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Sweeping(0, 50);
      store.Sweeping(0, 20);
      store.Sweeping(0, 50);

      Assert.Equal(2, store.Snapshot().WindowOverruns);

      // And the same pair with a DIFFERENT oversized band in the middle counts all three, which is
      // what makes the assertion above about the dedup rather than about the counter being stuck.
      var moving = Store(rows: 400, columns: 2, chunkRows: 10, windowChunks: 4);

      moving.Sweeping(0, 50);
      moving.Sweeping(100, 50);
      moving.Sweeping(0, 50);

      Assert.Equal(3, moving.Snapshot().WindowOverruns);
    }

    [Fact]
    public void ABandThatFitsTheWindowReportsNoOverrun()
    {
      // The control for the pair below. Five chunks inside a six-chunk budget: the band is
      // anchored, nothing is evicted from inside it, and neither counter has anything to say.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 6);

      store.Sweeping(50, 50);

      for (var pass = 0; pass < 3; pass++)
        for (var offset = 0; offset < 50; offset++)
          _ = store.GetCell(0, 50 + offset);

      var stats = store.Snapshot();

      Assert.Equal(0, stats.WindowOverruns);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(5, stats.ChunkLoads);
    }

    [Fact]
    public void ABandOneChunkTallerThanTheWindowReportsOneOverrunAndPaysForItInReloads()
    {
      // The sizing law being broken, and the two counters dividing the labour between them. The
      // overrun says WHY — a seven-chunk band cannot be held in six chunks — and it is counted once
      // for the band, because the band is announced once for the placement that opened it.
      // The reloads say WHAT IT COST: the band is swept three times and re-read almost entirely
      // each time. This pairing is the diagnostic a caller acts on by raising WindowRows.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 6);

      store.Sweeping(50, 70);

      for (var pass = 0; pass < 3; pass++)
        for (var offset = 0; offset < 70; offset++)
          _ = store.GetCell(0, 50 + offset);

      var stats = store.Snapshot();

      Assert.Equal(1, stats.WindowOverruns);
      Assert.Equal(14, stats.ChunkReloads);
      Assert.Equal(21, stats.ChunkLoads);
      Assert.True(stats.PeakResidentChunks <= stats.WindowChunks, "the budget is kept even while it is too small");
    }

    [Fact]
    public void ABandTallerThanTheWindowIsRereadRatherThanHeld()
    {
      // The collapse the sizing law exists to prevent, in miniature: one chunk of shortfall turns
      // "load it once" into "load it every time round". The measured version of this is three
      // orders of magnitude of wall time; here it is just arithmetic, which is the point — the
      // cost is structural and shows up in the counters before it shows up in a stopwatch.
      var store = Store(rows: 200, columns: 2, chunkRows: 10, windowChunks: 4);
      const int BandTop = 50;
      const int BandHeight = 50;   // five chunks against a four-chunk budget

      store.Sweeping(BandTop, BandHeight);

      for (var pass = 0; pass < 3; pass++)
        for (var offset = 0; offset < BandHeight; offset++)
          _ = store.GetCell(0, BandTop + offset);

      var stats = store.Snapshot();

      Assert.True(stats.ChunkReloads > 0, "a band that does not fit must be re-read");
      Assert.True(stats.PeakResidentChunks <= stats.WindowChunks, "the budget is kept even so");
    }

    [Fact]
    public void TheResidentSetNeverExceedsTheBudget()
    {
      // Whatever the access pattern. This is the promise a caller sizes their process on, so it is
      // asserted against a deliberately hostile script rather than a tidy one.
      var store = Store(rows: 1000, columns: 2, chunkRows: 10, windowChunks: 4);
      var random = new Random(20260903);

      for (var read = 0; read < 2000; read++)
      {
        var row = random.Next(1000);
        _ = store.GetCell(0, row);

        Assert.True(store.Snapshot().ResidentChunks <= 4);
      }

      Assert.Equal(4, store.Snapshot().PeakResidentChunks);
    }

    // --- What is NOT asserted here, and where it is instead -----------------------------------------
    //
    // RETENTION. The bytes below are what the store HOLDS; what the process keeps alive while a
    // result is held is a different question, and it is not a test's to answer — a duplicate string
    // is allocated by the reader before the adapter ever sees it, so an allocation counter measures
    // the wrong thing and a live-bytes measurement needs a settled heap. That is the Benchmarks
    // project's `Retention` leg: a deterministic one-shot measurement of LIVE bytes with a result
    // held, run as its own CI leg rather than as part of the suite.
    //
    // The phase-6 obligation on it is a NO-MOVE, not a new number: planes and points are structs
    // over the same cells, so the three floor rows and the `_Unique` controls must read what they
    // read before. It is not run from here and nothing here would notice if it moved — recorded so
    // that "the suite is green" is not mistaken for "retention was checked".

    [Fact]
    public void ResidentBytesAreTheCellsHeld()
    {
      // What the memory knob buys, in bytes: chunks × rows × columns × 24. Strings a Text cell
      // points at are the reader's, not counted here, and do not shrink with the window — the
      // floor that streaming does not remove.
      var store = Store(rows: 1000, columns: 5, chunkRows: 10, windowChunks: 4);

      for (var row = 0; row < 100; row++)
        _ = store.GetCell(0, row);

      var stats = store.Snapshot();

      Assert.Equal(4L * 10 * 5 * SheetStore.BytesPerCell, stats.ResidentBytes);
      Assert.Equal(4L * 10 * 5 * SheetStore.BytesPerCell, stats.PeakResidentBytes);
      Assert.Equal(40, stats.WindowRows);
    }

    // --- Lifetime --------------------------------------------------------------------------------

    [Fact]
    public void AReadAfterDisposeThrows_EvenWhereTheRowsAreStillInMemory()
    {
      // The check happens before the resident fast path on purpose. A read that succeeded because
      // its chunk happened to still be in memory would make the failure depend on the window's
      // history, and "it worked on my machine" is exactly what a lifetime bug looks like.
      var store = Store(rows: 100, columns: 2, chunkRows: 10, windowChunks: 4);

      Assert.Equal("0,0", store.GetCell(0, 0).AsText());

      store.Dispose();

      Assert.Throws<ObjectDisposedException>(() => store.GetCell(0, 0));
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
      var store = Store(rows: 100, columns: 2, chunkRows: 10, windowChunks: 4);

      store.Dispose();
      store.Dispose();
    }
  }
}
