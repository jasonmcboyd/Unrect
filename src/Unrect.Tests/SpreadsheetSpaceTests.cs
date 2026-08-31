using System;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Excel;
using Unrect.Strategies;

using Xunit;

using static Unrect.RegionBuilderFactory;
using static Unrect.Strategies.OffsetStrategies;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Tests
{
  /// <summary>
  /// End-to-end tests over the two example workbooks: a real file, adapted to canonical cell values,
  /// decomposed by a declared shape, and projected to typed results. These are the tests that would
  /// notice if any layer changed its meaning, so they assert real content, not just row counts.
  /// </summary>
  public class SpreadsheetSpaceTests
  {
    // The workbooks are copied into the test output, so tests never depend on the repository layout.
    private static string WorkbookPath(string fileName)
      => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private static ISpace SimpleReport() => SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "Report");

    private static ISpace InvestorsByDeal() => SpreadsheetSpace.Create(WorkbookPath("investors-by-deal.xlsx"), "Investors");

    // --- Adapter behaviour ------------------------------------------------------------------------

    [Fact]
    public void Create_ReadsTheSheetDimensions()
    {
      var space = SimpleReport();

      Assert.Equal(4, space.Area.Size.Width);
      Assert.Equal(16, space.Area.Size.Height);
    }

    [Fact]
    public void Create_MatchesSheetNamesCaseInsensitivelyByDefault()
    {
      var space = SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "report");

      Assert.Equal(16, space.Area.Size.Height);
    }

    [Fact]
    public void Create_WithCaseSensitiveMatchingAndTheWrongCase_FindsNoSheet()
    {
      Assert.Throws<InvalidOperationException>(() =>
        SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "report", caseSensitive: true));
    }

    [Fact]
    public void Create_WithAPredicate_ExposesTheSheetIndexAndName()
    {
      var contexts = new System.Collections.Generic.List<SpreadsheetContext>();

      var sheets = SpreadsheetSpace
        .Create(WorkbookPath("investors-by-deal.xlsx"), context =>
        {
          contexts.Add(context);
          return true;
        })
        .ToArray();

      Assert.Single(sheets);
      Assert.Equal(6, sheets[0].Area.Size.Width);
      Assert.Equal(18, sheets[0].Area.Size.Height);

      var only = Assert.Single(contexts);
      Assert.Equal(0, only.Index);
      Assert.Equal("Investors", only.Name);
    }

    [Fact]
    public void EmptyCellsInsideTheGrid_AreBlank()
    {
      var space = SimpleReport();

      // The title row has a value only in column 0; the rest of the row is genuinely empty.
      Assert.True(space[0, 0].HasValue);
      Assert.True(space[1, 0].IsBlank);
      Assert.Same(CellValue.Blank, space[1, 0]);
      Assert.Same(CellValue.Blank, space[3, 0]);
    }

    [Fact]
    public void SeparatorRowsBetweenBlocks_AreEntirelyBlank()
    {
      var space = InvestorsByDeal();

      Assert.All(
        Enumerable.Range(0, space.Area.Size.Width),
        column => Assert.True(space[column, 5].IsBlank));
    }

    [Fact]
    public void CellKinds_FollowTheUnderlyingSheetValues()
    {
      var space = SimpleReport();

      Assert.Equal(CellKind.Text, space[0, 0].Kind);
      Assert.Equal(CellKind.Temporal, space[0, 2].Kind);
      Assert.Equal(CellKind.Number, space[3, 8].Kind);
      Assert.Equal(CellKind.Blank, space[1, 4].Kind);
    }

    // --- simple-report.xlsx -----------------------------------------------------------------------
    //
    // A vertical header block, a blank gap, a column-header row, and a data table. Only the header
    // uses explicit bounds; every other boundary is discovered.

    private static Region3<Region, Region, Region> BuildSimpleReport() =>
      Vertical(
        Builder(0, 0, 1, 4),
        Builder(SkipBlankRows(), RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue()),
        Builder(RowsWhileAnyValue().ToAreaStrategy()))
        .Build(SimpleReport());

    [Fact]
    public void SimpleReport_DecomposesIntoHeaderColumnHeadersAndData()
    {
      var report = BuildSimpleReport();

      Assert.Equal(1, report.Subregion1.Space.Area.Size.Width);
      Assert.Equal(4, report.Subregion1.Space.Area.Size.Height);

      Assert.Equal(4, report.Subregion2.Space.Area.Size.Width);
      Assert.Equal(1, report.Subregion2.Space.Area.Size.Height);

      Assert.Equal(4, report.Subregion3.Space.Area.Size.Width);
      Assert.Equal(8, report.Subregion3.Space.Area.Size.Height);
    }

    [Fact]
    public void SimpleReport_HeaderHoldsTheTitleSubtitleDateAndId()
    {
      var header = BuildSimpleReport().Subregion1.Space;

      Assert.Equal("Capital Activity Report", header[0, 0].GetString());
      Assert.Equal("Q2 2026 - All Clients", header[0, 1].GetString());
      Assert.Equal(new DateTime(2026, 6, 30), header[0, 2].GetDate());
      Assert.Equal("RPT-00042", header[0, 3].GetString());
    }

    [Fact]
    public void SimpleReport_ColumnHeaderRowIsFoundAfterTheBlankGap()
    {
      var columnHeaders = BuildSimpleReport().Subregion2;

      Assert.Equal(
        new[] { "Client", "Transaction Date", "Transaction Type", "Amount" },
        columnHeaders.Rows().Single().Select(v => v.GetString()).ToArray());
    }

    [Fact]
    public void SimpleReport_DataTableHasEightTransactions()
    {
      Assert.Equal(8, BuildSimpleReport().Subregion3.Rows().Count());
    }

    [Fact]
    public void SimpleReport_AmountsSumExactlyAsDecimals()
    {
      var amounts = BuildSimpleReport().Subregion3.Rows().Select(r => r[3].GetDecimal()).ToArray();

      Assert.Equal(8, amounts.Length);
      Assert.Equal(1776750.24m, amounts.Sum());
    }

    [Fact]
    public void SimpleReport_PreservesNegativeAmounts()
    {
      var amounts = BuildSimpleReport().Subregion3.Rows().Select(r => r[3].GetDecimal()).ToArray();

      Assert.Equal(-82750.25m, amounts[3]);
      Assert.Contains(-41000m, amounts);
    }

    [Fact]
    public void SimpleReport_MapsRowsToTypedTransactions()
    {
      var report = BuildSimpleReport().Map((header, _, data, _) => new
      {
        Title = header.Space[0, 0].GetString(),
        Transactions = data.Rows()
          .Select(r => new
          {
            Client = r[0].GetString(),
            Date = r[1].GetDate(),
            Type = r[2].GetString(),
            Amount = r[3].GetDecimal(),
          })
          .ToArray(),
      });

      Assert.Equal("Capital Activity Report", report.Title);
      Assert.Equal(8, report.Transactions.Length);

      var first = report.Transactions[0];
      Assert.Equal("Acme Holdings", first.Client);
      Assert.Equal(new DateTime(2026, 4, 3), first.Date);
      Assert.Equal("Capital Call", first.Type);
      Assert.Equal(250000m, first.Amount);

      var last = report.Transactions[7];
      Assert.Equal("Dunmore Capital", last.Client);
      Assert.Equal(new DateTime(2026, 6, 28), last.Date);
      Assert.Equal(750000m, last.Amount);

      Assert.Equal(
        new[] { "Acme Holdings", "Birchwood Partners", "Cobalt Ventures", "Dunmore Capital" },
        report.Transactions.Select(t => t.Client).Distinct().ToArray());
    }

    // --- investors-by-deal.xlsx --------------------------------------------------------------------
    //
    // The repeating-block report: one declared deal block (code row, column-header row, N transaction
    // rows) applied N times, with block lengths discovered per block.

    private static SuperRegion<Region3<Region, Region, Region>> BuildInvestorsByDeal()
    {
      var blockBuilder =
        Vertical(
          SkipBlankRows(),
          RowsWhileAnyValue().ToAreaStrategy(),
          Builder(0, 0, 1, 1),
          Builder(RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue()),
          Builder(RowsWhileAnyValue().ToAreaStrategy()));

      return Repeat(blockBuilder).Build(InvestorsByDeal());
    }

    [Fact]
    public void InvestorsByDeal_YieldsOneBlockPerDeal()
    {
      var deals = BuildInvestorsByDeal().Subregions;

      Assert.Equal(
        new[] { "ATLAS-2024", "HELIOS-2025", "KESTREL-2025" },
        deals.Select(d => d.Subregion1.Space[0, 0].GetString()).ToArray());
    }

    [Fact]
    public void InvestorsByDeal_DiscoversEachBlocksTransactionCount()
    {
      var deals = BuildInvestorsByDeal().Subregions;

      Assert.Equal(
        new[] { 3, 5, 2 },
        deals.Select(d => d.Subregion3.Space.Area.Size.Height).ToArray());
    }

    [Fact]
    public void InvestorsByDeal_RepeatsTheColumnHeaderRowInEveryBlock()
    {
      var deals = BuildInvestorsByDeal().Subregions;

      Assert.All(deals, deal => Assert.Equal(
        new[] { "Account Key", "Fund Code", "Name", "Transaction Type", "Amount", "Transfer Date" },
        deal.Subregion2.Rows().Single().Select(v => v.GetString()).ToArray()));
    }

    [Fact]
    public void InvestorsByDeal_MapsBlocksToTypedDeals()
    {
      var deals = BuildInvestorsByDeal().Subregions
        .Select(block => block.Map((code, _, transactions, _) => new
        {
          Code = code.Space[0, 0].GetString(),
          Transactions = transactions.Rows()
            .Select(r => new
            {
              Account = r[0].GetString(),
              Name = r[2].GetString(),
              Type = r[3].GetString(),
              Amount = r[4].GetDecimal(),
              Date = r[5].GetDate(),
            })
            .ToArray(),
        }))
        .ToArray();

      var atlas = deals[0];
      Assert.Equal("ATLAS-2024", atlas.Code);
      Assert.Equal(3, atlas.Transactions.Length);
      Assert.Equal("ACCT-10001", atlas.Transactions[0].Account);
      Assert.Equal("Birchwood Partners LP", atlas.Transactions[0].Name);
      Assert.Equal(250000m, atlas.Transactions[0].Amount);
      Assert.Equal(new DateTime(2026, 3, 12), atlas.Transactions[0].Date);
      Assert.Equal("Transfer In", atlas.Transactions[2].Type);
      Assert.Equal(80000m, atlas.Transactions[2].Amount);

      var helios = deals[1];
      Assert.Equal("HELIOS-2025", helios.Code);
      Assert.Equal(5, helios.Transactions.Length);
      Assert.Equal("Harlan Endowment", helios.Transactions[2].Name);
      Assert.Equal(-75000m, helios.Transactions[2].Amount);
      Assert.Equal(new DateTime(2026, 4, 15), helios.Transactions[2].Date);
      Assert.Equal(95000.5m, helios.Transactions[4].Amount);

      var kestrel = deals[2];
      Assert.Equal("KESTREL-2025", kestrel.Code);
      Assert.Equal(2, kestrel.Transactions.Length);
      Assert.Equal("Dunmore Capital LLC", kestrel.Transactions[0].Name);
      Assert.Equal(62500.25m, kestrel.Transactions[1].Amount);
      Assert.Equal(new DateTime(2026, 5, 9), kestrel.Transactions[1].Date);

      Assert.Equal(1_897_500.75m, deals.SelectMany(d => d.Transactions).Sum(t => t.Amount));
    }
  }
}
