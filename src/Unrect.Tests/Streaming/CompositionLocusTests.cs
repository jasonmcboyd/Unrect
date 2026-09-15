using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// What the streaming store is told when a strategy composes with another one — the seam where the
  /// phase-6 locus arrives early, and the one place these counters are <em>chosen</em> rather than
  /// inherited from the reading.
  /// <para>
  /// A windowed view hands its own extent down with every cell it reads, and that pair is how the
  /// store tells a sweep of a bounded band apart from a walk down the sheet. Until phase 6 the
  /// engine keeps that honest by cutting a real subspace object for every region it hands a
  /// projection. A composing STRATEGY does not: <c>RowsThenColumns</c> narrows its region by
  /// arithmetic before handing it to the second half, because the reads are identical and cutting a
  /// second object would cost one per call. So the column scan reads two rows and announces the
  /// band its parent object names, which on this sheet is all 1,201 of them.
  /// </para>
  /// <para>
  /// <b>That is a decision, and this is where it is recorded.</b> The answers do not move — the same
  /// declaration reads the same extent through either door — so nothing else in the suite can see
  /// it; only the counters can, and only here. Phase 6 replaces the slice-borne hint with a
  /// per-placement announcement, at which point these numbers are expected to move and this file is
  /// the thing that will say so.
  /// </para>
  /// <para>
  /// The window and chunk sizes mirror <see cref="WorkbookTests"/>' tall-sheet gate on purpose, so
  /// the second case below can be read directly against the plain monotone walk pinned there.
  /// </para>
  /// </summary>
  public class CompositionLocusTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary>A 256-row window in 64-row chunks, warming off — the tall-sheet gate's own sizing.</summary>
    private static WorkbookOptions Cold() =>
      new WorkbookOptions { WarmReaders = false, WindowRows = 256, ChunkRows = 64, MaxReaders = 3 };

    /// <summary>
    /// <paramref name="area"/> applied to the tall ledger through the streaming door: the extent it
    /// settled on, and the counters it cost. The statistics die with the workbook, so they are taken
    /// out before the <c>using</c> closes.
    /// </summary>
    private static (string Read, long ChunkLoads, long ChunkReloads, long Evictions, long RowsMaterialised, long WindowOverruns) Measure(IAreaStrategy area)
    {
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold());

      var read = Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(book.Sheet("Ledger"));
      var stats = book.Statistics("Ledger")!.Value;

      return (read, stats.ChunkLoads, stats.ChunkReloads, stats.Evictions, stats.RowsMaterialised, stats.WindowOverruns);
    }

    /// <summary>The same declaration through the eager door, where there is no window to announce anything to.</summary>
    private static string Eager(IAreaStrategy area)
      => Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(
        SpreadsheetSpace.Create(Path("tall-ledger.xlsx"), "Ledger"));

    /// <summary>
    /// Two rows taken without reading anything, then the columns measured inside them — the
    /// composition where the first half consumes no rows at all, so every read the store sees comes
    /// from the second half and carries the parent's band.
    /// </summary>
    private static IAreaStrategy FirstHalfReadsNothing()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRows(2), ColumnStrategies.TakeColumnsWhileAnyValue());

    /// <summary>The contrast: a first half that walks the sheet itself, so the reading is its own.</summary>
    private static IAreaStrategy FirstHalfReads()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRowsWhileAnyValue(), ColumnStrategies.TakeColumnsWhileAnyValue());

    [Fact]
    public void AColumnScanInsideAnUnreadRowCountAnnouncesItsParentsBand()
    {
      // The recorded numbers, and what each of them is saying.
      //
      // The extent is three columns by two rows and costs ONE chunk — 64 rows materialised, which is
      // the smallest unit this window deals in, for a reading that wanted two. Nothing is evicted
      // and nothing is reloaded, because nothing competes for residency.
      //
      // WindowOverruns is 1, and that is the whole point of the file. The column scan reads rows 0
      // and 1; the band it announces while doing so is the region its parent object names, all
      // 1,201 rows, which does not fit a 256-row window. The store is therefore told a sweep is
      // open that is not, and says so. It costs nothing here — an overrun WITH reloads is the
      // collapse worth acting on, and reloads are zero — but it is a hint the store was given
      // wrongly rather than a fact about the reading, and it is chosen: narrowing by arithmetic is
      // what buys the composition its single object per call.
      var (read, loads, reloads, evictions, materialised, overruns) = Measure(FirstHalfReadsNothing());

      Assert.Equal("3x2", read);

      Assert.Equal(1L, loads);
      Assert.Equal(0L, reloads);
      Assert.Equal(0L, evictions);
      Assert.Equal(64L, materialised);
      Assert.Equal(1L, overruns);
    }

    [Fact]
    public void WhereAFirstHalfThatReadsForItselfCostsTheWholeWalk()
    {
      // The contrast, and the control on the numbers above. Here the row rule walks the sheet, so
      // the reading is a plain monotone pass and the counters are exactly the tall-sheet gate's:
      // nineteen loads, fifteen evictions, every row read once, one overrun for the root extent
      // that could not be held. Those numbers are inherited from the walk; the ones above are not.
      var (read, loads, reloads, evictions, materialised, overruns) = Measure(FirstHalfReads());

      Assert.Equal("3x1201", read);

      Assert.Equal(19L, loads);
      Assert.Equal(0L, reloads);
      Assert.Equal(15L, evictions);
      Assert.Equal(1201L, materialised);
      Assert.Equal(1L, overruns);
    }

    [Theory]
    [InlineData("first-half-reads-nothing", "3x2")]
    [InlineData("first-half-reads", "3x1201")]
    public void AndTheAnswerIsTheSameThroughEitherDoor(string composition, string extent)
    {
      // Why none of the above is visible anywhere else, stated as a test rather than as prose: the
      // locus governs cost and never content, so the eager door — which has no window and is told
      // nothing — settles on exactly the same rectangle. A change to the announcement can therefore
      // only ever be caught by counters, which is the argument for pinning them at all.
      var area = composition == "first-half-reads-nothing" ? FirstHalfReadsNothing() : FirstHalfReads();

      Assert.Equal(extent, Eager(area));
      Assert.Equal(extent, Measure(area).Read);
    }
  }
}
