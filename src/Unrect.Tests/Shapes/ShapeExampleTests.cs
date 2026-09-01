using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Excel;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;

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
        Header: v.Next(Column(4, c => new SimpleHeader(
          c[0].GetString(),
          c[1].GetString(),
          c[2].GetDateTime(),
          c[3].GetString()))
          .Named("report header")),
        Transactions: v.Next(TableRows(r => new Transaction(
          r["Client"].GetString(),
          r["Transaction Date"].GetDateTime(),
          r["Transaction Type"].GetString(),
          r["Amount"].GetDecimal()))
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
          Transactions: v.Next(TableRows(r => new DealTransaction(
            r["Account Key"].GetString(),
            r["Name"].GetString(),
            r["Transaction Type"].GetString(),
            r["Amount"].GetDecimal(),
            r["Transfer Date"].GetDateTime()))
            .Named("transactions"))))
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
      Assert.Equal("ACCT-10001", atlas.Transactions[0].Account);
      Assert.Equal("Birchwood Partners LP", atlas.Transactions[0].Name);
      Assert.Equal(250000m, atlas.Transactions[0].Amount);
      Assert.Equal(new DateTime(2026, 3, 12), atlas.Transactions[0].Date);
      Assert.Equal("Transfer In", atlas.Transactions[2].Type);

      Assert.Equal("Harlan Endowment", deals[1].Transactions[2].Name);
      Assert.Equal(-75000m, deals[1].Transactions[2].Amount);
      Assert.Equal(95000.5m, deals[1].Transactions[4].Amount);

      Assert.Equal(62500.25m, deals[2].Transactions[1].Amount);
      Assert.Equal(new DateTime(2026, 5, 9), deals[2].Transactions[1].Date);

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

      return VerticalFlow(v => new IrrReport(
        Title: v.Next(Column(4, c => c[0].GetString()).Named("report header")),
        Summary: v.Next(TableRows(r => r["Investors"].GetString()).Named("summary")),
        ByTransferDate: v.Next(series
          .After(Then(SeekRowContaining("Cash Flows Using Transfer Date"), SkipRows(1)))
          .Until(RowContaining(Inception))),
        ByInception: v.Next(series
          .After(Then(SeekRowContaining(Inception), SkipRows(1))))));
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

    // --- Result types ------------------------------------------------------------------------------------------

    private sealed record IrrReport(
      string Title,
      IReadOnlyList<string> Summary,
      IReadOnlyList<IReadOnlyList<string>> ByTransferDate,
      IReadOnlyList<IReadOnlyList<string>> ByInception);

    private sealed record SimpleHeader(string Title, string Subtitle, DateTime Date, string Id);

    private sealed record Transaction(string Client, DateTime Date, string Type, decimal Amount);

    private sealed record Deal(string Code, IReadOnlyList<DealTransaction> Transactions);

    private sealed record DealTransaction(string Account, string Name, string Type, decimal Amount, DateTime Date);

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
