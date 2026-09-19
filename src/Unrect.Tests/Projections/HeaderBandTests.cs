using System;
using System.IO;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A header more than one row tall: the last row holds the captions, and every row above it is a
  /// row of BANDS, each naming the columns beneath it. A banded column's name is its bands and its
  /// caption joined — <c>From Id</c> — which is what lets every reading by name work over it
  /// unchanged, and what makes two columns captioned <c>Id</c> two different columns.
  /// </summary>
  public class HeaderBandTests
  {
    /// <summary>
    /// The owner's workbook: a blank first row and column, "From" and "To" merged over two columns
    /// each, and the captions Id, Code, Id, Code beneath them.
    /// </summary>
    private static ISheetCells Workbook()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "multi-header-table.xlsx"), "Sheet1");

    [Fact]
    public void ABandedColumnIsNamedByItsBandAndItsCaption()
    {
      // A merged cell reads as a value in its first cell and blanks beside it, so a band reaches
      // rightward over the columns after it until the next band says otherwise. The blank column
      // the table is indented by has no caption, and no band names a column that has none.
      Assert.Equal(
        new[] { "", "From Id", "From Code", "To Id", "To Code" },
        Table(2, t => t.ColumnNames).Map(Workbook()));
    }

    [Fact]
    public void WhichIsWhatTellsTwoColumnsWithOneCaptionApart()
    {
      var transfers = Table(2, r => new
      {
        FromId = r["From Id"].Integer(),
        FromCode = r["From Code"].Text(),
        ToId = r["To Id"].Integer(),
        ToCode = r["To Code"].Text(),
      }).Map(Workbook());

      var only = Assert.Single(transfers);

      Assert.Equal((1, "FEP", 2, "FCP"), (only.FromId, only.FromCode, only.ToId, only.ToCode));
    }

    [Fact]
    public void ACaptionTwoBandsShareIsStillRefusedWithoutItsBand()
    {
      // Fail fast: "Id" alone is two columns, and picking the first would hide the day a report
      // grows a second one.
      var failure = Assert.Throws<ProjectionException>(() => Table(2, r => r["Id"].Integer()).Map(Workbook()));

      Assert.Contains("column 'Id' appears at indices 1 and 3", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A column to the left of every band, and one to the right of the last, neither under a band of its own.</summary>
    private static ISheetCells Mixed() => SheetGrid.Of(new object?[,]
    {
      { null, "From", null, "To", null, null },
      { "Date", "Id", "Code", "Id", "Code", "Notes" },
      { new DateTime(2026, 1, 5), 1m, "FEP", 2m, "FCP", "ok" },
    });

    [Fact]
    public void AColumnLeftOfEveryBandKeepsItsCaption()
      => Assert.Equal("Date", Table(2, t => t.ColumnNames).Map(Mixed())[0]);

    [Fact]
    public void ACaptionThatIsUniqueNamesItsColumnWithoutItsBand()
    {
      // Without merge ranges there is no telling where "To" ends, so Notes reads as under it — and
      // it does not matter: Notes is one column whichever way it is asked for.
      var read = Assert.Single(Table(2, r => (r["Notes"].Text(), r["To Notes"].Text(), r["Date"].Date().Year)).Map(Mixed()));

      Assert.Equal(("ok", "ok", 2026), read);
    }

    [Fact]
    public void BandsStackOutermostFirst()
    {
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Actual", null, "Budget", null },
        { "Q1", "Q2", "Q1", "Q2" },
        { "Net", "Net", "Net", "Net" },
        { 1m, 2m, 3m, 4m },
      });

      Assert.Equal(
        new[] { "Actual Q1 Net", "Actual Q2 Net", "Budget Q1 Net", "Budget Q2 Net" },
        Table(3, t => t.ColumnNames).Map(sheet));

      Assert.Equal(3m, Assert.Single(Table(3, r => r["Budget Q1 Net"].Decimal()).Map(sheet)));
    }

    [Fact]
    public void TheBodyStartsUnderTheWholeHeader()
    {
      // The band row is header, not data: one body row, read from the row under the captions.
      Assert.Equal(1, Table(2, t => t.RowCount).Map(Workbook()));
      Assert.Equal(new[] { 1 }, Table(2, r => r["From Id"].Integer()).Map(Workbook()).ToArray());
    }

    [Fact]
    public void ARowSlotTableReadsUnderBandsToo()
    {
      // The streaming rung, which reads its header through ColumnLabels rather than a view: the
      // same parse, so the same names.
      var ids = Table(2, Record((TableRow<ISheetCells> r) => r["To Id"].Integer())).Map(Workbook());

      Assert.Equal(new[] { 2 }, ids);
    }

    [Fact]
    public void AFailureCitesTheCaptionRow()
    {
      var failure = Assert.Throws<ProjectionException>(() => Table(2, r => r["From Id"].Text()).Map(Workbook()));

      Assert.Contains("expected Text at B4, found Number", failure.Message, StringComparison.Ordinal);
    }
  }
}
