using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The acceptance test for the whole feature: the two doors differ in the shape of their cost and
  /// in nothing else.
  /// <para>
  /// Every assertion here is a differential one — the same declaration, the same workbook, read
  /// eagerly and through a window, compared. That form is deliberate: it needs no expected values
  /// of its own, so it cannot drift away from what the eager path means, and any divergence is by
  /// construction a streaming bug rather than a stale fixture.
  /// </para>
  /// </summary>
  public class StreamingIdentityTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary>Every committed workbook, and the sheet in it worth reading.</summary>
    public static TheoryData<string, string> Workbooks => new TheoryData<string, string>
    {
      { "simple-report.xlsx", "Report" },
      { "investors-by-deal.xlsx", "Investors" },
      { "investor-summary.xlsx", "Summary" },
      { "investor-irr.xlsx", "IRR" },
      { "edge-cases.xlsx", "Edges" },
      { "multi-sheet.xlsx", "Detail" },
      { "tall-ledger.xlsx", "Ledger" },
      { "no-extent.xlsx", "Undeclared" },
      { "repeated-text.xlsx", "Ledger" },
    };

    [Theory]
    [MemberData(nameof(Workbooks))]
    public void EveryCellOfEveryWorkbookReadsTheSameThroughAWindow(string file, string sheet)
    {
      var eager = SpreadsheetSpace.Create(Path(file), sheet);
      using var book = Workbook.Open(Path(file), new WorkbookOptions { WarmReaders = false });
      var streamed = book.Sheet(sheet);

      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);

      AssertEveryCellAgrees(eager, streamed);
    }

    [Theory]
    [MemberData(nameof(Workbooks))]
    public void EveryCellStillReadsTheSameThroughAWindowFarTooSmallForIt(string file, string sheet)
    {
      // One row per chunk, floored to the four-chunk minimum: a window deliberately far below the
      // sizing law. An undersized window is slow — it re-reads, and the counters say so — and it is
      // never wrong. That is the property that makes WindowRows a performance knob rather than a
      // correctness one.
      var eager = SpreadsheetSpace.Create(Path(file), sheet);
      using var book = Workbook.Open(
        Path(file),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 1, WindowRows = 1 });
      var streamed = book.Sheet(sheet);

      AssertEveryCellAgrees(eager, streamed);

      Assert.Equal(1, book.Statistics(sheet)!.Value.ChunkRows);
    }

    [Fact]
    public void BothDoorsMeasureASheetThatWillNotSayHowBigItIsTheSameWay()
    {
      // Named rather than left to the theory above, because this is the one file the differential
      // form cannot carry on its own. Its cell loop is vacuous — a zero-wide space has no cells to
      // compare — so the whole law is in the extent, and an extent the two doors agree on could
      // still be agreed nonsense. Hence the only literal in this class: four rows are really in the
      // file. The streaming door has measured such a sheet since it shipped; the eager door sized
      // its grid from the counts the reader would not give and yielded an empty space for a file
      // with four rows in it. Both now read the sheet to find out, and get the same answer.
      var eager = SpreadsheetSpace.Create(Path("no-extent.xlsx"), "Undeclared");
      using var book = Workbook.Open(Path("no-extent.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = book.Sheet("Undeclared");

      Assert.Equal(4, eager.Area.Size.Height);
      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);

      // The measure is where that answer came from, and the streaming door says so out loud — which
      // also guards the fixture. A regenerated no-extent.xlsx that described itself again would
      // still pass every assertion above, by the ordinary declared path, and would have stopped
      // testing anything; a survey of zero rows here says so.
      Assert.Equal(4, book.Statistics("Undeclared")!.Value.RowsMeasured);
    }

    [Theory]
    [MemberData(nameof(Workbooks))]
    public void EveryWorkbookSharesItsRepeatedTextTheSameWayThroughAWindow(string file, string sheet)
    {
      // The differential form applied to identity rather than to value. Each door is asked, for
      // every text cell of a sheet, which earlier cell it shares its characters with — and the two
      // answers must be the same list. That is the strongest form of "the doors differ in nothing a
      // caller can observe": it is not enough that the cells are equal, because a caller who holds
      // a grid pays for the instances, and the two doors keep separate tables with separate guards
      // that could drift apart. repeated-text.xlsx is the case with something to say (its 256- and
      // 257-character neighbours land on opposite sides of the guard, and both doors must put them
      // there); the rest are the control.
      var eager = SpreadsheetSpace.Create(Path(file), sheet);
      using var book = Workbook.Open(Path(file), new WorkbookOptions { WarmReaders = false });
      var streamed = book.Sheet(sheet);

      Assert.Equal(SharingPattern(eager), SharingPattern(streamed));
    }

    /// <summary>
    /// For each cell in reading order, the position of the first cell holding the same string
    /// INSTANCE — itself for a first sighting, and -1 for a cell that is not text at all. Two
    /// spaces with the same pattern share exactly the same values as each other.
    /// </summary>
    private static IReadOnlyList<int> SharingPattern(ISheetCells space)
    {
      // Reference equality on purpose: the question is which instance a cell points at, and the
      // default comparer would answer the one this test is not asking.
      var seen = new Dictionary<object, int>(ByReference.Instance);
      var pattern = new List<int>();

      for (var row = 0; row < space.Area.Size.Height; row++)
        for (var column = 0; column < space.Area.Size.Width; column++)
        {
          if (!space.IsText(column, row) || space.AsText(column, row) is not string text)
          {
            pattern.Add(-1);
            continue;
          }

          if (!seen.TryGetValue(text, out var first))
            seen[text] = first = pattern.Count;

          pattern.Add(first);
        }

      return pattern;
    }

    // --- The flagship declaration ---------------------------------------------------------------------
    //
    // investor-irr.xlsx read by the projection the example tests use: a VerticalFlow of a Column, a
    // Table and two Repeats under captions, one of them Until-bounded. It reaches backwards
    // (the second series anchors on the caption that bounded the first), it makes several passes,
    // and it consumes the whole sheet — which is to say it exercises the pool, the window and the
    // diagnostics in one declaration. If streaming can read this, it can read a report.

    private static IProjectionDefinition<ISheetCells, (string Title, IReadOnlyList<string> Summary, IReadOnlyList<IReadOnlyList<string>> ByTransferDate, IReadOnlyList<IReadOnlyList<string>> ByInception)> InvestorIrr()
    {
      var investorBlock = Table(row => row["Investor Name"].Text()).Named("investor block");
      var series = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      const string Inception = "Cash Flows using inception date";

      return VerticalFlow(v =>
      {
        var columnSlot = v.Next(Column(4, column => column[0].Text()).Named("report header"));
        var table = v.Next(Table(row => row["Investors"].Text()).Named("summary"));
        var until = v.Next(Until(RowContaining(Inception)).Heading("IRR Details").Heading("Cash Flows Using Transfer Date").Of(series));
        var heading = v.Next(Heading(Inception).Of(series));

        return v.Build(read => (
          Title: read.Of(columnSlot),
          Summary: read.Of(table),
          ByTransferDate: read.Of(until),
          ByInception: read.Of(heading)));
      });
    }

    [Fact]
    public void TheFlagshipDeclarationProjectsTheSameValuesFromBothDoors()
    {
      var declaration = InvestorIrr();

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("investor-irr.xlsx"), "IRR"));

      using var book = Workbook.Open(Path("investor-irr.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = declaration.Map(book.Sheet("IRR"));

      Assert.Equal(eager.Title, streamed.Title);
      Assert.Equal(eager.Summary, streamed.Summary);
      Assert.Equal(
        eager.ByTransferDate.Select(block => block.ToArray()).ToArray(),
        streamed.ByTransferDate.Select(block => block.ToArray()).ToArray());
      Assert.Equal(
        eager.ByInception.Select(block => block.ToArray()).ToArray(),
        streamed.ByInception.Select(block => block.ToArray()).ToArray());
    }

    [Fact]
    public void TheFlagshipDeclarationConsumesTheSameExtentAndReportsTheSameDiagnostics()
    {
      // Not just the values: the same extent consumed and the same diagnostics, in order. A
      // streaming read that quietly consumed less would still produce the right answer here and be
      // wrong about the sheet, and the unconsumed-space Info is what would have said so.
      var declaration = InvestorIrr();

      var eagerSpace = SpreadsheetSpace.Create(Path("investor-irr.xlsx"), "IRR");
      var eager = declaration.MapWithDiagnostics(eagerSpace);
      var eagerExtent = declaration.Apply(eagerSpace);

      using var book = Workbook.Open(Path("investor-irr.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamedSpace = book.Sheet("IRR");
      var streamed = declaration.MapWithDiagnostics(streamedSpace);
      var streamedExtent = declaration.Apply(streamedSpace);

      Assert.Equal(eagerExtent.Consumed.Width, streamedExtent.Consumed.Width);
      Assert.Equal(eagerExtent.Consumed.Height, streamedExtent.Consumed.Height);
      Assert.Equal(Describe(eager.Diagnostics), Describe(streamed.Diagnostics));
      Assert.Empty(streamed.Diagnostics);
    }

    [Fact]
    public void TheFlagshipDeclarationIsUnchangedByAWindowSmallerThanTheSheet()
    {
      // The backward-reaching, multi-pass projection against a window that cannot hold what it
      // sweeps. The counters move — that is the cost model working — and the answer does not.
      var declaration = InvestorIrr();

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("investor-irr.xlsx"), "IRR"));

      using var book = Workbook.Open(
        Path("investor-irr.xlsx"),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 1, WindowRows = 1 });
      var streamed = declaration.Map(book.Sheet("IRR"));

      Assert.Equal(eager.Title, streamed.Title);
      Assert.Equal(eager.Summary, streamed.Summary);
      Assert.Equal(eager.ByTransferDate.Count, streamed.ByTransferDate.Count);
      Assert.Equal(eager.ByInception.Count, streamed.ByInception.Count);

      var stats = book.Statistics("IRR")!.Value;

      Assert.True(stats.RowsMaterialised > 0);
      Assert.True(stats.PeakResidentChunks <= stats.WindowChunks);
    }

    // --- The other example declarations -----------------------------------------------------------------

    [Fact]
    public void ADeclarationOverRepeatedBlocksReadsTheSameThroughAWindow()
    {
      // investors-by-deal: repeating blocks separated by blank bands, which is the projection whose
      // termination depends on reading past the end of one block and into the next.
      var declaration = VerticalRepeat(
        VerticalFlow(v =>
        {
          var textCell = v.Next(TextCell());
          var table = v.Next(Table(row => row["Name"].Text()));

          return v.Build(read => (
            Deal: read.Of(textCell),
            Rows: read.Of(table)));
        }),
        separatedBy: BlankRows());

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("investors-by-deal.xlsx"), "Investors"));

      using var book = Workbook.Open(Path("investors-by-deal.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = declaration.Map(book.Sheet("Investors"));

      Assert.Equal(eager.Select(block => block.Deal), streamed.Select(block => block.Deal));
      Assert.Equal(
        eager.Select(block => block.Rows.ToArray()).ToArray(),
        streamed.Select(block => block.Rows.ToArray()).ToArray());
    }

    [Fact]
    public void ATableBoundByItsHeaderReadsTheSameThroughAWindow()
    {
      var declaration = VerticalFlow(v =>
      {
        var columnSlot = v.Next(Column(4, column => column[0].Text()));
        var table = v.Next(Table(row => (row["Client"].Text(), row["Amount"].Decimal())));

        return v.Build(read => (
          Header: read.Of(columnSlot),
          Rows: read.Of(table)));
      });

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("simple-report.xlsx"), "Report"));

      using var book = Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = declaration.Map(book.Sheet("Report"));

      Assert.Equal(eager.Header, streamed.Header);
      Assert.Equal(eager.Rows, streamed.Rows);
    }

    // --- The row-projection slot ----------------------------------------------------------------------------
    //
    // Table(0, eachRow) reads its body as a tiler of one-row bands, which is the walk the whole
    // streaming door is sized around: one band open at a time, each band the row after the last. So
    // the slot form is the declaration most worth reading through a window that cannot hold the
    // sheet, and the counters are where the claim is — a monotone walk must not reload a chunk,
    // whatever the window is set to.

    private sealed record LedgerEntry(int Entry, int Amount, string Category);

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<LedgerEntry>> Ledger()
    {
      var ledgerEntry = HorizontalFlow(h =>
      {
        var integer = h.Next(Integer());
        var integer2 = h.Next(Integer());
        var textSlot = h.Next(Text());

        return h.Build(read => new LedgerEntry(
          Entry: read.Of(integer),
          Amount: read.Of(integer2),
          Category: read.Of(textSlot)));
      });

      return Below(RowContaining("Entry")).Of(Table(headerRows: 0, eachRow: ledgerEntry));
    }

    [Fact]
    public void ATableOfRecordProjectionsReadsTheSameThroughAWindow()
    {
      var declaration = Ledger();

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("tall-ledger.xlsx"), "Ledger"));

      using var book = Workbook.Open(Path("tall-ledger.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = declaration.Map(book.Sheet("Ledger"));

      Assert.Equal(1200, eager.Count);
      Assert.Equal(eager, streamed);
    }

    [PullOnlyFact("the window, the pool and the sweep announcement retire with the pull interpreter")]
    public void AndCostsNoRereadingEvenThroughAWindowFourRowsTall()
    {
      // One row per chunk, floored to the four-chunk minimum: a window two orders of magnitude
      // smaller than the sheet, and 1,200 records read through it. ChunkReloads is the cost meter
      // and it must be zero — a record projection is handed one band at a time and never reaches
      // back, so nothing the window dropped is ever wanted again.
      var declaration = Ledger();

      using var book = Workbook.Open(
        Path("tall-ledger.xlsx"),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 1, WindowRows = 1 });

      var streamed = declaration.Map(book.Sheet("Ledger"));

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(1200, streamed.Count);
      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(1201, stats.RowsMaterialised);      // every row once, and not one of them twice

      // One overrun, not two, and the missing one is the point. The band is announced once per
      // PLACEMENT now (ISweepAware), where the engine cuts the region, instead of riding down on
      // every cell read attached to whatever subspace object happened to make it. So what is counted
      // is what a placement DECLARED: the table's own discovered band, taller than four rows,
      // costing nothing. The whole-sheet extent is not a declared band — it is the space the
      // placement was resolved AGAINST, which the landmark scan reads through to find the header
      // row — so it is announced nowhere and counted nowhere.
      //
      // ChunkReloads 0 and RowsMaterialised 1201 above are unchanged, which is the half that makes
      // this a re-pin rather than a regression: the announcement that went away bought no residency.
      Assert.Equal(1, stats.WindowOverruns);
    }

    /// <summary>
    /// The same ledger read as a HEADERED table, so the composition the slot rung is built from —
    /// a header read once, then the body tiled beneath it — is the thing under the window.
    /// </summary>
    private static IProjectionDefinition<ISheetCells, IReadOnlyList<LedgerEntry>> HeaderedLedger()
    {
      var ledgerEntry = HorizontalFlow(h =>
      {
        var integer = h.Next(Integer());
        var integer2 = h.Next(Integer());
        var textSlot = h.Next(Text());

        return h.Build(read => new LedgerEntry(
          Entry: read.Of(integer),
          Amount: read.Of(integer2),
          Category: read.Of(textSlot)));
      });

      return On(RowContaining("Entry")).Of(Table(headerRows: 1, eachRow: ledgerEntry));
    }

    [Fact]
    public void AndAHeaderedOneCostsNoRereadingEither()
    {
      // The header is the part that could have cost a reload: it is read once, before the body, and
      // the body then walks forward from the row after it. If the composition reached back for its
      // captions per record — or measured the block before projecting — this counter would say so.
      var declaration = HeaderedLedger();

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("tall-ledger.xlsx"), "Ledger"));

      using var book = Workbook.Open(
        Path("tall-ledger.xlsx"),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 1, WindowRows = 1 });

      var streamed = declaration.Map(book.Sheet("Ledger"));

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(1200, streamed.Count);
      Assert.Equal(eager, streamed);
      Assert.Equal(0, stats.ChunkReloads);
    }

    // --- A band sweep, which is what the residency law was written for --------------------------------

    [Fact]
    public void ABandSweptAcrossByAHorizontalFlowCostsNoReloading()
    {
      // The case plain LRU gets wrong, and the reason the locus exists at all. A HorizontalFlow over
      // a band reads it once per child, and the order the children read in is not something the
      // store gets to choose — so a band that FITS the window must survive being swept across, and
      // ChunkReloads is how that is said. A window of 64 rows over a three-row band leaves room to
      // spare; a reload here would mean the store had dropped a chunk of the band it was told about.
      //
      // The whole sheet is 1,201 rows, so this is not a declaration that happens to fit by reading
      // everything: it is a bounded band inside a sheet two orders of magnitude bigger.
      var band = On(RowContaining("Entry")).Sized(AreaStrategies.ExplicitArea(3, 3)).Of(
        HorizontalFlow(h =>
        {
          var columnSlot = h.Next(Column(3, c => c.Count));
          var columnSlot2 = h.Next(Column(3, c => c.Count));
          var columnSlot3 = h.Next(Column(3, c => c.Count));

          return h.Build(read => $"{read.Of(columnSlot)}|{read.Of(columnSlot2)}|{read.Of(columnSlot3)}");
        }));

      using var book = Workbook.Open(
        Path("tall-ledger.xlsx"),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 16, WindowRows = 64 });

      Assert.Equal("3|3|3", band.Map(book.Sheet("Ledger")));

      var stats = book.Statistics("Ledger")!.Value;

      Assert.Equal(0, stats.ChunkReloads);
      Assert.Equal(0, stats.WindowOverruns);
    }

    [PullOnlyFact("the window, the pool and the sweep announcement retire with the pull interpreter")]
    public void AndAnOversizedBandWrappedThreeDeepIsStillOneOverrun()
    {
      // The counter reports the declaration's SHAPE rather than its spelling — the hazard being that
      // every transparent wrapper places the same region again, so one band arrives at the store
      // once per wrapper. Three wrappers inside the declared extent here, and one overrun: the band
      // that does not fit is one thing for a caller to fix, whatever it is written as.
      //
      // The wrappers sit INSIDE the pipeline's Sized, which is what makes them wrappers over THIS
      // band rather than over the whole sheet: a wrapper carrying no geometry of its own is handed
      // the region its parent settled, so each of them announces 3x200 again, consecutively. Written
      // the other way round — the geometry on the inner shape, the wrappers outside it — they would
      // each announce the whole sheet, which is a second oversized band and correctly a second
      // overrun; the counter is about bands, and that really is two of them.
      //
      // The control is the unwrapped declaration, so the number is a property of the band and not of
      // this particular chain.
      var read = Range(block => $"{block.Width}x{block.Height}");

      Assert.Equal(1, Overruns(Sized(AreaStrategies.ExplicitArea(3, 200)).Of(read)));
      Assert.Equal(1, Overruns(Sized(AreaStrategies.ExplicitArea(3, 200)).Of(read.Optional().Padded(0).Named("wrapped"))));
    }

    /// <summary>The overruns a declaration costs over the tall ledger, through a window it does not fit.</summary>
    private static long Overruns<T>(IProjectionDefinition<ISheetCells, T> declaration)
    {
      using var book = Workbook.Open(
        Path("tall-ledger.xlsx"),
        new WorkbookOptions { WarmReaders = false, ChunkRows = 16, WindowRows = 64 });

      declaration.Map(book.Sheet("Ledger"));

      return book.Statistics("Ledger")!.Value.WindowOverruns;
    }

    // --- Failures are identical too -----------------------------------------------------------------------

    [Fact]
    public void ADeclarationThatFailsFailsWithTheSameMessageFromBothDoors()
    {
      // A diagnostic that named a different cell depending on which door was used would be worse
      // than useless: the message is what a caller acts on, and it has to describe the workbook
      // rather than the reader.
      var declaration = IntCell().Named("a number");

      var eager = Assert.Throws<ProjectionException>(
        () => declaration.Map(SpreadsheetSpace.Create(Path("edge-cases.xlsx"), "Edges")));

      using var book = Workbook.Open(Path("edge-cases.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = Assert.Throws<ProjectionException>(() => declaration.Map(book.Sheet("Edges")));

      Assert.Equal(eager.Message, streamed.Message);
      Assert.Equal(eager.Location.ToString(), streamed.Location.ToString());
      Assert.Equal(eager.Path, streamed.Path);
    }

    [Fact]
    public void AnErrorCellReadsAsAnErrorThroughAWindow()
    {
      // The adapter is the same one either way — the row source applies blankness and adapts kinds
      // exactly as the eager reader does — and an error cell is the sharpest test of that, because
      // it is the kind a careless adapter turns into a Blank.
      using var book = Workbook.Open(Path("edge-cases.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = book.Sheet("Edges");

      Assert.True(streamed.IsErrorAt(0, 1));
      Assert.Equal("#VALUE!", streamed.AsText(0, 1));
      Assert.Equal("Error(#VALUE!)", streamed.Describe(0, 1));
      Assert.False(streamed.IsBlank(0, 1));
    }

    /// <summary>
    /// Every cell of two doors onto one file, compared on everything a reader can ask a cell: what
    /// kind of thing it is, whether it says anything, whether what it says is its own, and what that
    /// is. A whole-cell equality said all four at once; four questions say them one at a time and
    /// name the one that differs.
    /// </summary>
    /// <summary>
    /// Every member of <see cref="ISheetCells"/>, at both doors, for every cell — the canonical four,
    /// the error queries, and all six kinded reads including the sentence a refused one produces.
    /// <para>
    /// The whole interface rather than the canonical part of it, because the interface is the
    /// contract and a door that agreed about renderings while disagreeing about kinds would satisfy
    /// half a law. The six kinded reads are asked of EVERY cell, so most of what is compared is the
    /// refusals — which is the useful half: a reading that succeeds at both doors agrees trivially,
    /// and a reading that fails has a sentence, a kind and an A1 to disagree about.
    /// </para>
    /// </summary>
    private static void AssertEveryCellAgrees(ISheetCells eager, ISheetCells streamed)
    {
      for (var row = 0; row < eager.Area.Size.Height; row++)
        for (var column = 0; column < eager.Area.Size.Width; column++)
        {
          Assert.Equal(eager.Describe(column, row), streamed.Describe(column, row));
          Assert.Equal(eager.IsBlank(column, row), streamed.IsBlank(column, row));
          Assert.Equal(eager.IsText(column, row), streamed.IsText(column, row));
          Assert.Equal(eager.AsText(column, row), streamed.AsText(column, row));

          // The error queries, which are the two ISheetCells members no leaf reads: IsErrorAt asks
          // whether the cell IS one, and ErrorTextAt hands back the file's own spelling where it
          // differs from the canonical one. A door that lost the literal would answer null here for
          // two different reasons, and the contract has one.
          Assert.Equal(eager.IsErrorAt(column, row), streamed.IsErrorAt(column, row));
          Assert.Equal(eager.ErrorTextAt(column, row), streamed.ErrorTextAt(column, row));

          Assert.Equal(Read(eager, column, row), Read(streamed, column, row));
        }
    }

    /// <summary>
    /// All six kinded reads of one cell, each rendered as its value or as the sentence that refused
    /// it, so a whole cell's kinded behaviour is one string to compare.
    /// </summary>
    private static string Read(ISheetCells space, int column, int row)
    {
      string Of<T>(Func<int, int, (bool Read, T Value, CellProblem? Problem)> read)
      {
        var (ok, value, problem) = read(column, row);

        return ok
          ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "<null>"
          : problem!(ProjectionLocation.At(Plane<ISheetCells>.Of(space)[column, row]).A1);
      }

      return string.Join(
        " | ",
        Of<string>((c, r) => (space.TextAt(c, r, out var v, out var p), v, p)),
        Of<decimal>((c, r) => (space.DecimalAt(c, r, out var v, out var p), v, p)),
        Of<int>((c, r) => (space.IntegerAt(c, r, out var v, out var p), v, p)),
        Of<double>((c, r) => (space.DoubleAt(c, r, out var v, out var p), v, p)),
        Of<DateTime>((c, r) => (space.DateTimeAt(c, r, out var v, out var p), v, p)),
        Of<bool>((c, r) => (space.BooleanAt(c, r, out var v, out var p), v, p)));
    }

    private static string Describe(IReadOnlyList<ProjectionDiagnostic> diagnostics) =>
      string.Join(
        Environment.NewLine,
        diagnostics.Select(d => $"{d.Severity}|{d.Subject}|{d.Message}|{d.Path}|{d.Location}"));

    /// <summary>`ReferenceEqualityComparer` is .NET 5 and up; this is the same thing, portably.</summary>
    private sealed class ByReference : IEqualityComparer<object>
    {
      internal static readonly ByReference Instance = new ByReference();

      public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

      public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
  }
}
