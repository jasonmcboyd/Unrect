using System;
using System.IO;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Addressing a column by its PATH through the header: <c>row["From", "Id"]</c>. A step is a name,
  /// the nth of a name, or a position, every number counted from zero — and a path is a list of
  /// typed values, so a caption that says "2" and a position 2 can never be mistaken for each other.
  /// </summary>
  public class HeaderPathTests
  {
    private static ICellSpace Workbook()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "multi-header-table.xlsx"), "Sheet1");

    [Fact]
    public void TheOwnersCallSite()
    {
      var report = Table(2, r => new
      {
        FromId = r["From", "Id"].Integer(),
        FromCode = r["From", "Code"].Text(),
        ToId = r["To", "Id"].Integer(),
        ToCode = r["To", "Code"].Text(),
      });

      var only = Assert.Single(report.Map(Workbook()));

      Assert.Equal((1, "FEP", 2, "FCP"), (only.FromId, only.FromCode, only.ToId, only.ToCode));
    }

    /// <summary>A column under no band, two bands that share their captions, a band that appears twice, and a caption that says "1".</summary>
    private static ICellSpace Sheet() => SheetGrid.Of(new object?[,]
    {
      { null, "From", null, "To", null, "Totals", null, "Totals", null },
      { "Id", "Id", "Code", "Id", "1", "Net", "Tax", "Net", "Tax" },
      { 100m, 1m, "FEP", 2m, "one", 10m, 11m, 20m, 21m },
    });

    private static T Read<T>(Func<TableRow<ICellSpace>, T> read) => Assert.Single(Table(2, read).Map(Sheet()));

    [Fact]
    public void AStepIsANameTheNthOfANameOrAPosition()
    {
      Assert.Equal(1m, Read(r => r["From", "Id"].Decimal()));
      Assert.Equal(2m, Read(r => r["To", "Id"].Decimal()));

      // Every number is an index and indexes start at zero.
      Assert.Equal("FEP", Read(r => r["From", 1].Text()));
      Assert.Equal(20m, Read(r => r[("Totals", 1), "Net"].Decimal()));
      Assert.Equal(11m, Read(r => r[("Totals", 0), 1].Decimal()));
    }

    [Fact]
    public void ANameIsNeverMistakenForAPosition()
    {
      // Under "To": a caption that SAYS "1", and position 1. They are the same column here and
      // would not be in the next file; what matters is that they are different questions.
      Assert.Equal("one", Read(r => r["To", "1"].Text()));
      Assert.Equal("one", Read(r => r["To", 1].Text()));
      Assert.Equal(2m, Read(r => r["To", 0].Decimal()));
    }

    [Fact]
    public void AOneStepPathIsAName()
    {
      // The column whose whole path it is, before a caption somewhere under a band…
      Assert.Equal(100m, Read(r => r["Id"].Decimal()));

      // …and with an index, every column that carries the caption, in column order.
      Assert.Equal(100m, Read(r => r[("Id", 0)].Decimal()));
      Assert.Equal(1m, Read(r => r[("Id", 1)].Decimal()));
      Assert.Equal(2m, Read(r => r[("Id", 2)].Decimal()));

      // A caption that is unique needs no band said.
      Assert.Equal("FEP", Read(r => r["Code"].Text()));
    }

    private static string Problem(Func<TableRow<ICellSpace>, object> read)
      => Assert.Throws<ProjectionException>(() => Table(2, read).Map(Sheet())).Message;

    [Fact]
    public void EveryMissSaysWhatIsThere()
    {
      Assert.Contains("there is no \"Rate\" under \"From\"; under it are \"Id\", \"Code\"", Problem(r => r["From", "Rate"]), StringComparison.Ordinal);
      Assert.Contains("there is no \"Via\" under the header; under it are \"Id\", \"From\", \"To\", \"Totals\"", Problem(r => r["Via", "Id"]), StringComparison.Ordinal);

      // Fail fast: a name several things answer to is refused, with the ways to say which.
      Assert.Contains(
        "\"Totals\" is 2 things under the header: [\"Totals\", \"Net\"] and [\"Totals\", \"Net\"]; say which by its path, or (\"Totals\", 0) for the first",
        Problem(r => r["Totals", "Net"]),
        StringComparison.Ordinal);
      Assert.Contains("column 'Net' appears at indices 5 and 7; use the index, or its path: [\"Totals\", \"Net\"] or [\"Totals\", \"Net\"].", Problem(r => r["Net"]), StringComparison.Ordinal);

      Assert.Contains("\"From\" spans 2 columns (0 to 1), so there is no position 2", Problem(r => r["From", 2]), StringComparison.Ordinal);
      Assert.Contains("there are 3 \"Id\" under the header (0 to 2), so there is no (\"Id\", 3)", Problem(r => r[("Id", 3)]), StringComparison.Ordinal);
      Assert.Contains("(\"From\", 0) is a band over 2 columns, not a column; say which, by name or by position", Problem(r => r[("From", 0)]), StringComparison.Ordinal);
      Assert.Contains("\"Id\" is a column, not a band, so nothing is under it", Problem(r => r["Id", "Code"]), StringComparison.Ordinal);
      Assert.Contains("0 is a position, which names a column, so nothing can follow it in a path", Problem(r => r[0, "Id"]), StringComparison.Ordinal);
    }

    [Fact]
    public void APathDeeperThanTheHeaderFailsAtTheHeaderWhateverTheDataIs()
    {
      // One header row, so no bands: the path can never be right, and says so on the first row.
      var sheet = SheetGrid.Of(new object?[,] { { "Id", "Code" }, { 1m, "x" } });

      var failure = Assert.Throws<ProjectionException>(() => Table(1, r => r["From", "Id"].Decimal()).Map(sheet));

      Assert.Contains("there is no \"From\" under the header", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheCaptionsABindIsHandedTakeTheSamePaths()
    {
      var table = Table(headerRows: 2, eachRow: captions =>
      {
        Assert.Equal(2, captions.Depth);
        Assert.True(captions.Has("To", "1"));
        Assert.False(captions.Has("To", "Rate"));
        Assert.False(captions.Has("Via", "Id"));

        // A band as labels in their own right: the same columns, by the names they have there.
        var totals = captions.Under(("Totals", 1));

        Assert.Equal(new[] { 7, 8 }, new[] { totals["Net"], totals["Tax"] });
        Assert.Equal(new[] { "", "", "", "", "", "", "", "Net", "Tax" }, totals.Labels);

        var toId = captions["To", "Id"];

        return Right(toId).Of(Range(1, 1, cell => cell[0, 0].AsText()));
      });

      Assert.Equal(new[] { "2" }, table.Map(Sheet()));
    }

    [Fact]
    public void UnderRefusesWhatIsNotABand()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table(headerRows: 2, eachRow: captions => Right(captions.Under("Id")["x"]).Of(Range(1, 1, cell => cell[0, 0].AsText()))).Map(Sheet()));

      Assert.Contains("[\"Id\"] is a column, not a band, so there is nothing under it", failure.Message, StringComparison.Ordinal);
    }
  }
}
