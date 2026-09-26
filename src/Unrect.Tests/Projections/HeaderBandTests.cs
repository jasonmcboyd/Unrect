using System;
using System.IO;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A header more than one row tall: the last row holds the captions, and every row above it is a
  /// row of BANDS. What a header yields is each column's PATH — the distinct regions it passes
  /// through from the top of the header to the bottom — and a path is a list, never a string: two
  /// columns captioned <c>Id</c> are <c>From, Id</c> and <c>To, Id</c>, and nothing joins them into
  /// text a caption could be mistaken for.
  /// <para>
  /// From the cells alone a band merged over two columns and a band sitting over one are the same
  /// thing, so the regions are reconstructed by the convention every reader of such headers uses —
  /// see <c>HeaderRegions</c>. These pin the convention; addressing a column BY its path is the next
  /// step's.
  /// </para>
  /// </summary>
  public class HeaderBandTests
  {
    /// <summary>
    /// The owner's workbook: a blank first row and column, "From" and "To" merged over two columns
    /// each, and the captions Id, Code, Id, Code beneath them.
    /// </summary>
    private static ICellSpace Workbook()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "multi-header-table.xlsx"), "Sheet1");

    private static string[] Paths(int headerRows, ICellSpace sheet)
      => Table(headerRows, t => t.ColumnPaths).Map(sheet).Select(path => string.Join(" / ", path)).ToArray();

    [Fact]
    public void AColumnsPathIsItsBandAndThenItsCaption()
    {
      // A merged cell reads as a value in its first cell and blanks beside it, so a band reaches
      // rightward over the blank cells after it. The blank column the table is indented by has no
      // caption, and a band never names a column that has none.
      Assert.Equal(new[] { "", "From / Id", "From / Code", "To / Id", "To / Code" }, Paths(2, Workbook()));
    }

    [Fact]
    public void AColumnsNameIsItsOwnCaptionAndNothingJoined()
    {
      Assert.Equal(new[] { "", "Id", "Code", "Id", "Code" }, Table(2, t => t.ColumnNames).Map(Workbook()));

      // A path is never flattened into text and read back: "From Id" is a caption that says exactly
      // that, and there is none.
      var failure = Assert.Throws<ProjectionException>(() => Table(2, r => r["From Id"].Integer()).Map(Workbook()));

      Assert.Contains("there is no column named 'From Id'", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACaptionTwoBandsShareIsRefusedByItself()
    {
      // Fail fast: "Id" alone is two columns, and picking the first would hide the day a report
      // grows a second one.
      var failure = Assert.Throws<ProjectionException>(() => Table(2, r => r["Id"].Integer()).Map(Workbook()));

      Assert.Contains("column 'Id' appears at indices 1 and 3", failure.Message, StringComparison.Ordinal);
    }

    /// <summary>A label merged DOWN, a caption under no band, the bands, and a caption to the right of the last.</summary>
    private static ICellSpace Mixed() => SheetGrid.Of(new object?[,]
    {
      { "Date", null, "From", null, "To", null, null },
      { null, "Rate", "Id", "Code", "Id", "Code", "Notes" },
      { new DateTime(2026, 1, 5), 0.5m, 1m, "FEP", 2m, "FCP", "ok" },
    });

    [Fact]
    public void ALabelOverNothingButBlanksIsItsOwnColumnsNameAndSpreadsNowhere()
    {
      // "Date" merged down two rows is ONE region, so its column's path is one step — and because
      // it has nothing beneath it, it is a column's name rather than a band, and "Rate" beside it is
      // under nothing. Without merge ranges there is no telling where "To" ends, so "Notes" reads as
      // under it; that is the convention, and a unique caption is found either way.
      Assert.Equal(
        new[] { "Date", "Rate", "From / Id", "From / Code", "To / Id", "To / Code", "To / Notes" },
        Paths(2, Mixed()));

      var read = Assert.Single(Table(2, r => (r["Date"].Date().Year, r["Rate"].Decimal(), r["Notes"].Text())).Map(Mixed()));

      Assert.Equal((2026, 0.5m, "ok"), read);
    }

    [Fact]
    public void AColumnUnderNoBandIsFoundBeforeACaptionSomewhereUnderOne()
    {
      // Three columns captioned Id, and only one of them is a column whose whole path is "Id". A
      // bare name means that one; the other two are reached through their bands.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { null, "From", null, "To", null },
        { "Id", "Id", "Code", "Id", "Code" },
        { 7m, 1m, "FEP", 2m, "FCP" },
      });

      Assert.Equal(7m, Assert.Single(Table(2, r => r["Id"].Decimal()).Map(sheet)));
    }

    [Fact]
    public void ASpacerColumnEndsABand()
    {
      // A band never claims a column with nothing beneath it. The table's width is declared,
      // because a column blank all the way down would otherwise end the TABLE — from the cells, a
      // spacer inside one table and the gap between two are the same thing, and the declaration is
      // what says which.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "From", null, null, "To", null },
        { "Id", "Code", null, "Id", "Code" },
        { 1m, "FEP", null, 2m, "FCP" },
      });

      var paths = Sized(RowsWhileAnyIsNotBlank()).Of(Table(2, t => t.ColumnPaths)).Map(sheet).Select(path => string.Join(" / ", path));

      Assert.Equal(new[] { "From / Id", "From / Code", "", "To / Id", "To / Code" }, paths);
    }

    [Fact]
    public void BandsStackOutermostFirstAndNeverReachPastTheRegionOverThem()
    {
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Actual", null, "Budget", null },
        { "Q1", null, "Q1", "Q2" },
        { "Net", "Tax", "Net", "Net" },
        { 1m, 2m, 3m, 4m },
      });

      // "Q1" under Actual reaches over Tax; "Q1" under Budget stops where "Q2" starts; and the first
      // Q1 does not reach into Budget, whose own Q1 is a different region that says the same thing.
      Assert.Equal(
        new[] { "Actual / Q1 / Net", "Actual / Q1 / Tax", "Budget / Q1 / Net", "Budget / Q2 / Net" },
        Paths(3, sheet));
    }

    [Fact]
    public void AHeaderOfYearsIsAHeader()
    {
      // A label is what a header cell SAYS, whatever its kind: period columns are captions, and a
      // band row of years is a band row.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Fund", 2025, null, 2026, null },
        { null, "Q1", "Q2", "Q1", "Q2" },
        { "Alpha", 1m, 2m, 3m, 4m },
      });

      Assert.Equal(new[] { "Fund", "2025 / Q1", "2025 / Q2", "2026 / Q1", "2026 / Q2" }, Paths(2, sheet));
    }

    [Fact]
    public void TheBodyStartsUnderTheWholeHeader()
    {
      // The band row is header, not data: one body row, read from the row under the captions.
      Assert.Equal(1, Table(2, t => t.RowCount).Map(Workbook()));
      Assert.Equal(new[] { 1 }, Table(2, r => r[1].Integer()).Map(Workbook()).ToArray());
    }

    [Fact]
    public void ARowSlotTableReadsUnderBandsToo()
    {
      // The streaming rung, which reads its header through ColumnLabels rather than a view: the
      // same parse, so the same paths.
      var labels = ColumnLabels(2).Map(Workbook());

      Assert.Equal(new[] { "", "From / Id", "From / Code", "To / Id", "To / Code" }, labels.Paths.Select(path => string.Join(" / ", path)));
      Assert.Equal(new[] { 2 }, Table(2, Record((TableRow<ICellSpace> r) => r[3].Integer())).Map(Workbook()));
    }

    [Fact]
    public void AFailureCitesTheCellItIsAbout()
    {
      var failure = Assert.Throws<ProjectionException>(() => Table(2, r => r[1].Text()).Map(Workbook()));

      Assert.Contains("expected Text at B4, found Number", failure.Message, StringComparison.Ordinal);
    }
  }
}
