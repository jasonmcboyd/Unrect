using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Shapes.Shape;

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

      for (var row = 0; row < eager.Area.Size.Height; row++)
        for (var column = 0; column < eager.Area.Size.Width; column++)
          Assert.Equal(eager[column, row], streamed[column, row]);
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

      for (var row = 0; row < eager.Area.Size.Height; row++)
        for (var column = 0; column < eager.Area.Size.Width; column++)
          Assert.Equal(eager[column, row], streamed[column, row]);

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
      // The differential form applied to identity rather than to value. Each door is asked, for every
      // text cell of a sheet, which earlier cell it shares its characters with — and the two answers
      // must be the same list. That is the strongest form of "the doors differ in nothing a caller can
      // observe": it is not enough that the cells are equal, because a caller who holds a grid pays
      // for the instances, and the two doors keep separate tables with separate guards that could
      // drift apart. repeated-text.xlsx is the case with something to say (its 256- and 257-character
      // neighbours land on opposite sides of the guard, and both doors must put them there); the rest
      // are the control.
      var eager = SpreadsheetSpace.Create(Path(file), sheet);
      using var book = Workbook.Open(Path(file), new WorkbookOptions { WarmReaders = false });
      var streamed = book.Sheet(sheet);

      Assert.Equal(SharingPattern(eager), SharingPattern(streamed));
    }

    /// <summary>
    /// For each cell in reading order, the position of the first cell holding the same string
    /// INSTANCE — itself for a first sighting, and -1 for a cell that is not text at all. Two spaces
    /// with the same pattern share exactly the same values as each other.
    /// </summary>
    private static IReadOnlyList<int> SharingPattern(ISpace space)
    {
      // Reference equality on purpose: the question is which instance a cell points at, and the
      // default comparer would answer the one this test is not asking.
      var seen = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
      var pattern = new List<int>();

      for (var row = 0; row < space.Area.Size.Height; row++)
        for (var column = 0; column < space.Area.Size.Width; column++)
        {
          if (space[column, row].TryGetString() is not string text)
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
    // investor-irr.xlsx read by the shape the example tests use: a VerticalFlow of a Column, a
    // TableRows and two Repeats under captions, one of them Until-bounded. It reaches backwards
    // (the second series anchors on the caption that bounded the first), it makes several passes,
    // and it consumes the whole sheet — which is to say it exercises the pool, the window and the
    // diagnostics in one declaration. If streaming can read this, it can read a report.

    private static IShape<(string Title, IReadOnlyList<string> Summary, IReadOnlyList<IReadOnlyList<string>> ByTransferDate, IReadOnlyList<IReadOnlyList<string>> ByInception)> InvestorIrr()
    {
      var investorBlock = TableRows(row => row["Investor Name"].GetString()).Named("investor block");
      var series = Repeat(investorBlock, separatedBy: BlankRows());

      const string Inception = "Cash Flows using inception date";

      return VerticalFlow(v => (
        Title: v.Next(Column(4, column => column[0].GetString()).Named("report header")),
        Summary: v.Next(TableRows(row => row["Investors"].GetString()).Named("summary")),
        ByTransferDate: v.Next(series
          .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
          .Until(RowContaining(Inception))),
        ByInception: v.Next(series.Under(Caption(Inception)))));
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
      // The backward-reaching, multi-pass shape against a window that cannot hold what it sweeps.
      // The counters move — that is the cost model working — and the answer does not.
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
      // investors-by-deal: repeating blocks separated by blank bands, which is the shape whose
      // termination depends on reading past the end of one block and into the next.
      var declaration = Repeat(
        VerticalFlow(v => (
          Deal: v.Next(Cell(cell => cell.GetString())),
          Rows: v.Next(TableRows(row => row["Name"].GetString())))),
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
      var declaration = VerticalFlow(v => (
        Header: v.Next(Column(4, column => column[0].GetString())),
        Rows: v.Next(TableRows(row => (row["Client"].GetString(), row["Amount"].GetDecimal())))));

      var eager = declaration.Map(SpreadsheetSpace.Create(Path("simple-report.xlsx"), "Report"));

      using var book = Workbook.Open(Path("simple-report.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = declaration.Map(book.Sheet("Report"));

      Assert.Equal(eager.Header, streamed.Header);
      Assert.Equal(eager.Rows, streamed.Rows);
    }

    // --- Failures are identical too -----------------------------------------------------------------------

    [Fact]
    public void ADeclarationThatFailsFailsWithTheSameMessageFromBothDoors()
    {
      // A diagnostic that named a different cell depending on which door was used would be worse
      // than useless: the message is what a caller acts on, and it has to describe the workbook
      // rather than the reader.
      var declaration = Cell(cell => cell.GetInt()).Named("a number");

      var eager = Assert.Throws<ShapeException>(
        () => declaration.Map(SpreadsheetSpace.Create(Path("edge-cases.xlsx"), "Edges")));

      using var book = Workbook.Open(Path("edge-cases.xlsx"), new WorkbookOptions { WarmReaders = false });
      var streamed = Assert.Throws<ShapeException>(() => declaration.Map(book.Sheet("Edges")));

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

      Assert.Equal(CellKind.Error, streamed[0, 1].Kind);
      Assert.Equal(CellError.Value, streamed[0, 1].GetError());
      Assert.False(streamed[0, 1].IsBlank);
    }

    private static string Describe(IReadOnlyList<ShapeDiagnostic> diagnostics) =>
      string.Join(
        Environment.NewLine,
        diagnostics.Select(d => $"{d.Severity}|{d.Subject}|{d.Message}|{d.Path}|{d.Location}"));
  }
}
