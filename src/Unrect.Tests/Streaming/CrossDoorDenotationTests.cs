using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// The cross-door matrix: one declaration set, read through the eager door and through the
  /// streaming door over the same file, compared at L3 — the value, the extent consumed and where
  /// from, the diagnostics in order, and the failure's problem, cell, path and subject.
  /// <para>
  /// It is the door-shaped twin of <c>Projections/LazyDenotationTests</c>, which sweeps 44
  /// declarations across eager and deferred extent resolution. The cross-door half of that claim was
  /// the weaker one: <see cref="StreamingIdentityTests"/> reaches L3 for one flagship declaration,
  /// and that declaration's diagnostic list is <em>empty</em> — so the strongest existing statement
  /// of "the doors agree about what a parse noticed" was a comparison of two empty lists. Every case
  /// here is chosen to have something to say, and <see cref="AssertNonVacuous"/> refuses to let the
  /// matrix quietly stop saying it: a scenario that produces neither a diagnostic nor a failure
  /// fails the test that names it.
  /// </para>
  /// <para>
  /// <strong>The fixtures are written here rather than committed.</strong> What the matrix needs is
  /// content chosen for the diagnostic it provokes — a landmark that is absent, a column of the
  /// wrong kind, a row nothing describes — and in a binary fixture none of that is visible to a
  /// reviewer. Each grid is an <c>object?[,]</c> a few lines above the scenarios that read it, and
  /// the writer below turns one into the smallest <c>.xlsx</c> that carries it. The precedent is
  /// <c>Spreadsheets/SpreadsheetSpaceDurationTests</c> and <c>Spreadsheets/FormulaAbsenceTests</c>,
  /// which build their packages in-test for the same reason.
  /// </para>
  /// <para>
  /// <strong>What this suite deliberately does not compare:</strong> cost. The window is the default
  /// for the matrix, and the one case that turns it down asserts the denotation is unmoved <em>and</em>
  /// that the counters moved — the separation the streaming design is built on.
  /// </para>
  /// </summary>
  public class CrossDoorDenotationTests : IDisposable
  {
    /// <summary>Every fixture carries one sheet, and it is always called this.</summary>
    private const string SheetName = "Data";

    /// <summary>The files this test wrote, by grid name, deleted when the test finishes.</summary>
    private readonly Dictionary<string, string> _files = new Dictionary<string, string>(StringComparer.Ordinal);

    public void Dispose()
    {
      foreach (var path in _files.Values)
      {
        if (File.Exists(path))
          File.Delete(path);
      }
    }

    /// <summary>Warming off everywhere, as in the sibling suites: it makes nothing wrong, only noisy.</summary>
    private static WorkbookOptions Cold() => new WorkbookOptions { WarmReaders = false };

    // --- The grids ---------------------------------------------------------------------------------
    //
    // Written out rather than generated, so the row a scenario's diagnostic names can be counted on
    // the page. Null is an absent cell and is not written to the file at all, exactly as an export
    // writes it; Valueless is the one cell that IS written and carries no value, which is what a
    // formatted-but-empty export region looks like.

    /// <summary>
    /// A report with a title, a gap, a three-column table of three records, and a totals line under
    /// a second gap. 3 columns by 8 rows.
    /// <code>
    ///   1  Quarterly Report
    ///   2
    ///   3  Client   Amount   Note
    ///   4  Alpha    100      ok
    ///   5  Beta     250.5    ok
    ///   6  Gamma    30       late
    ///   7
    ///   8  Totals   380.5
    /// </code>
    /// </summary>
    private static object?[,] Report() => new object?[,]
    {
      { "Quarterly Report", null, null },
      { null, null, null },
      { "Client", "Amount", "Note" },
      { "Alpha", 100, "ok" },
      { "Beta", 250.5m, "ok" },
      { "Gamma", 30, "late" },
      { null, null, null },
      { "Totals", 380.5m, null },
    };

    /// <summary>
    /// Two blank-separated deal blocks, with a memo in a third column nothing describes — so a
    /// repeat has a separator to absorb, a reason to stop, and something left over to be noticed.
    /// 3 columns by 7 rows.
    /// <code>
    ///   1  Deal One                memo
    ///   2  Name    Units
    ///   3  Alpha   10
    ///   4
    ///   5  Deal Two
    ///   6  Name    Units
    ///   7  Beta    20
    /// </code>
    /// </summary>
    private static object?[,] Blocks() => new object?[,]
    {
      { "Deal One", null, "memo" },
      { "Name", "Units", null },
      { "Alpha", 10, null },
      { null, null, null },
      { "Deal Two", null, null },
      { "Name", "Units", null },
      { "Beta", 20, null },
    };

    /// <summary>
    /// The same two blocks with a total line under them that is not a block: its first row reads as
    /// a deal name and the header the item then wants is not there. 2 columns by 9 rows.
    /// <code>
    ///   1  Deal One
    ///   2  Name    Units
    ///   3  Alpha   10
    ///   4
    ///   5  Deal Two
    ///   6  Name    Units
    ///   7  Beta    20
    ///   8
    ///   9  Grand Total   30
    /// </code>
    /// </summary>
    private static object?[,] Drift() => new object?[,]
    {
      { "Deal One", null },
      { "Name", "Units" },
      { "Alpha", 10 },
      { null, null },
      { "Deal Two", null },
      { "Name", "Units" },
      { "Beta", 20 },
      { null, null },
      { "Grand Total", 30 },
    };

    /// <summary>
    /// A header, a cell holding two spaces, and a value under it. The two spaces are the case: both
    /// file doors default to treating whitespace-only text as blank, so the row-wise rule must stop
    /// above it through either of them.
    /// </summary>
    private static object?[,] Whitespace() => new object?[,]
    {
      { "Header" },
      { "  " },
      { "value" },
    };

    /// <summary>
    /// A sheet whose cells are all written and none of them valued — the shape of a
    /// formatted-but-empty export region, and the file that makes both doors measure the sheet by
    /// reading it rather than by believing what it says about itself. Four rows, no width.
    /// </summary>
    private static object?[,] Valueless() => new object?[,]
    {
      { Empty, Empty, Empty },
      { Empty, Empty, Empty },
      { Empty, Empty, null },
      { Empty, Empty, Empty },
    };

    /// <summary>
    /// A ledger long enough that a small window cannot hold it: a caption, a header, 37 records, and
    /// a terminating landmark on the last row. 2 columns by 40 rows.
    /// </summary>
    private static object?[,] Tall()
    {
      var grid = new object?[40, 2];

      grid[0, 0] = "Ledger";
      grid[1, 0] = "Entry";
      grid[1, 1] = "Amount";

      for (var row = 2; row < 39; row++)
      {
        grid[row, 0] = row - 1;
        grid[row, 1] = (row - 1) * 2.5m;
      }

      grid[39, 0] = "End";

      return grid;
    }

    private static object?[,] Grid(string name) => name switch
    {
      "report" => Report(),
      "blocks" => Blocks(),
      "drift" => Drift(),
      "whitespace" => Whitespace(),
      "valueless" => Valueless(),
      "tall" => Tall(),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such grid."),
    };

    /// <summary>The file holding <paramref name="grid"/>, written on first ask.</summary>
    private string Fixture(string grid)
    {
      if (_files.TryGetValue(grid, out var existing))
        return existing;

      var path = Path.Combine(Path.GetTempPath(), $"unrect-cross-door-{grid}-{Guid.NewGuid():N}.xlsx");

      WriteWorkbook(path, SheetName, Grid(grid));
      _files.Add(grid, path);

      return path;
    }

    // --- What the scenarios are built from -----------------------------------------------------------

    /// <summary>What the report's table binds to. Note is a column no member claims, which is fine.</summary>
    private sealed record Entry(string Client, decimal Amount);

    /// <summary>The same table asked for the wrong kind: Amount is a Number, not text.</summary>
    private sealed record Mistyped(string Client, string Amount);

    /// <summary>One line of a deal block.</summary>
    private sealed record Holding(string Name, decimal Units);

    /// <summary>One record of the tall ledger.</summary>
    private sealed record Ledger(int Entry, decimal Amount);

    /// <summary>A deal block: its name, and how many holdings are under it.</summary>
    private static IProjection<string> DealBlock() =>
      VerticalFlow(v => $"{v.Next(Text())}[{v.Next(Table<Holding>()).Count}]");

    /// <summary>The tall ledger, anchored on its caption and bounded by its terminator.</summary>
    private static IProjection<IReadOnlyList<Ledger>> TallLedger() =>
      Table<Ledger>().Below(RowContaining("Ledger")).Until(RowContaining("End"));

    // --- The matrix ----------------------------------------------------------------------------------

    private static Scenario Case(string name) => name switch
    {
      // The plain shapes, each reading less of the sheet than there is, so the unconsumed-space Info
      // has something to say about where the declaration stops.
      "one row of a report" => Scenario.Of(Row(cells => cells.Count), "report"),
      "a flow of two leaves" => Scenario.Of(
        VerticalFlow(v => $"{v.Next(Text())}|{v.Next(Cell(cell => cell.IsBlank ? "-" : "x"))}"),
        "report"),

      // An overlay whose second child places itself three rows down and one across: an extent far
      // taller than a row, which is the shape the window sizing law is written about.
      "an overlay reaching down and across" => Scenario.Of(
        Overlay(o => $"{o.Next(Text())}|{o.Next(Decimal().Down(3).Right(1))}"),
        "report"),

      // The table ladder, the four rungs a declaration is normally written at (the view lambda is the
      // escape hatch and reads nothing new here), each anchored on the report's own caption so the
      // placement crosses a blank row on the way.
      "a typed table under its caption" => Scenario.Of(
        Table<Entry>().Under(Caption("Quarterly Report")),
        "report"),
      "the bind rung" => Scenario.Of(
        Table(
          headerRows: 1,
          eachRow: captions => Overlay(o => $"{o.Next(Text().Right(captions["Client"]))}={o.Next(Decimal().Right(captions["Amount"]))}"))
          .Under(Caption("Quarterly Report")),
        "report"),
      "the record-projection rung" => Scenario.Of(
        Table(headerRows: 1, eachRow: HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Decimal())}"))
          .Under(Caption("Quarterly Report")),
        "report"),
      // .Under before .Select on purpose: Select's wrapper is a projection with a placement of its
      // own, so anchoring the wrapper would leave the table inside it placed by its own default.
      "the dictionary rung" => Scenario.Of(
        Table()
          .Under(Caption("Quarterly Report"))
          .Select(rows => rows.Select(row => $"{row["Client"]}/{row["Amount"]}").ToList()),
        "report"),

      // A table bounded by a landmark that is really there: the bound is consumed in full, so what
      // is left over is the totals row and nothing else.
      "a table bounded by a landmark" => Scenario.Of(
        Table<Entry>().Under(Caption("Quarterly Report")).Until(RowContaining("Totals")),
        "report"),

      // Repetition: two blocks with a blank separator between them, and a memo column the
      // declaration never reaches, so the reading is complete down the sheet and short across it.
      "a repeat of blocks" => Scenario.Of(
        VerticalRepeat(DealBlock(), separatedBy: BlankRows()),
        "blocks"),
      // The same repeat bounded by a landmark that is not in the file: an Info, and a reading that
      // runs to the end of the space instead of failing.
      "a repeat bounded by an absent landmark" => Scenario.Of(
        VerticalRepeat(DealBlock(), separatedBy: BlankRows()).Until(RowContaining("Nowhere"), orEnd: true),
        "blocks"),
      // The repeat's own rule, which is the sharpest failure path in the matrix: only the item's
      // PLACEMENT ends a repetition, so a third occurrence whose first row reads and whose header
      // then is not there is loud rather than a quiet truncation. Both doors must say it in the same
      // words, about the same cell, at the same occurrence index.
      "a repeat meeting a line it cannot read" => Scenario.Of(
        VerticalRepeat(DealBlock(), separatedBy: BlankRows()),
        "drift"),

      // The three tolerance boundaries, each absorbing a real failure and saying so.
      "a choice absorbing its losing alternative" => Scenario.Of(Choice(Text().Down(1), Text()), "report"),
      "optional absorbing a failure" => Scenario.Of(Integer().Optional(), "report"),
      "else absorbing a failure" => Scenario.Of(Integer().Else(-1), "report"),

      // Failures. A landmark that is not there, a kind mismatch deep inside a table record, the same
      // mismatch reported by the binder instead, and a leaf placed off the end of the sheet.
      "a caption that is not there" => Scenario.Of(Caption("Annual Report"), "report"),
      "a kind mismatch inside a table record" => Scenario.Of(
        Table(headerRows: 1, eachRow: HorizontalFlow(h => $"{h.Next(Text())}/{h.Next(Text())}"))
          .Under(Caption("Quarterly Report")),
        "report"),
      "a binder that asks for the wrong kind" => Scenario.Of(
        Table<Mistyped>().Under(Caption("Quarterly Report")),
        "report"),
      "a leaf past the end of the sheet" => Scenario.Of(Text().Down(20), "report"),

      // Blankness, which is the adapter's decision and therefore the one most easily made twice: the
      // row-wise rule must stop above the whitespace-only cell through either door.
      "a rule stopped by a whitespace-only cell" => Scenario.Of(
        Range(RowsWhileAnyValue(), block => $"{block.Width}x{block.Height}"),
        "whitespace"),

      // The measured-by-reading path: a sheet with no valued cell has no width, so the leaf that
      // reads its first cell runs off the edge — identically, from a sheet both doors had to read to
      // measure.
      "a leaf over a sheet with no valued cells" => Scenario.Of(Text(), "valueless"),

      // Forty rows, an anchor at the top and a terminator at the bottom: the declaration the window
      // sweep below re-reads through a window that cannot hold it.
      "a tall ledger between two landmarks" => Scenario.Of(TallLedger(), "tall"),

      _ => throw new ArgumentOutOfRangeException(nameof(name), name, "No such case."),
    };

    /// <summary>
    /// The matrix, named once: the sweep below reads every case through both doors, and the census
    /// under it reads the same list to say what kinds of evidence it is made of.
    /// </summary>
    private static readonly string[] CaseNames =
    {
      "one row of a report",
      "a flow of two leaves",
      "an overlay reaching down and across",
      "a typed table under its caption",
      "the bind rung",
      "the record-projection rung",
      "the dictionary rung",
      "a table bounded by a landmark",
      "a repeat of blocks",
      "a repeat bounded by an absent landmark",
      "a repeat meeting a line it cannot read",
      "a choice absorbing its losing alternative",
      "optional absorbing a failure",
      "else absorbing a failure",
      "a caption that is not there",
      "a kind mismatch inside a table record",
      "a binder that asks for the wrong kind",
      "a leaf past the end of the sheet",
      "a rule stopped by a whitespace-only cell",
      "a leaf over a sheet with no valued cells",
      "a tall ledger between two landmarks",
    };

    /// <inheritdoc cref="CaseNames"/>
    public static TheoryData<string> Cases
    {
      get
      {
        var data = new TheoryData<string>();

        foreach (var name in CaseNames)
          data.Add(name);

        return data;
      }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ADeclarationMeansTheSameThroughEitherDoor(string name)
    {
      var scenario = Case(name);
      var path = Fixture(scenario.Grid);

      var eager = scenario.Read(SpreadsheetSpace.Create(path, SheetName));

      Observation streamed;

      using (var book = Workbook.Open(path, Cold()))
        streamed = scenario.Read(book.Sheet(SheetName));

      AssertNonVacuous(name, eager);
      AssertL3(eager, streamed);
    }

    /// <summary>
    /// The guard that keeps the matrix honest. Comparing two readings that noticed nothing and failed
    /// at nothing is a comparison of two empty lists, which is exactly the gap this suite exists to
    /// close — so a scenario that stops producing evidence fails here rather than passing quietly.
    /// </summary>
    private static void AssertNonVacuous(string name, Observation observation)
      => Assert.True(
        observation.Failure is not null || observation.Diagnostics.Count > 0,
        $"'{name}' produced neither a diagnostic nor a failure, so comparing the two doors on it proves nothing.");

    [Fact]
    public void TheMatrixIsMadeOfBothKindsOfEvidenceAndSixDifferentFailures()
    {
      // The census, and the guard the per-case non-vacuity check cannot be: every case producing a
      // failure would satisfy that check case by case and would mean the matrix had collapsed into
      // one boring statement — a fixture writer that emitted an unreadable sheet, say, would make
      // every declaration run off the same edge and every comparison pass. So the shape of the
      // evidence is pinned as well as its presence: how many cases succeed with something noticed,
      // how many fail, and that the failures are six DIFFERENT failures rather than one repeated.
      //
      // The numbers are meant to be edited. A case added to the matrix moves one of them, on purpose,
      // and the edit is where its author says which kind of evidence it contributes.
      var observed = CaseNames
        .Select(name => (Name: name, Observation: Case(name).Read(SpreadsheetSpace.Create(Fixture(Case(name).Grid), SheetName))))
        .ToList();

      Assert.Equal(21, observed.Count);

      foreach (var (name, observation) in observed)
        AssertNonVacuous(name, observation);

      var failures = observed.Where(entry => entry.Observation.Failure is not null).ToList();

      Assert.Equal(6, failures.Count);
      Assert.Equal(15, observed.Count - failures.Count);
      Assert.Equal(6, failures.Select(entry => entry.Observation.Failure).Distinct().Count());

      // Both severities a successful parse can raise are represented, so the diagnostic half of L3 is
      // exercised on more than one kind of remark.
      var severities = observed
        .SelectMany(entry => entry.Observation.Diagnostics)
        .Select(diagnostic => diagnostic.Split(' ')[0])
        .Distinct()
        .OrderBy(severity => severity, StringComparer.Ordinal)
        .ToList();

      Assert.Equal(new[] { "Info", "Warning" }, severities);
    }

    // --- Blankness: the doors share a default the array adapter does not ---------------------------------

    [Fact]
    public void BothDoorsCallAWhitespaceOnlyCellBlankAndTheArrayAdapterDoesNot()
    {
      // The scenario above rides on this and cannot state it: a row-wise rule stopping at row 2 is
      // consistent with the cell being blank and with the whole file being read wrongly. Here the cell
      // is asked directly, through both doors, and then the third adapter is asked the same question
      // to show the agreement is a shared DECISION rather than an inevitability. GridSpace decides
      // nothing about whitespace — an in-memory grid has no export conventions to compensate for —
      // and both file doors decide the same thing, because a workbook is full of "  " cells that are
      // meant to be empty.
      var path = Fixture("whitespace");

      var eager = SpreadsheetSpace.Create(path, SheetName);

      using var book = Workbook.Open(path, Cold());
      var streamed = book.Sheet(SheetName);

      Assert.True(eager[0, 1].IsBlank);
      Assert.True(streamed[0, 1].IsBlank);

      // The whole cell, not just the verdict: blankness is decided AT ADAPTATION, so a cell the
      // predicate calls blank arrives as Blank and its two spaces are gone. Both doors do the same
      // thing to it, which is the stronger statement — a door that kept the text would be equal on
      // IsBlank and different on everything a declaration could read out of the cell.
      Assert.Equal(CellValue.Blank, eager[0, 1]);
      Assert.Equal(eager[0, 1], streamed[0, 1]);
      Assert.Null(streamed[0, 1].TryGetString());

      // ...and the same characters through a door that decides nothing about whitespace: text, and
      // not blank. The agreement above is a shared decision, not an inevitability.
      Assert.False(GridSpace.Create(new[,] { { "  " } })[0, 0].IsBlank);
      Assert.Equal("  ", GridSpace.Create(new[,] { { "  " } })[0, 0].GetString());
    }

    // --- The sheet that will not say how big it is --------------------------------------------------------

    [Fact]
    public void BothDoorsMeasureASheetWithNoValuedCellTheSameWay()
    {
      // The extent behind the "leaf over a sheet with no valued cells" scenario, which the differential
      // form cannot carry on its own: a zero-wide space has no cells to compare, so an extent the two
      // doors agreed on could still be agreed nonsense. Hence the literals — four rows really are in
      // the file, and none of them carries a value.
      var path = Fixture("valueless");

      var eager = SpreadsheetSpace.Create(path, SheetName);

      using var book = Workbook.Open(path, Cold());
      var streamed = book.Sheet(SheetName);

      Assert.Equal(4, eager.Area.Size.Height);
      Assert.Equal(0, eager.Area.Size.Width);

      Assert.Equal(eager.Area.Size.Height, streamed.Area.Size.Height);
      Assert.Equal(eager.Area.Size.Width, streamed.Area.Size.Width);

      // And the streaming door says out loud that it got there by reading, which guards the fixture:
      // a grid that started describing itself again would pass every assertion above by the ordinary
      // declared path and would have stopped testing anything.
      Assert.Equal(4, book.Statistics(SheetName)!.Value.RowsMeasured);
    }

    // --- The window moves the cost and not the reading ------------------------------------------------------

    [Fact]
    public void AWindowTooSmallForTheSheetChangesTheCostAndNotTheDenotation()
    {
      // The denotation-versus-cost separation, stated at L3 rather than on values alone. The
      // declaration is anchored at the top of a forty-row sheet and bounded at the bottom of it, so
      // finding its extent reads past what an eight-row window can hold and reading its records then
      // reaches back — which is the shape of walk the counters are for. What must not move is
      // anything a caller can observe.
      var path = Fixture("tall");
      var declaration = TallLedger();

      var eager = Observe(declaration, SpreadsheetSpace.Create(path, SheetName));

      using var book = Workbook.Open(
        path,
        new WorkbookOptions { WarmReaders = false, WindowRows = 8, ChunkRows = 2 });

      var streamed = Observe(declaration, book.Sheet(SheetName));

      AssertNonVacuous("a tall ledger between two landmarks", eager);
      AssertL3(eager, streamed);

      // ...and the cost really did move, which is what makes the equality above worth asserting. The
      // window holds eight of the sheet's forty rows, and the reading paid for the difference.
      var statistics = book.Statistics(SheetName)!.Value;

      Assert.Equal(8, statistics.WindowChunks * statistics.ChunkRows);
      Assert.True(
        statistics.ChunkReloads > 0,
        $"the window was expected to be re-read; the counters say {statistics.ChunkReloads} reloads.");
    }

    // --- The sugar reads what the two-step form reads ----------------------------------------------------------

    [Fact]
    public void TheMapWorkbookSugarReadsWhatOpeningTheBookYourselfReads()
    {
      // Scenario (c) of the matrix, and the one case that cannot join the theory: MapWorkbook closes
      // the book on the way out, so there is no space left to Apply the declaration to and the L2
      // facets — the offset and the extent consumed — are not observable through it at all. What the
      // sugar can carry is the value and what the parse noticed, and those are compared in full.
      var path = Fixture("report");
      var declaration = Table<Entry>().Under(Caption("Quarterly Report"));

      var viaSugar = declaration.MapWorkbookWithDiagnostics(path, SheetName, Cold());

      using var book = Workbook.Open(path, Cold());
      var viaBook = declaration.MapWithDiagnostics(book.Sheet(SheetName));

      Assert.Equal(viaBook.Value, viaSugar.Value);

      // Not vacuous: this reading really does leave the totals row undescribed.
      Assert.NotEmpty(viaSugar.Diagnostics);
      Assert.Equal(
        viaBook.Diagnostics.Select(diagnostic => diagnostic.ToString()),
        viaSugar.Diagnostics.Select(diagnostic => diagnostic.ToString()));
    }

    // --- One declaration over one file, ready to be read through either door -------------------------------------

    /// <summary>
    /// A declaration paired with the grid it is written for, with the projection's type closed over
    /// so the matrix can hold scenarios that project different things.
    /// </summary>
    private sealed class Scenario
    {
      private readonly Func<ISpace, Observation> _read;

      private Scenario(string grid, Func<ISpace, Observation> read)
      {
        Grid = grid;
        _read = read;
      }

      /// <summary>Which fixture this scenario is read over.</summary>
      public string Grid { get; }

      public static Scenario Of<T>(IProjection<T> projection, string grid)
        => new Scenario(grid, space => Observe(projection, space));

      public Observation Read(ISpace space) => _read(space);
    }

    // --- The fixture writer -------------------------------------------------------------------------------------

    /// <summary>
    /// A cell that is written to the file and carries no value — what an export leaves behind where a
    /// region was formatted and never filled. It is the only way to write a row that exists without
    /// putting anything in it, and it is what makes a sheet the readers have to measure by reading.
    /// </summary>
    private static readonly object Empty = new object();

    /// <summary>
    /// The smallest <c>.xlsx</c> holding <paramref name="grid"/> on one sheet: inline strings for
    /// text, plain numbers for the rest, absent cells for nulls.
    /// <para>
    /// The <c>dimension</c> element is written and describes exactly the cells that follow it. It is
    /// there because a real export has one, and it is deliberately not load-bearing: ExcelDataReader
    /// derives its counts from a pre-scan of the cells on every format and does not consult it, so
    /// the extent these fixtures read at is decided by their content either way.
    /// </para>
    /// </summary>
    private static void WriteWorkbook(string path, string sheetName, object?[,] grid)
    {
      using var file = File.Create(path);
      using var package = new ZipArchive(file, ZipArchiveMode.Create);

      Add("[Content_Types].xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """);

      Add("_rels/.rels", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """);

      Add("xl/workbook.xml", $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="{sheetName}" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """);

      Add("xl/_rels/workbook.xml.rels", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """);

      // One style beyond General, and it is General too. A valueless cell in a real export carries a
      // style index — that is why it was written at all — so the fixtures write one, and nothing here
      // depends on what it formats.
      Add("xl/styles.xml", """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
          <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
          <borders count="1"><border/></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="2">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
          </cellXfs>
        </styleSheet>
        """);

      Add("xl/worksheets/sheet1.xml", Worksheet(grid));

      void Add(string name, string xml)
      {
        using var entry = new StreamWriter(
          package.CreateEntry(name, CompressionLevel.Optimal).Open(),
          new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        entry.Write(xml);
      }
    }

    /// <summary>The worksheet part for <paramref name="grid"/>, one row element per written row.</summary>
    private static string Worksheet(object?[,] grid)
    {
      var height = grid.GetLength(0);
      var width = grid.GetLength(1);

      var rows = new StringBuilder();
      var lastRow = 0;
      var lastColumn = 0;

      for (var row = 0; row < height; row++)
      {
        var cells = new StringBuilder();

        for (var column = 0; column < width; column++)
        {
          var value = grid[row, column];

          if (value is null)
            continue;

          cells.Append(CellXml(Reference(column, row), value));
          lastColumn = Math.Max(lastColumn, column);
        }

        if (cells.Length == 0)
          continue;

        rows.Append($"<row r=\"{row + 1}\">{cells}</row>");
        lastRow = row;
      }

      // The dimension describes what was actually written, which is the only honest thing it can say
      // — a grid whose trailing rows are entirely null has no cells there to describe.
      var dimension = rows.Length == 0
        ? string.Empty
        : $"<dimension ref=\"A1:{Reference(lastColumn, lastRow)}\"/>";

      return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>"
        + "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">"
        + dimension
        + $"<sheetData>{rows}</sheetData>"
        + "</worksheet>";
    }

    /// <summary>
    /// One cell element: inline text, a plain number, a flag, or a written-but-empty cell.
    /// <para>
    /// Named <c>CellXml</c> rather than <c>Cell</c> because a class member beats a
    /// <c>using static</c>: a private <c>Cell</c> here would shadow the leaf factory for every
    /// scenario in the file.
    /// </para>
    /// </summary>
    private static string CellXml(string reference, object value) => value switch
    {
      string text => $"<c r=\"{reference}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Escape(text)}</t></is></c>",
      int number => $"<c r=\"{reference}\"><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>",
      long number => $"<c r=\"{reference}\"><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>",
      double number => $"<c r=\"{reference}\"><v>{number.ToString("R", CultureInfo.InvariantCulture)}</v></c>",
      decimal number => $"<c r=\"{reference}\"><v>{number.ToString(CultureInfo.InvariantCulture)}</v></c>",
      bool flag => $"<c r=\"{reference}\" t=\"b\"><v>{(flag ? 1 : 0)}</v></c>",

      _ when ReferenceEquals(value, Empty) => $"<c r=\"{reference}\" s=\"1\" t=\"n\"/>",

      _ => throw new ArgumentException($"No cell spelling for {value.GetType()}.", nameof(value)),
    };

    /// <summary>An A1 reference. The fixtures are narrow, so one letter of column is enough.</summary>
    private static string Reference(int column, int row)
    {
      if (column >= 26)
        throw new ArgumentOutOfRangeException(nameof(column), column, "The fixture writer spells columns A-Z.");

      return $"{(char)('A' + column)}{row + 1}";
    }

    private static string Escape(string text) =>
      text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
  }
}
