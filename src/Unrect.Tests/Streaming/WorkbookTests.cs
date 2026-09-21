using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

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
    private static WorkbookOptions Cold() => new WorkbookOptions();

    /// <summary>The tall ledger's body, one band per row: what a forward pass streams without holding.</summary>
    private static IProjectionDefinition<ICellSpace, IReadOnlyList<string>> LedgerRows()
      => On(RowContaining("Entry")).Of(Table(headerRows: 1, eachRow: Row(3, cells => cells[2].AsText()!)));

    // --- Vending ----------------------------------------------------------------------------------

    [Fact]
    public void ASheetReadsAsTheEagerPathReadsIt()
    {
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var streamed = book.Sheet("Report");
      var eager = SpreadsheetSpace.Create(Path("simple-report.xlsx"), "Report");

      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal("Capital Activity Report", streamed.AsText(0, 0));
    }

    [Fact]
    public void SheetTwiceIsTwoPassesThatReadTheSameThing()
    {
      // Each call is a fresh pass over its own cursor: a second declaration over an already-open
      // book reads the sheet again, from the top, and gets the same answer.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var declaration = Column(4, c => c[0].Text());

      var first = declaration.Map(book.Sheet("Report"));
      var second = declaration.Map(book.Sheet("Report"));

      Assert.Equal(first, second);

      // Four rows taken, and the fifth loaded to be offered and refused: a rule that stops sees
      // the row it stops at.
      Assert.Equal(5, book.Statistics("Report")!.Value.RowsRead);
    }

    [Fact]
    public void ASheetIsOnePassAndASecondMapOverItIsRefusedAsARead()
    {
      // The pass releases rows behind the declaration as it goes, so the same sheet value driven
      // again cannot go back: the failure says so, and says to ask the workbook again.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold());
      var sheet = book.Sheet("Ledger");

      _ = LedgerRows().Map(sheet);

      var failure = Assert.Throws<ProjectionException>(() => Column(4, c => c[0].AsText()).Map(sheet));

      Assert.Contains("has left the buffer", failure.Message);
    }

    [Fact]
    public void AViewIsAValueRatherThanAHandle()
    {
      // Sheet() hands back a new pass each time, each over its own cursor; what they read is the
      // same rows.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      var first = book.Sheet("Report");
      var second = book.Sheet("Report");

      Assert.NotSame(first, second);
      Assert.Equal(first.AsText(0, 0), second.AsText(0, 0));
      Assert.Equal(first.Describe(0, 0), second.Describe(0, 0));
    }

    [Fact]
    public void ASlicedViewSharesTheStoreAndReadsTheRightCells()
    {
      // Slicing is free and slices share the pass, so a declaration that decomposes a sheet into
      // a hundred regions still reads it once.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var sheet = Plane<ICellSpace>.Of(book.Sheet("Report"));
      var slice = sheet.Slice(new Offset(0, 5), new Area(4, 5));

      Assert.Equal(sheet[0, 5], slice[0, 0]);
      Assert.Equal(sheet[2, 7], slice[2, 2]);

      var nested = slice.Slice(new Offset(1, 1), new Area(2, 2));

      Assert.Equal(sheet[1, 6], nested[0, 0]);

      // ...and the cells really are read: naming a cell costs nothing at all, so the claim above
      // would hold vacuously over a sheet nobody had touched.
      Assert.Equal(sheet[1, 6].AsText(), nested[0, 0].AsText());
      Assert.Equal(sheet[2, 7].AsText(), slice[2, 2].AsText());
      Assert.Equal(8, book.Statistics("Report")!.Value.RowsRead);
    }

    [Fact]
    public void AnIndexPastTheEndOfAViewIsABoundsCondition()
    {
      // OutOfBoundsException and not IndexOutOfRangeException, deliberately: the engine's fault
      // list classifies the latter as a bug in the reading code, non-absorbable — while running off
      // the end of a space is an ordinary bounds condition a declaration is allowed to recover
      // from.
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");

      Assert.Throws<OutOfBoundsException>(() => space.AsText(-1, 0));
      Assert.Throws<OutOfBoundsException>(() => space.AsText(space.Area.Size.Width, 0));
      Assert.Throws<OutOfBoundsException>(() => space.AsText(0, space.Area.Size.Height));
      Assert.Throws<OutOfBoundsException>(
        () => Plane<ICellSpace>.Of(space).Slice(new Offset(0, 0), new Area(99, 99)));
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
      Assert.Equal("Alpha Fund", book.Sheet("Detail").AsText(0, 1));
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
      Assert.Equal("Alpha Fund", summary.AsText(0, 1));
      Assert.Equal("Fund", detail.AsText(0, 0));
      Assert.Equal(1500d, Plane<ICellSpace>.Of(detail)[2, 5].Double());

      // And the catalogue really did grow: the third sheet is in it, without the walk that
      // SheetNames would have forced.
      Assert.Equal(new[] { "Cover", "Summary", "Detail" }, book.SheetNames.ToArray());
    }

    [Fact]
    public void TheLastSheetCanBeAskedForFirst()
    {
      // Skipping straight to the end records everything on the way, so the sheets before it cost
      // nothing afterwards. This order always worked; it is pinned beside its mirror image so the
      // pair reads as one rule — the catalogue grows in whichever direction it is asked to.
      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);

      Assert.Equal("Alpha Fund", book.Sheet("Summary").AsText(0, 1));
      Assert.Equal("Quarterly Pack", book.Sheet("Cover").AsText(0, 0));
    }

    [Fact]
    public void EverySheetOfAWorkbookCanBeReadInAnyOrder()
    {
      // The general statement, over all six orders of three sheets: whichever way a caller names
      // them, every one vends and reads the same cells. One order failing out of six was the
      // projection of the bug, so the pin is the permutation rather than a case of it.
      var sheets = new[] { "Cover", "Summary", "Detail" };
      var heights = new Dictionary<string, int> { ["Cover"] = 2, ["Summary"] = 4, ["Detail"] = 6 };

      foreach (var order in Permutations(sheets))
      {
        using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());

        foreach (var name in order)
          Assert.Equal(heights[name], book.Sheet(name).Area.Size.Height);

        Assert.Equal("Fund", book.Sheet("Detail").AsText(0, 0));
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
        new WorkbookOptions { CaseSensitiveSheetNames = true });

      Assert.Equal(6, book.Sheet("Detail").Area.Size.Height);
      Assert.Throws<ArgumentException>(() => book.Sheet("detail"));
    }

    // --- The one open at Open -----------------------------------------------------------------------

    [Fact]
    public void AWalkDownATallSheetReleasesAsItGoesAndReadsEveryRowOnce()
    {
      // The pass doing its job on a real workbook: 1,201 rows read once, and at no point more than
      // a handful held — the row in hand, the band being placed, nothing behind them. This is the
      // shape of every monotone parse, and the reason a forward pass costs so little memory.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold());
      var space = book.Sheet("Ledger");

      Assert.Equal(1200, LedgerRows().Map(space).Count);

      var walked = book.Statistics("Ledger")!.Value;

      Assert.Equal(1201, walked.RowsRead);
      Assert.True(walked.PeakRetained < 16, $"peak retained {walked.PeakRetained}");

      // ...and reaching back to a row the pass has released is a read failure, not a reload.
      Assert.Throws<CellReadException>(() => space.AsText(0, 0));
    }

    [Fact]
    public void ADeclarationThatHoldsTheSheetIsFaultedByTheCap()
    {
      // A column lambda reads its whole extent at random, so the pass has to hold every row it is
      // offered; a cap below the sheet's height is a fault naming the shape that holds.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), new WorkbookOptions { BufferRows = 100 });

      var failure = Assert.Throws<ProjectionException>(() => Column(row => row.Count).Map(book.Sheet("Ledger")));

      Assert.True(failure.IsFault);
      Assert.Contains("Column is holding", failure.Message);
      Assert.Contains("more than the 100 the source allows", failure.Message);
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
      _ = space.AsText(0, 0);

      var stats = book.Statistics("Report");

      Assert.NotNull(stats);
      Assert.Equal("Report", stats!.Value.SheetName);
      Assert.Equal(1, stats.Value.RowsRead);
    }

    [Fact]
    public void StatisticsForASheetThatDoesNotExistAreNullRatherThanAFailure()
    {
      using var book = Workbook.Open(Path("simple-report.xlsx"), Cold());

      Assert.Null(book.Statistics("No Such Sheet"));
    }

    // --- Lifetime --------------------------------------------------------------------------------------

    [Fact]
    public void AViewReadAfterDisposeThrows_EvenWhereItsRowsAreStillInMemory()
    {
      // A sheet is undisposable and outlives nothing: the only thing that invalidates it is the
      // workbook going away. The check runs before the buffer is consulted, so this does not depend
      // on whether the row happens still to be held.
      var book = Workbook.Open(Path("simple-report.xlsx"), Cold());
      var space = book.Sheet("Report");

      _ = space.AsText(0, 0);        // the row is now held

      book.Dispose();

      Assert.Throws<ObjectDisposedException>(() => space.AsText(0, 0));
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

      var declaration = Column(4, c => c[0].Text()).Named("header");

      var direct = Assert.Throws<ProjectionException>(() => declaration.Map(space));
      Assert.IsType<ObjectDisposedException>(direct.GetBaseException());

      var tolerated = Assert.Throws<ProjectionException>(() => declaration.Optional().Map(space));
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

      var declaration = Table(row => row[0].Text());

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
    public void ManyThreadsEachReadingTheirOwnPassSeeTheSameCells()
    {
      // A pass is one consumer's; many threads over one workbook each ask for their own, and share
      // only the string table. What must hold is that nothing is torn: every pass reads every cell.
      using var book = Workbook.Open(Path("tall-ledger.xlsx"), Cold());

      Parallel.For(0, 16, worker =>
      {
        var sheet = Plane<ICellSpace>.Of(book.Sheet("Ledger"));

        for (var row = 1; row <= 200; row++)
          Assert.Equal(row, sheet[0, row].Integer());
      });
    }

    // --- Options ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ABufferCapOfNoRowsIsRejected(int bufferRows)
    {
      Assert.Throws<ArgumentOutOfRangeException>(
        () => Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { BufferRows = bufferRows }));
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
        new WorkbookOptions { IsBlank = _ => false });

      Assert.True(lenient.Sheet("Edges").IsBlank(0, 2));
      Assert.Equal("  ", strict.Sheet("Edges").AsText(0, 2));
    }
  }
}
