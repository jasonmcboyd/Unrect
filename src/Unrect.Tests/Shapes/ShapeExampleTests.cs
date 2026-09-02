using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Excel;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The three example workbooks, parsed by the shapes the shipped scripts declare. These are the
  /// tests that would notice if any layer changed its meaning — adapter, strategy, engine or view —
  /// so they assert real content and real extents rather than counts alone.
  /// </summary>
  public class ShapeExampleTests
  {
    // The workbooks are copied into the test output, so tests never depend on the repository layout.
    private static ISpace Workbook(string fileName, string sheet)
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", fileName), sheet);

    // --- simple-report.xlsx: a fixed header over a table ----------------------------------------------------

    private static IShape<(SimpleHeader Header, IReadOnlyList<Transaction> Transactions)> SimpleReport() =>
      VerticalFlow(v => (
        // The header's kinds are declared rather than asked for: four leaves in a flow read the
        // same four rows Column(4, ...) did, and say what each one is.
        Header: v.Next(VerticalFlow(h => new SimpleHeader(
          Title: h.Next(Text()),
          Subtitle: h.Next(Text()),
          Date: h.Next(Date()),
          Id: h.Next(Text())))
          .Named("report header")),
        // Two captions the comparer would not have found; the other two bind free.
        Transactions: v.Next(TableRows<Transaction>(bind => bind
          .Column(t => t.Date, "Transaction Date")
          .Column(t => t.Type, "Transaction Type"))
          .Named("transactions"))));

    [Fact]
    public void SimpleReport_ProjectsItsHeader()
    {
      var (header, _) = SimpleReport().Map(Workbook("simple-report.xlsx", "Report"));

      Assert.Equal("Capital Activity Report", header.Title);
      Assert.Equal("Q2 2026 - All Clients", header.Subtitle);
      Assert.Equal(new DateTime(2026, 6, 30), header.Date);
      Assert.Equal("RPT-00042", header.Id);
    }

    [Fact]
    public void SimpleReport_ProjectsItsTransactions()
    {
      var (_, transactions) = SimpleReport().Map(Workbook("simple-report.xlsx", "Report"));

      Assert.Equal(8, transactions.Count);
      Assert.Equal(1776750.24m, transactions.Sum(t => t.Amount));
      Assert.Equal(-82750.25m, transactions[3].Amount);

      var first = transactions[0];
      Assert.Equal("Acme Holdings", first.Client);
      Assert.Equal(new DateTime(2026, 4, 3), first.Date);
      Assert.Equal("Capital Call", first.Type);
      Assert.Equal(250000m, first.Amount);

      Assert.Equal("Dunmore Capital", transactions[7].Client);
      Assert.Equal(750000m, transactions[7].Amount);
    }

    [Fact]
    public void SimpleReport_LandsOnTheExtentsTheSpecRecords()
    {
      // Header 1x4, table 4x9 (one header row plus eight body rows), and between them a blank gap
      // that the table's default offset discovers rather than a hard-coded skip.
      var applied = SimpleReport().Apply(Workbook("simple-report.xlsx", "Report"));

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(16, applied.Consumed.Height);

      var header = Column(4, c => c.Count).Apply(Workbook("simple-report.xlsx", "Report"));
      Assert.Equal(1, header.Consumed.Width);
      Assert.Equal(4, header.Consumed.Height);

      // Handed the space the header left behind, the table's own defaults find the gap and the body.
      var table = Table(t => (t.ColumnCount, t.RowCount))
        .Apply(Workbook("simple-report.xlsx", "Report").GetSubspace(new Offset(0, 4)));

      Assert.Equal((4, 8), table.Value);
      Assert.Equal(3, table.Offset.Size.Height);
      Assert.Equal(4, table.Consumed.Width);
      Assert.Equal(9, table.Consumed.Height);
    }

    // --- investors-by-deal.xlsx: repeating blocks of differing lengths ------------------------------------------

    private static IShape<IReadOnlyList<Deal>> InvestorsByDeal()
    {
      var deal =
        VerticalFlow(v => new Deal(
          Code: v.Next(Cell(c => c.GetString()).Named("deal code")),
          // Every caption binds free: AccountKey to "Account Key", TransferDate to "Transfer Date".
          Transactions: v.Next(TableRows<DealTransaction>().Named("transactions"))))
          .Named("deal block");

      return Repeat(deal, separatedBy: BlankRows());
    }

    [Fact]
    public void InvestorsByDeal_YieldsOneBlockPerDeal()
    {
      var deals = InvestorsByDeal().Map(Workbook("investors-by-deal.xlsx", "Investors"));

      Assert.Equal(
        new[] { "ATLAS-2024", "HELIOS-2025", "KESTREL-2025" },
        deals.Select(d => d.Code).ToArray());
      Assert.Equal(new[] { 3, 5, 2 }, deals.Select(d => d.Transactions.Count).ToArray());
    }

    [Fact]
    public void InvestorsByDeal_ProjectsTransactionsWithinEachBlock()
    {
      var deals = InvestorsByDeal().Map(Workbook("investors-by-deal.xlsx", "Investors"));

      var atlas = deals[0];
      Assert.Equal("ACCT-10001", atlas.Transactions[0].AccountKey);
      Assert.Equal("Birchwood Partners LP", atlas.Transactions[0].Name);
      Assert.Equal(250000m, atlas.Transactions[0].Amount);
      Assert.Equal(new DateTime(2026, 3, 12), atlas.Transactions[0].TransferDate);
      Assert.Equal("Transfer In", atlas.Transactions[2].TransactionType);

      Assert.Equal("Harlan Endowment", deals[1].Transactions[2].Name);
      Assert.Equal(-75000m, deals[1].Transactions[2].Amount);
      Assert.Equal(95000.5m, deals[1].Transactions[4].Amount);

      Assert.Equal(62500.25m, deals[2].Transactions[1].Amount);
      Assert.Equal(new DateTime(2026, 5, 9), deals[2].Transactions[1].TransferDate);

      Assert.Equal(1_897_500.75m, deals.SelectMany(d => d.Transactions).Sum(t => t.Amount));
    }

    [Fact]
    public void InvestorsByDeal_DiscoversABlockLengthPerBlock()
    {
      // Blocks of 5, 7 and 4 rows: a code row, a column-header row, and however many transactions.
      var applied = InvestorsByDeal().Apply(Workbook("investors-by-deal.xlsx", "Investors"));

      Assert.Equal(6, applied.Consumed.Width);
      Assert.Equal(18, applied.Consumed.Height);
    }

    // --- investor-summary.xlsx: header, summary table, and repeating detail blocks ---------------------------------

    private static IShape<Report> InvestorSummary()
    {
      var detail =
        VerticalFlow(v => new Detail(
          Investor: v.Next(Cell(c => c.GetString()).Named("investor name")),
          Transactions: v.Next(TableRows(r => new DetailTransaction(
            r["Date"].GetDateTime(),
            r["Transaction Type"].GetString(),
            r["Amount"].GetDecimal()))
            .Named("transactions"))))
          .Named("investor detail");

      return VerticalFlow(v => new Report(
        Header: v.Next(Column(c => new SummaryHeader(c[0].GetString(), c[1].GetDateTime(), c[2].GetString()))
          .Named("report header")),
        Summary: v.Next(TableRows(r => new SummaryRow(
          r["Investor"].GetString(),
          r["Contributions"].GetDecimal(),
          r["Distributions"].GetDecimal(),
          r["Net"].GetDecimal()))
          .Named("summary")),
        Details: v.Next(Repeat(detail, separatedBy: BlankRows(), atLeast: 1)
          .AfterBlankRows()
          .Named("investor details"))));
    }

    [Fact]
    public void InvestorSummary_DiscoversItsHeaderHeight()
    {
      // Nothing declares "three rows"; Column(c => ...) reads as far as the values go.
      var report = InvestorSummary().Map(Workbook("investor-summary.xlsx", "Summary"));

      Assert.Equal("Investor Summary Report", report.Header.Title);
      Assert.Equal(new DateTime(2026, 6, 30), report.Header.Date);
      Assert.Equal("RPT-00107", report.Header.Id);

      var header = Column(c => c.Count).Apply(Workbook("investor-summary.xlsx", "Summary"));
      Assert.Equal(1, header.Consumed.Width);
      Assert.Equal(3, header.Consumed.Height);
    }

    [Fact]
    public void InvestorSummary_ProjectsTheSummaryTable()
    {
      var report = InvestorSummary().Map(Workbook("investor-summary.xlsx", "Summary"));

      Assert.Equal(3, report.Summary.Count);
      Assert.Equal(
        new[] { "Birchwood Partners LP", "Meridian Family Trust", "Harlan Endowment" },
        report.Summary.Select(s => s.Investor).ToArray());

      Assert.Equal(650000m, report.Summary[0].Contributions);
      Assert.Equal(-120000m, report.Summary[0].Distributions);
      Assert.Equal(530000m, report.Summary[0].Net);

      Assert.Equal(245000.5m, report.Summary[1].Contributions);
      Assert.Equal(0m, report.Summary[1].Distributions);
      Assert.Equal(245000.5m, report.Summary[1].Net);

      Assert.Equal(1000000m, report.Summary[2].Contributions);
      Assert.Equal(-75000.25m, report.Summary[2].Distributions);
      Assert.Equal(924999.75m, report.Summary[2].Net);
    }

    [Fact]
    public void InvestorSummary_ProjectsEachDetailBlock()
    {
      var report = InvestorSummary().Map(Workbook("investor-summary.xlsx", "Summary"));

      Assert.Equal(3, report.Details.Count);
      Assert.Equal(
        new[] { "Birchwood Partners LP", "Meridian Family Trust", "Harlan Endowment" },
        report.Details.Select(d => d.Investor).ToArray());
      Assert.Equal(new[] { 3, 2, 4 }, report.Details.Select(d => d.Transactions.Count).ToArray());

      var birchwood = report.Details[0];
      Assert.Equal(new DateTime(2026, 2, 10), birchwood.Transactions[0].Date);
      Assert.Equal("Capital Call", birchwood.Transactions[0].Type);
      Assert.Equal(400000m, birchwood.Transactions[0].Amount);
      Assert.Equal(-120000m, birchwood.Transactions[2].Amount);

      Assert.Equal(95000.5m, report.Details[1].Transactions[1].Amount);
      Assert.Equal(new DateTime(2026, 6, 27), report.Details[2].Transactions[3].Date);
      Assert.Equal(200000m, report.Details[2].Transactions[3].Amount);
    }

    [Fact]
    public void InvestorSummary_SummaryRowsAndDetailBlocksAgree()
    {
      // Cross-region correlation is post-parse validation, not decomposition: the shape declares the
      // document, and the caller checks that the document is internally consistent.
      var report = InvestorSummary().Map(Workbook("investor-summary.xlsx", "Summary"));

      Assert.Equal(report.Summary.Count, report.Details.Count);
      Assert.Equal(
        report.Summary.Select(s => s.Investor).ToArray(),
        report.Details.Select(d => d.Investor).ToArray());

      foreach (var (summary, detail) in report.Summary.Zip(report.Details, (s, d) => (s, d)))
      {
        Assert.Equal(summary.Contributions, detail.Transactions.Where(t => t.Amount > 0).Sum(t => t.Amount));
        Assert.Equal(summary.Distributions, detail.Transactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
        Assert.Equal(summary.Net, detail.Transactions.Sum(t => t.Amount));
      }
    }

    [Fact]
    public void InvestorSummary_ConsumesTheWholeSheet()
    {
      var applied = InvestorSummary().Apply(Workbook("investor-summary.xlsx", "Summary"));

      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(28, applied.Consumed.Height);
    }

    // --- investor-irr.xlsx: one shape, two placements of the same repeat -----------------------------------------
    //
    // The file the Until vocabulary exists for: two series of per-investor blocks, the first ending
    // exactly where the second's caption begins. One declaration reads both, because the first
    // series is bounded by the caption the second is anchored on.

    private static IShape<IrrReport> InvestorIrr()
    {
      var investorBlock = TableRows(r => r["Investor Name"].GetString()).Named("investor block");

      // Declared once, placed twice: the same series of blocks read from two different anchors.
      var series = Repeat(investorBlock, separatedBy: BlankRows());

      const string Inception = "Cash Flows using inception date";

      // The captions are declared rather than skipped over: each section says what it sits under,
      // and the rows those captions occupy are described by the shape instead of being absorbed
      // into an offset nobody can see.
      return VerticalFlow(v => new IrrReport(
        Title: v.Next(Column(4, c => c[0].GetString()).Named("report header")),
        Summary: v.Next(TableRows(r => r["Investors"].GetString()).Named("summary")),
        ByTransferDate: v.Next(series
          .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
          .Until(RowContaining(Inception))),
        ByInception: v.Next(series
          .Under(Caption(Inception)))));
    }

    [Fact]
    public void InvestorIrr_ReadsBothSeriesWithOneDeclaration()
    {
      var report = InvestorIrr().Map(Workbook("investor-irr.xlsx", "IRR"));

      Assert.Equal("Investor IRR Report", report.Title);
      Assert.Equal(3, report.Summary.Count);

      // Three blocks in each series, of three, two and four transaction rows.
      Assert.Equal(3, report.ByTransferDate.Count);
      Assert.Equal(3, report.ByInception.Count);
      Assert.Equal(new[] { 3, 2, 4 }, report.ByTransferDate.Select(block => block.Count).ToArray());
      Assert.Equal(new[] { 3, 2, 4 }, report.ByInception.Select(block => block.Count).ToArray());
    }

    [Fact]
    public void InvestorIrr_TheFirstSeriesStopsWhereTheSecondsCaptionBegins()
    {
      // Without the bound the first repeat would run into the caption and fail from inside its
      // item; with it, the second series anchors on that same caption at distance zero.
      var report = InvestorIrr().Map(Workbook("investor-irr.xlsx", "IRR"));

      Assert.Equal("Alpha Capital LLC", report.ByTransferDate[0][0]);
      Assert.Equal("Cedar Holdings", report.ByInception[2][0]);
    }

    [Fact]
    public void InvestorIrr_DescribesTheWholeSheet()
    {
      // An empty diagnostic list IS the full-consumption assertion: anything the shape left over
      // on either axis would arrive as the unconsumed-space Info. So the sheet's own dimensions and
      // the absence of diagnostics together say the declaration described all 6x45 of it.
      var space = Workbook("investor-irr.xlsx", "IRR");

      var result = InvestorIrr().MapWithDiagnostics(space);

      Assert.Equal(6, space.Area.Size.Width);
      Assert.Equal(45, space.Area.Size.Height);
      Assert.Empty(result.Diagnostics);
    }

    // --- Captioned sections, mirroring scrubbed-k1 -----------------------------------------------------------
    //
    // The shape of the K-1 without the workbook: captioned sections separated by a blank row, each
    // read by the caption it sits under. The pin that matters is the burn-down one — a caption row
    // is DESCRIBED by the shape that owns it, not smuggled past inside an offset — so the section's
    // own rows exclude its caption, and the meter counts the caption rows all the same.

    private static ISpace CaptionedSheet() => Mixed(new object?[,]
    {
      { "K-1 Lines 1-21", null },
      { "Ordinary income", 100 },
      { "Interest income", 25 },
      { null, null },
      { "Foreign transactions", null },
      { "Gross income", 40 },
    });

    [Fact]
    public void CaptionedSections_AreReadByTheCaptionsTheySitUnder()
    {
      var lines = TableRows(0, r => r[0].GetString());

      var report = VerticalFlow(v => new
      {
        Ordinary = v.Next(lines.Under(Caption("K-1 Lines 1-21")).Until(RowContaining("Foreign transactions"))),
        Foreign = v.Next(lines.Under(Caption("Foreign transactions"))),
      }).Map(CaptionedSheet());

      Assert.Equal(new[] { "Ordinary income", "Interest income" }, report.Ordinary);
      Assert.Equal(new[] { "Gross income" }, report.Foreign);
    }

    [Fact]
    public void ACaptionRowIsOwnedByItsCaptionAndNotByTheSectionBelowIt()
    {
      // The regression pin for "the caption stopped being smuggled": neither section's rows contain
      // the caption that introduced it, and neither contains the other section's caption either.
      var lines = TableRows(0, r => r[0].GetString());

      var report = VerticalFlow(v => new
      {
        Ordinary = v.Next(lines.Under(Caption("K-1 Lines 1-21")).Until(RowContaining("Foreign transactions"))),
        Foreign = v.Next(lines.Under(Caption("Foreign transactions"))),
      }).Map(CaptionedSheet());

      Assert.DoesNotContain("K-1 Lines 1-21", report.Ordinary);
      Assert.DoesNotContain("Foreign transactions", report.Ordinary);
      Assert.DoesNotContain("Foreign transactions", report.Foreign);
    }

    [Fact]
    public void CaptionRowsAreDescribedRatherThanSkipped()
    {
      // ...and the meter did not move: every row of the sheet is accounted for, including the two
      // caption rows and the blank one the second section's seek crossed.
      var lines = TableRows(0, r => r[0].GetString());

      var report = VerticalFlow(v => new
      {
        Ordinary = v.Next(lines.Under(Caption("K-1 Lines 1-21")).Until(RowContaining("Foreign transactions"))),
        Foreign = v.Next(lines.Under(Caption("Foreign transactions"))),
      });

      var result = report.MapWithDiagnostics(CaptionedSheet());
      var applied = report.Apply(CaptionedSheet());

      Assert.Empty(result.Diagnostics);
      Assert.Equal(6, applied.Consumed.Height);
    }

    // --- The typed form changed the projection and nothing else ---------------------------------------------------

    [Fact]
    public void OneGridReadBothWays_GivesTheSameValuesAndTheSameExtent()
    {
      // The regression pin for the whole phase: TableRows<T>() is the projecting spelling with the
      // lambda moved into the type. Same placement, same extent, same numbers — only the code that
      // says what a column means has moved.
      var space = Mixed(new object?[,]
      {
        { null, null, null },
        { "Client", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 3, 4), 250000m },
        { "Beta", new DateTime(2026, 5, 17), 175500.5m },
      });

      var typed = TableRows<Line>().Apply(space);

      var projected = TableRows(r => new Line(
        r["Client"].GetString(),
        r["Transaction Date"].GetDateTime(),
        r["Amount"].GetDecimal()))
        .Apply(space);

      Assert.Equal(projected.Value, typed.Value);
      Assert.Equal(projected.Offset.Size.Height, typed.Offset.Size.Height);
      Assert.Equal(projected.Consumed.Width, typed.Consumed.Width);
      Assert.Equal(projected.Consumed.Height, typed.Consumed.Height);

      Assert.Equal(2, typed.Value.Count);
      Assert.Equal(425500.5m, typed.Value.Sum(line => line.Amount));
    }

    private sealed record Line(string Client, DateTime TransactionDate, decimal Amount);

    // --- Result types ------------------------------------------------------------------------------------------

    private sealed record IrrReport(
      string Title,
      IReadOnlyList<string> Summary,
      IReadOnlyList<IReadOnlyList<string>> ByTransferDate,
      IReadOnlyList<IReadOnlyList<string>> ByInception);

    private sealed record SimpleHeader(string Title, string Subtitle, DateTime Date, string Id);

    private sealed record Transaction(string Client, DateTime Date, string Type, decimal Amount);

    private sealed record Deal(string Code, IReadOnlyList<DealTransaction> Transactions);

    private sealed record DealTransaction(
      string AccountKey,
      string Name,
      string TransactionType,
      decimal Amount,
      DateTime TransferDate);

    private sealed record SummaryHeader(string Title, DateTime Date, string Id);

    private sealed record SummaryRow(string Investor, decimal Contributions, decimal Distributions, decimal Net);

    private sealed record DetailTransaction(DateTime Date, string Type, decimal Amount);

    private sealed record Detail(string Investor, IReadOnlyList<DetailTransaction> Transactions);

    private sealed record Report(
      SummaryHeader Header,
      IReadOnlyList<SummaryRow> Summary,
      IReadOnlyList<Detail> Details);
  }
}
