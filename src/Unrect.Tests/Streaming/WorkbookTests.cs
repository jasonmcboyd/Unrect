using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Shapes.Shape;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The public door: opening a file, naming a sheet, and the lifetime that ties a vended view to
  /// the workbook it came from.
  /// <para>
  /// These read real workbooks, because what they are about — the catalogue, the adopted reader,
  /// the disposed view — is exactly the part a synthetic source cannot stand in for.
  /// </para>
  /// </summary>
  public class WorkbookTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary>Warming off by default here: a background open makes counting non-deterministic, and most of these are about counts.</summary>
    private static WorkbookOptions Cold(int windowRows = 8192, int chunkRows = 0, int maxReaders = 3) =>
      new WorkbookOptions { WarmReaders = false, WindowRows = windowRows, ChunkRows = chunkRows, MaxReaders = maxReaders };

    // --- Vending ----------------------------------------------------------------------------------

    [Fact]
    public void ASheetReadsAsTheEagerPathReadsIt()
    {
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var streamed = book.Sheet("Report");
      var eager = SpreadsheetSpace.Create(Path("simple-report.xlsx"), "Report");

      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal("Capital Activity Report", streamed[0, 0].GetString());
    }

    [Fact]
    public void SheetTwiceReadsFromOneStore()
    {
      // The single most valuable property of the design: a second declaration over an already-open
      // book re-pays neither the reader open nor, when the rows are still resident, the read. Both
      // counters have to be unchanged — Opens says no file was touched, ChunkLoads says no row was.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var declaration = Column(4, c => c[0].GetString());

      _ = declaration.Map(book.Sheet("Report"));

      var opens = book.ReaderStatistics.Opens;
      var loads = book.Statistics("Report")!.Value.ChunkLoads;

      _ = declaration.Map(book.Sheet("Report"));

      Assert.Equal(opens, book.ReaderStatistics.Opens);
      Assert.Equal(loads, book.Statistics("Report")!.Value.ChunkLoads);
    }

    [Fact]
    public void AViewIsAValueRatherThanAHandle()
    {
      // Sheet() hands back a new view each time and they share one store, which is what makes a
      // view free to slice, pass around and keep. Reference equality would be the wrong promise —
      // the promise is that they read the same rows.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      var first = book.Sheet("Report");
      var second = book.Sheet("Report");

      Assert.NotSame(first, second);
      Assert.Equal(first[0, 0], second[0, 0]);
      Assert.Equal(1, book.Statistics("Report")!.Value.ChunkLoads);
    }

    [Fact]
    public void ASlicedViewSharesTheStoreAndReadsTheRightCells()
    {
      // Slicing is free and slices share the window, so a declaration that decomposes a sheet into
      // a hundred regions still holds one window rather than a hundred.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");
      var slice = space.GetSubspace(new Offset(0, 5), new Area(4, 5));

      Assert.Equal(space[0, 5], slice[0, 0]);
      Assert.Equal(space[2, 7], slice[2, 2]);

      var nested = slice.GetSubspace(new Offset(1, 1), new Area(2, 2));

      Assert.Equal(space[1, 6], nested[0, 0]);
      Assert.Equal(1, book.Statistics("Report")!.Value.ChunkLoads);
    }

    [Fact]
    public void AnIndexPastTheEndOfAViewIsABoundsCondition()
    {
      // OutOfBoundsException and not IndexOutOfRangeException, deliberately: the engine's fault list
      // classifies the latter as a bug in the reading code, non-absorbable — while running off the
      // end of a space is an ordinary bounds condition a declaration is allowed to recover from.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");

      Assert.Throws<OutOfBoundsException>(() => space[-1, 0]);
      Assert.Throws<OutOfBoundsException>(() => space[space.Area.Size.Width, 0]);
      Assert.Throws<OutOfBoundsException>(() => space[0, space.Area.Size.Height]);
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(99, 99)));
    }

    // --- The catalogue -----------------------------------------------------------------------------

    [Fact]
    public void SheetNamesAreTheWorkbooksOwn()
    {
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      Assert.Equal(new[] { "Cover", "Summary", "Detail" }, book.SheetNames.ToArray());
    }

    [Fact]
    public void AskingForTheNamesFirstStillLeavesEverySheetReadable()
    {
      // SheetNames walks the parked reader to the end of the workbook, which leaves it past every
      // sheet and useless as a first lease, so it is retired. The documented cost is one extra
      // reader open later; the requirement is that nothing else changes.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      _ = book.SheetNames;

      Assert.Equal(2, book.Sheet("Cover").Area.Size.Height);
      Assert.Equal(4, book.Sheet("Summary").Area.Size.Height);
      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);
      Assert.Equal("Alpha Fund", book.Sheet("Detail")[0, 1].GetString());
    }

    [Fact]
    public void ASheetAheadOfTheOneAlreadyNamedIsReachable()
    {
      // The order that used to dead-end. Once the parked reader was adopted there was nothing left
      // to walk the catalogue with, so every sheet the first walk had not already passed became
      // permanently invisible — and the error said the sheet did not exist. Any reader can do the
      // walking now: a lease borrowed at the catalogue's edge steps forward from there.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      var summary = book.Sheet("Summary");
      var detail = book.Sheet("Detail");

      Assert.Equal(4, summary.Area.Size.Height);
      Assert.Equal(6, detail.Area.Size.Height);

      // Not just vended — read. A catalogue entry with the wrong index would hand back a view over
      // the wrong sheet, which an Area alone would not catch.
      Assert.Equal("Alpha Fund", summary[0, 1].GetString());
      Assert.Equal("Fund", detail[0, 0].GetString());
      Assert.Equal(1500d, detail[2, 5].GetDouble());

      // And the catalogue really did grow: the third sheet is in it, without the walk that
      // SheetNames would have forced.
      Assert.Equal(new[] { "Cover", "Summary", "Detail" }, book.SheetNames.ToArray());
    }

    [Fact]
    public void WalkingOnToALaterSheetIsServedByTheReadersRatherThanByAnewOne()
    {
      // The walk is an ordinary forward read: the reader already parked in the workbook does it.
      // Vending a second sheet therefore costs no open at all, and what the reading afterwards costs
      // is one reach backwards — to Summary, which the walk to Detail has now moved past.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      var summary = book.Sheet("Summary");
      var detail = book.Sheet("Detail");

      Assert.Equal(1, book.ReaderStatistics.Opens);
      Assert.Equal(1, book.ReaderStatistics.ReadersOpen);

      _ = detail[0, 1];
      _ = summary[0, 1];

      var stats = book.ReaderStatistics;

      Assert.Equal(2, stats.Opens);
      Assert.Equal(0, stats.Reopens);
    }

    [Fact]
    public void TheLastSheetCanBeAskedForFirst()
    {
      // Skipping straight to the end records everything on the way, so the sheets before it cost
      // nothing afterwards. This order always worked; it is pinned beside its mirror image so the
      // pair reads as one rule — the catalogue grows in whichever direction it is asked to.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);
      Assert.Equal(1, book.ReaderStatistics.Opens);

      Assert.Equal("Alpha Fund", book.Sheet("Summary")[0, 1].GetString());
      Assert.Equal("Quarterly Pack", book.Sheet("Cover")[0, 0].GetString());
    }

    [Fact]
    public void EverySheetOfAWorkbookCanBeReadInAnyOrder()
    {
      // The general statement, over all six orders of three sheets: whichever way a caller names
      // them, every one vends and reads the same cells. One order failing out of six was the shape
      // of the bug, so the pin is the permutation rather than a case of it.
      var sheets = new[] { "Cover", "Summary", "Detail" };
      var heights = new Dictionary<string, int> { ["Cover"] = 2, ["Summary"] = 4, ["Detail"] = 6 };

      foreach (var order in Permutations(sheets))
      {
        using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

        foreach (var name in order)
          Assert.Equal(heights[name], book.Sheet(name).Area.Size.Height);

        Assert.Equal("Fund", book.Sheet("Detail")[0, 0].GetString());
      }
    }

    private static IEnumerable<string[]> Permutations(string[] values) =>
      values.Length == 1
        ? new[] { values }
        : values.SelectMany(
            value => Permutations(values.Where(other => other != value).ToArray()),
            (value, rest) => new[] { value }.Concat(rest).ToArray());

    [Fact]
    public void ASheetBehindTheOneAlreadyNamedIsStillReachable()
    {
      // The catalogue is built as the parked reader passes each sheet, so asking for the furthest
      // sheet first records the ones before it on the way and they cost nothing afterwards.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);
      Assert.Equal(4, book.Sheet("Summary").Area.Size.Height);
      Assert.Equal(2, book.Sheet("Cover").Area.Size.Height);
    }

    [Fact]
    public void AnUnknownSheetNamesTheWorkbookAndWhatWasSeen()
    {
      // "Sequence contains no elements" would tell a caller nothing about the file they opened or
      // the name they asked for. The names seen so far are the honest half of the answer: the walk
      // is lazy, so the message says what it knows rather than pretending to know the rest.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      var failure = Assert.Throws<ArgumentException>(() => book.Sheet("Nope"));

      Assert.Contains("No sheet named 'Nope'", failure.Message);
      Assert.Contains("multi-sheet.xlsx", failure.Message);
      Assert.Contains("Sheets seen so far", failure.Message);
    }

    [Fact]
    public void SheetNamesMatchWithoutRegardToCaseByDefault()
    {
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      Assert.Equal(6, book.Sheet("detail").Area.Size.Height);
      Assert.Equal(6, book.Sheet("DETAIL").Area.Size.Height);
    }

    [Fact]
    public void CaseSensitiveSheetNamesIsHonoured()
    {
      using var book = Workbook.Open(
        Path("multi-sheet.xlsx"),
        new WorkbookOptions { WarmReaders = false, CaseSensitiveSheetNames = true });

      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);
      Assert.Throws<ArgumentException>(() => book.Sheet("detail"));
    }

    // --- The one open at Open -----------------------------------------------------------------------

    [Fact]
    public void OpeningAndReadingOneSheetCostsOneReader()
    {
      // The lazy catalogue's whole purpose. Open parks a reader at sheet 0; Sheet(name) walks it to
      // the sheet asked for, recording what it passes, and then ADOPTS it — already open, already in
      // the right place. Walking the whole catalogue eagerly would give better errors and a free
      // SheetNames at the cost of a second multi-second open in the single-sheet case that is most
      // usage, which is the trade this number represents.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      _ = Column(4, c => c[0].GetString()).Map(book.Sheet("Report"));

      Assert.Equal(1, book.ReaderStatistics.Opens);
      Assert.Equal(1, book.ReaderStatistics.ReadersOpen);
    }

    [Fact]
    public void WarmingOpensASpareNobodyAskedFor()
    {
      // Two, by default, and NOT a regression. The two are the reader Open parked and Sheet then
      // ADOPTED, plus one warm spare opened on a background task so the first backward reach does
      // not have to pay five seconds for it. Neither of them is a second catalogue walk — the count
      // above shows the walk is free — and there is no third, because the slot the parked reader
      // will be adopted into is reserved and never a warm target.
      //
      // Waiting for the count rather than reading it once: warming is a background task, so "has
      // the spare arrived" is a question with a settling time. The wait fails the test if the spare
      // never comes and costs milliseconds when it does.
      using var book = Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions());

      _ = Column(4, c => c[0].GetString()).Map(book.Sheet("Report"));

      Assert.True(
        SpinWait.SpinUntil(() => book.ReaderStatistics.ReadersOpen == 2, TimeSpan.FromSeconds(10)),
        $"a spare reader should have been warmed; readers were {book.ReaderStatistics}");

      var stats = book.ReaderStatistics;

      Assert.Equal(2, stats.Opens);
      Assert.Equal(1, stats.SpareOpens);
      Assert.Equal(0, stats.Reopens);
    }

    [Fact]
    public void TheReaderThatWalksOnToALaterSheetIsTheOneThatThenReadsIt()
    {
      // Cross-sheet reuse, at the workbook level. Naming a sheet beyond the catalogue's edge walks a
      // reader forward to find it, and that reader is left standing exactly where the reading is
      // about to begin — so the rows of the second sheet cost nothing beyond the walk itself. This
      // is the argument for owning the readers at the book rather than at the sheet: a position is
      // a place in a WORKBOOK, and moving on to the next sheet is a forward move like any other.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      var summary = book.Sheet("Summary");

      for (var row = 0; row < summary.Area.Size.Height; row++)
        _ = summary[0, row];

      var detail = book.Sheet("Detail");
      var afterWalk = book.ReaderStatistics;

      for (var row = 0; row < detail.Area.Size.Height; row++)
        _ = detail[0, row];

      var afterReading = book.ReaderStatistics;

      Assert.Equal(afterWalk.Opens, afterReading.Opens);
      Assert.Equal(0, afterReading.Reopens);
      Assert.Equal("Alpha Fund", detail[0, 1].GetString());
      Assert.True(
        afterReading.RowsPerReader.Sum() > afterWalk.RowsPerReader.Sum(),
        "the reader already on Detail did the reading rather than a new one being opened");
    }

    [Fact]
    public void AWalkDownATallSheetEvictsAsItGoesAndReadsEveryRowOnce()
    {
      // The window doing its job on a real workbook. 1,201 rows in 64-row chunks is nineteen loads;
      // a four-chunk budget means fifteen of them are dropped again on the way down; and not one row
      // is read twice. This is the shape of every monotone parse, and the reason streaming costs
      // about a third more time for about a third of the memory.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold(windowRows: 256, chunkRows: 64));
      var space = book.Sheet("Ledger");

      for (var row = 0; row < space.Area.Size.Height; row++)
        _ = space[0, row];

      var walked = book.Statistics("Ledger")!.Value;

      Assert.Equal(19, walked.ChunkLoads);
      Assert.Equal(0, walked.ChunkReloads);
      Assert.Equal(15, walked.Evictions);
      Assert.Equal(4, walked.ResidentChunks);
      Assert.Equal(4, walked.PeakResidentChunks);
      Assert.Equal(1201, walked.RowsMaterialised);

      // ...and reaching back to a row the window has dropped costs exactly one reload.
      _ = space[0, 0];

      Assert.Equal(1, book.Statistics("Ledger")!.Value.ChunkReloads);
    }

    [Fact]
    public void AWalkDownASheetTallerThanTheWindowReportsOneOverrunThatCostNothing()
    {
      // Read this one carefully before "fixing" it.
      //
      // A view hands its extent down with every cell, and the root view's extent is the WHOLE
      // SHEET — 1,201 rows against a 256-row window. That band does not fit, which is precisely
      // what WindowOverruns counts, so a plain walk down a tall sheet reports one. Once, because
      // the counter is deduplicated on the extent rather than counted per cell.
      //
      // The natural assertion here is `Equal(0, WindowOverruns)`, and it is WRONG under the adopted
      // semantics. The counter says a band did not fit; ChunkReloads says what not fitting cost.
      // One overrun with zero reloads is the honest reading of a monotone walk: the root extent
      // could not be held, and because nothing ever swept it twice, holding it would have bought
      // nothing. The pair is the diagnostic — overruns WITH reloads is the collapse worth acting on.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold(windowRows: 256, chunkRows: 64));
      var space = book.Sheet("Ledger");

      for (var row = 0; row < space.Area.Size.Height; row++)
        _ = space[0, row];

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(1, stats.WindowOverruns);
      Assert.Equal(0, stats.ChunkReloads);
    }

    [Fact]
    public void AWalkDownASheetThatFitsTheWindowReportsNoOverrunAtAll()
    {
      // The other side of the same rule, so the pair cannot be read as "overruns are meaningless".
      // A sheet small enough to be held whole is a band that fits, and nothing is reported.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");

      for (var row = 0; row < space.Area.Size.Height; row++)
        _ = space[0, row];

      var stats = book.Statistics("Report")!.Value;

      Assert.Equal(0, stats.WindowOverruns);
      Assert.Equal(0, stats.ChunkReloads);
    }

    // --- Statistics ----------------------------------------------------------------------------------

    [Fact]
    public void StatisticsAreNullUntilASheetIsVended()
    {
      // A sheet nobody asked for has no story to tell, and a zeroed struct would be a lie that
      // reads like data.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      Assert.Null(book.Statistics("Report"));

      var space = book.Sheet("Report");
      _ = space[0, 0];

      var stats = book.Statistics("Report");

      Assert.NotNull(stats);
      Assert.Equal("Report", stats!.Value.SheetName);
      Assert.True(stats.Value.RowsMaterialised > 0);
    }

    [Fact]
    public void StatisticsForASheetThatDoesNotExistAreNullRatherThanAFailure()
    {
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      Assert.Null(book.Statistics("No Such Sheet"));
    }

    [Fact]
    public void TheWindowIsSizedFromTheOptionsInRows()
    {
      // The knob a caller turns is rows; chunks are what the store thinks in. This is where the two
      // meet, and the floor of four chunks is visible in it.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold(windowRows: 256, chunkRows: 64));
      var space = book.Sheet("Ledger");
      _ = space[0, 0];

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(64, stats.ChunkRows);
      Assert.Equal(4, stats.WindowChunks);
      Assert.Equal(256, stats.WindowRows);
    }

    // --- Lifetime --------------------------------------------------------------------------------------

    [Fact]
    public void AViewReadAfterDisposeThrows_EvenWhereItsRowsAreStillInMemory()
    {
      // A view is undisposable and outlives nothing: the only thing that invalidates it is the
      // workbook going away. The check runs before the resident fast path so this does not depend
      // on whether the window happens still to hold the row.
      var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");

      _ = space[0, 0];        // the chunk is now resident

      book.Dispose();

      Assert.Throws<ObjectDisposedException>(() => space[0, 0]);
    }

    [Fact]
    public void AMapOverADisposedViewFailsAsAFaultRatherThanAsAbsentData()
    {
      // The correctness fix, at the workbook level. A tolerance boundary absorbs failures about the
      // SHAPE of the data; a view outliving its workbook is not one of those, and reporting it as
      // "the section is absent" would turn a lifetime bug into a quietly wrong answer.
      var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");
      book.Dispose();

      var declaration = Column(4, c => c[0].GetString()).Named("header");

      var direct = Assert.Throws<ShapeException>(() => declaration.Map(space));
      Assert.IsType<ObjectDisposedException>(direct.GetBaseException());

      var tolerated = Assert.Throws<ShapeException>(() => declaration.Optional().Map(space));
      Assert.IsType<ObjectDisposedException>(tolerated.GetBaseException());
    }

    [Fact]
    public void UsingADisposedWorkbookThrows()
    {
      var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      book.Dispose();

      Assert.Throws<ObjectDisposedException>(() => book.Sheet("Report"));
      Assert.Throws<ObjectDisposedException>(() => _ = book.SheetNames);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
      var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      book.Dispose();
      book.Dispose();
    }

    [Fact]
    public void PathIsTheFileItReads()
    {
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      Assert.Equal(Path("simple-report.xlsx"), book.Path);
    }

    // --- Concurrency ------------------------------------------------------------------------------------

    [Fact]
    public void MapsOverTwoSheetsOfOneWorkbookAgreeWithRunningThemInTurn()
    {
      // Different sheets of one book share a reader pool but not a store, so they load in parallel
      // and only lease SELECTION serialises. What has to be true is not that it is fast but that it
      // is the same answer either way.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"));

      var summary = book.Sheet("Summary");
      var detail = book.Sheet("Detail");

      var declaration = TableRows(row => row[0].GetString());

      var serial = new[] { declaration.Map(summary), declaration.Map(detail) };

      using var second = Workbook.Open(Path("multi-sheet.xlsx"));
      var parallelSummary = second.Sheet("Summary");
      var parallelDetail = second.Sheet("Detail");
      var parallel = new IReadOnlyList<string>[2];

      Parallel.Invoke(
        () => parallel[0] = declaration.Map(parallelSummary),
        () => parallel[1] = declaration.Map(parallelDetail));

      Assert.Equal(serial[0], parallel[0]);
      Assert.Equal(serial[1], parallel[1]);
    }

    [Fact]
    public void ManyThreadsReadingOneSheetSeeTheSameCells()
    {
      // Maps over ONE sheet serialise on that sheet's store gate — a documented v1 limitation, not
      // a correctness one. What must hold is that serialising is all that happens: no torn read, no
      // chunk installed twice, no thread seeing a cell another thread was loading.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold(windowRows: 256, chunkRows: 64));
      var space = book.Sheet("Ledger");

      Parallel.For(0, 16, worker =>
      {
        for (var row = 1; row <= 200; row++)
          Assert.Equal(row, space[0, row].GetInt());
      });
    }

    // --- Options ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AWindowOfNoRowsIsRejected(int windowRows)
    {
      Assert.Throws<ArgumentOutOfRangeException>(
        () => Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { WindowRows = windowRows }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AWorkbookWithNoReadersIsRejected(int maxReaders)
    {
      Assert.Throws<ArgumentOutOfRangeException>(
        () => Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { MaxReaders = maxReaders }));
    }

    [Fact]
    public void ANegativeChunkSizeIsRejected_ButZeroMeansDeriveIt()
    {
      var failure = Assert.Throws<ArgumentOutOfRangeException>(
        () => Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { ChunkRows = -1 }));

      Assert.Contains("pass 0 to derive it", failure.Message);

      using var book = Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { WarmReaders = false, ChunkRows = 0 });
      var space = book.Sheet("Report");
      _ = space[0, 0];

      Assert.Equal(SheetStore.DefaultChunkRows(space.Area.Size.Width), book.Statistics("Report")!.Value.ChunkRows);
    }

    [Fact]
    public void OpenRejectsNulls()
    {
      Assert.Throws<ArgumentNullException>(() => Workbook.Open(null!));
      Assert.Throws<ArgumentNullException>(() => Workbook.Open(Path("simple-report.xlsx"), null!));

      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      Assert.Throws<ArgumentNullException>(() => book.Sheet(null!));
      Assert.Throws<ArgumentNullException>(() => book.Statistics(null!));
    }

    [Fact]
    public void IsBlankDecidesWhatCountsAsEmpty_JustAsTheEagerPathDoes()
    {
      // Blankness belongs to the adapter, and the workbook's adapter is the row source. The default
      // treats whitespace-only text as blank; strict fidelity is one option away, and it changes
      // what a discovered extent finds — which is the point of the knob.
      using var lenient = Workbook.Open(Path("edge-cases.xlsx"), Cold());
      using var strict = Workbook.Open(
        Path("edge-cases.xlsx"),
        new WorkbookOptions { WarmReaders = false, IsBlank = _ => false });

      Assert.True(lenient.Sheet("Edges")[0, 2].IsBlank);
      Assert.Equal("  ", strict.Sheet("Edges")[0, 2].GetString());
    }
  }
}
