using System;
using System.IO;

using Unrect.Interactive;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Interactive.ScaffoldBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// A banded header scaffolds into the FLAT type that binds it: each member named as
  /// <c>Table&lt;T&gt;(headerRows)</c> will match it — band and caption run together — from the
  /// table's own header parse, so what is pasted is what reads.
  /// </summary>
  public class ScaffoldBandsTests
  {
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    private static ISheetCells Workbook()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "multi-header-table.xlsx"), "Sheet1");

    public sealed record Transfer(int FromId, string FromCode, int ToId, string ToCode);

    [Fact]
    public void TheOwnersWorkbookScaffoldsTheFlatRecordThatBindsIt()
    {
      // Nothing said about where the header is: the band row is the first row of text, and the
      // samples are read from under the WHOLE header.
      Assert.Equal(
        Lines(
          "// binds with Table<Transfer>(2): a member answers to a caption, or to its band and caption run together",
          "public sealed record Transfer(int FromId, string FromCode, int ToId, string ToCode);"),
        Workbook().ScaffoldRecord("Transfer", headerRows: 2));

      // …and the record it wrote is the one declared above, which reads the sheet with nothing bound.
      Assert.Equal(new Transfer(1, "FEP", 2, "FCP"), Assert.Single(Table<Transfer>(2).Map(Workbook())));
    }

    [Fact]
    public void AColumnUnderNoBandKeepsItsOwnNameAndAnAwkwardCaptionGetsItsPath()
    {
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Date", "From", null, null },
        { null, "Id", "Net (USD)", "Notes" },
        { new DateTime(2026, 1, 5), 1m, 2.5m, null },
      });

      Assert.Equal(
        Lines(
          "// binds with Table<Row>(2): a member answers to a caption, or to its band and caption run together",
          "// \"Net (USD)\" does not bind to FromNetUSD by name: .Column(r => r.FromNetUSD, \"From\", \"Net (USD)\")",
          "public sealed record Row(DateTime Date, int FromId, decimal FromNetUSD, string? FromNotes);"),
        sheet.ScaffoldRecord("Row", headerRows: 2));
    }

    [Fact]
    public void TheLeafFormStandsWhereTheBandedTableWill()
    {
      var report = VerticalFlow(v => new
      {
        Transfers = v.Next(ScaffoldClass("Transfer", headerRows: 2)),
      });

      Assert.Equal(
        Lines(
          "// binds with Table<Transfer>(2): a member answers to a caption, or to its band and caption run together",
          "public sealed class Transfer",
          "{",
          "    public int FromId { get; init; }",
          "    public string FromCode { get; init; } = \"\";",
          "    public int ToId { get; init; }",
          "    public string ToCode { get; init; } = \"\";",
          "}"),
        report.Map(Workbook()).Transfers);
    }

    [Fact]
    public void BandsAreReadAlongARowOnly()
    {
      var sheet = SheetGrid.Of(new object?[,] { { "A", "B" }, { "x", "y" } });

      Assert.Equal("headerRows", Assert.Throws<ArgumentException>(() => sheet.ScaffoldRecord("Row", LabelsIn.Column, headerRows: 2)).ParamName);
      Assert.Equal("headerRows", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.ScaffoldRecord("Row", headerRows: 0)).ParamName);
      Assert.Equal("headerRows", Assert.Throws<ArgumentException>(() => ScaffoldRecord("Row", LabelsIn.Column, headerRows: 2)).ParamName);
    }
  }
}
