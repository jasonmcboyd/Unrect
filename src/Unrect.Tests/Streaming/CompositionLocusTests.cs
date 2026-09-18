using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// What the streaming store is told when a strategy composes with another one — and the one place
  /// these counters are <em>chosen</em> rather than inherited from the reading.
  /// <para>
  /// The store tells a sweep of a bounded band apart from a walk down the sheet by being told which
  /// band is open. Until phase 6 it was told by inference: a windowed view handed its own extent
  /// down with every cell it read, so the band was whatever subspace object happened to make the
  /// read. That made a composing STRATEGY a hazard — <c>RowsThenColumns</c> narrows its region by
  /// arithmetic rather than cutting a second object, so a column scan reading two rows announced the
  /// band its PARENT object named, all 1,201 of them, and the store was told a sweep was open that
  /// was not.
  /// </para>
  /// <para>
  /// <b>Phase 6 removes the inference, and with it the hazard.</b> The engine announces once per
  /// placement (<c>ISweepAware</c>), and what it announces is the placement's own region — the
  /// rectangle the declaration asked for — so a strategy's reads announce nothing at all and the
  /// composition's spelling cannot reach the counters. The first case below is therefore the one
  /// that moved: its overrun was an artefact of the inference and is gone. The second did not move,
  /// because a declaration that really does open a 1,201-row band really does overrun a 256-row
  /// window, which is the honest reading the counter exists for.
  /// </para>
  /// <para>
  /// <b>What is still chosen, and still recorded here.</b> The answers do not move — the same
  /// declaration reads the same extent through either door — so nothing else in the suite can see
  /// what the store was told; only the counters can, and only here. That is as true of a number that
  /// is now zero as of one that is not.
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
    public void AColumnScanInsideAnUnreadRowCountAnnouncesNothingOfItsOwn()
    {
      // The recorded numbers, and what each of them is saying.
      //
      // The extent is three columns by two rows and costs ONE chunk — 64 rows materialised, which is
      // the smallest unit this window deals in, for a reading that wanted two. Nothing is evicted
      // and nothing is reloaded, because nothing competes for residency.
      //
      // WindowOverruns is 0, and that is the whole point of the file. What is announced is the
      // placement's own region, once, where the engine cuts it — three by two, which fits a 256-row
      // window with room to spare. The column scan's reads announce nothing: the strategy's spelling
      // (narrowing by arithmetic rather than cutting a second subspace object, which is what buys
      // the composition one object per call) no longer reaches the store at all.
      //
      // This read 1 until phase 6, when the band was inferred from whichever object made the read
      // and the scan's reads therefore carried its parent's 1,201 rows. That overrun described the
      // declaration's spelling and not its reading — a hint given wrongly — and it is gone with the
      // inference that produced it, not suppressed.
      var (read, loads, reloads, evictions, materialised, overruns) = Measure(FirstHalfReadsNothing());

      Assert.Equal("3x2", read);

      Assert.Equal(1L, loads);
      Assert.Equal(0L, reloads);
      Assert.Equal(0L, evictions);
      Assert.Equal(64L, materialised);
      Assert.Equal(0L, overruns);
    }

    [PullOnlyFact("the window, the pool and the sweep announcement retire with the pull interpreter")]
    public void WhereAFirstHalfThatReadsForItselfCostsTheWholeWalk()
    {
      // The contrast, and the control on the numbers above — kept, because it is what shows the zero
      // above is a band that fits rather than a counter that stopped counting. Here the row rule
      // walks the sheet, so the reading is a plain monotone pass and the counters are exactly the
      // tall-sheet gate's: nineteen loads, fifteen evictions, every row read once, and one overrun
      // for the 1,201-row band this placement really does open. Those numbers are inherited from the
      // walk; the ones above are not.
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
