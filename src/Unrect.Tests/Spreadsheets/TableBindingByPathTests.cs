using System;
using System.IO;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Interactive.ExploratoryBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// <c>Table&lt;T&gt;(headerRows: 2, …)</c> over a banded header. A member binds to a caption that
  /// is unique as it always has; a flat member never binds across a band by itself, so a column
  /// that shares its caption with another is bound by its path, and a path the declared header
  /// could not hold is refused where it is written.
  /// </summary>
  public class TableBindingByPathTests
  {
    private static ICellSpace Workbook()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "multi-header-table.xlsx"), "Sheet1");

    public sealed record Transfer(int FromId, string FromCode, int ToId, string ToCode);

    public sealed record Dated(DateTime Date, int FromId, string? Notes);

    public sealed record Wider(int FromId, int ToId, decimal? Rate);

    [Fact]
    public void AFlatTypeBindsABandedTableByPath()
    {
      var transfers = Table<Transfer>(2, bind => bind
        .Column(t => t.FromId, "From", "Id")
        .Column(t => t.FromCode, "From", "Code")
        .Column(t => t.ToId, "To", "Id")
        .Column(t => t.ToCode, "To", 1));

      Assert.Equal(new Transfer(1, "FEP", 2, "FCP"), Assert.Single(transfers.Map(Workbook())));
    }

    [Fact]
    public void AFlatMemberBindsToTheColumnWhoseWholePathItsNameRunsTogether()
    {
      // The shape these reports are usually read into: FromId for the column at From, Id. It is a
      // way of MATCHING a member to a path — case and spaces ignored, as for any caption — and
      // never a way of naming a column: a row still reads row["From", "Id"], not row["FromId"].
      Assert.Equal(new Transfer(1, "FEP", 2, "FCP"), Assert.Single(Table<Transfer>(2).Map(Workbook())));

      var failure = Assert.Throws<ProjectionException>(() =>
        ProjectionBuilders<ICellSpace>.Table(2, r => r["FromId"].Integer()).Map(Workbook()));

      Assert.Contains("there is no column named 'FromId'", failure.Message, StringComparison.Ordinal);
    }

    public sealed record Net(decimal Q1Net);

    [Fact]
    public void TheWholePathAndNothingShorter()
    {
      // Q1Net does not reach under Actual: a name that skipped a band could mean either Q1.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Actual", "Budget" },
        { "Q1", "Q1" },
        { "Net", "Net" },
        { 1m, 2m },
      });

      Assert.Contains("no column binds Net.Q1Net", Assert.Throws<ProjectionException>(() => Table<Net>(3).Map(sheet)).Message, StringComparison.Ordinal);
    }

    public sealed record Mixed(int FromId);

    [Fact]
    public void OneNameOneColumn()
    {
      // A caption that literally says "From Id" AND a column at From, Id: both answer to FromId,
      // which is two columns, refused like any other two — the day a report grows the second one
      // is the day to hear about it.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { null, "From", null },
        { "From Id", "Id", "Code" },
        { 9m, 1m, "FEP" },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table<Mixed>(2).Map(sheet));

      Assert.Contains("Mixed.FromId matches the columns at A2 ('From Id') and B2 ([\"From\", \"Id\"])", failure.Message, StringComparison.Ordinal);
    }

    public sealed record Missing(int FromId, decimal FromRate);

    [Fact]
    public void AMemberThatFindsNothingIsToldTheColumnsAsTheyAre()
    {
      // Under bands the captions alone would read 'Id', 'Code', 'Id', 'Code' and explain nothing:
      // the columns are said by their paths, with how a flat name is matched against them.
      var failure = Assert.Throws<ProjectionException>(() => Table<Missing>(2).Map(Workbook()));

      Assert.Contains(
        "no column binds Missing.FromRate; the table's columns are [\"From\", \"Id\"], [\"From\", \"Code\"], [\"To\", \"Id\"], [\"To\", \"Code\"] "
        + "— a member binds to a caption, or to a whole path run together (FromId for [\"From\", \"Id\"])",
        failure.Message,
        StringComparison.Ordinal);
    }

    public sealed record City(decimal NewYorkCityTotal);

    [Fact]
    public void TwoPathsThatRunTogetherAlikeAreRefusedWithThePathThatSaysWhich()
    {
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "New York", "New York City" },
        { "City Total", "Total" },
        { 1m, 2m },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table<City>(2).Map(sheet));

      Assert.Contains(
        "City.NewYorkCityTotal matches the columns at A2 ([\"New York\", \"City Total\"]) and B2 ([\"New York City\", \"Total\"])",
        failure.Message,
        StringComparison.Ordinal);
      Assert.Contains("Bind it by its path with Column(t => t.NewYorkCityTotal, \"New York\", \"City Total\")", failure.Message, StringComparison.Ordinal);

      Assert.Equal(2m, Assert.Single(Table<City>(2, bind => bind.Column(c => c.NewYorkCityTotal, "New York City", "Total")).Map(sheet)).NewYorkCityTotal);
    }

    [Fact]
    public void ACaptionThatIsUniqueStillBindsWithNothingDeclared()
    {
      var sheet = SheetGrid.Of(new object?[,]
      {
        { "Date", "From", null, "To", null, null },
        { null, "Id", "Code", "Id", "Code", "Notes" },
        { new DateTime(2026, 1, 5), 1m, "FEP", 2m, "FCP", "ok" },
      });

      // Date is a label over nothing but blanks — its own column's name; Notes is unique, whatever
      // band the convention put it under. Only the Id needs its path.
      var rows = Table<Dated>(2, bind => bind.Column(d => d.FromId, "From", "Id")).Map(sheet);

      Assert.Equal(new Dated(new DateTime(2026, 1, 5), 1, "ok"), Assert.Single(rows));
    }

    [Fact]
    public void APathTheDeclaredHeaderCouldNotHoldIsRefusedWhereItIsWritten()
    {
      // One header row has no bands for a two-step path to go through: wrong whatever file it
      // meets, so it is refused before one is opened — with and without headerRows said.
      var bare = Assert.Throws<ArgumentException>(() => Table<Transfer>(bind => bind.Column(t => t.FromId, "From", "Id")));
      var said = Assert.Throws<ArgumentException>(() => Table<Transfer>(1, bind => bind.Column(t => t.FromId, "From", "Id")));

      Assert.Contains(
        "Transfer.FromId is bound to a path of 2 steps, and the table declares 1 header row, which is a header with no bands; declare headerRows: 2, or shorten the path.",
        bare.Message,
        StringComparison.Ordinal);
      Assert.Equal(bare.Message, said.Message);
    }

    [Fact]
    public void APathTheFileDoesNotHoldNamesTheMemberAndWhatIsThere()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table<Transfer>(2, bind => bind
          .Column(t => t.FromId, "Via", "Id")
          .Column(t => t.FromCode, "From", "Code")
          .Column(t => t.ToId, "To", "Id")
          .Column(t => t.ToCode, "To", "Code")).Map(Workbook()));

      Assert.Contains(
        "Transfer.FromId is bound to [\"Via\", \"Id\"]: there is no \"Via\" under the header; under it are \"From\", \"To\"",
        failure.Message,
        StringComparison.Ordinal);
    }

    [Fact]
    public void AFailureInAColumnBoundByPathNamesThePath()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table<Transfer>(2, bind => bind
          .Column(t => t.FromId, "From", "Code")
          .Column(t => t.FromCode, "From", "Code")
          .Column(t => t.ToId, "To", "Id")
          .Column(t => t.ToCode, "To", "Code")).Map(Workbook()));

      Assert.Equal("Table<Transfer>[0] -> column \"From\", \"Code\"", failure.Path);
      Assert.Contains("expected Number at C4, found Text", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLooseTableTakesTheSameHeader()
    {
      var read = LooseTable<Wider>(2, bind => bind.Column(t => t.ToId, "To", "Id")).MapWithDiagnostics(Workbook());

      Assert.Equal(new Wider(1, 2, null), Assert.Single(read.Value));
      Assert.Contains(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning && d.Message.IndexOf("no column binds Wider.Rate", StringComparison.Ordinal) >= 0);
    }

    [Fact]
    public void ABindingIsRefusedWhereItIsWritten()
    {
      Assert.Contains("bound twice", Assert.Throws<ArgumentException>(() =>
        Table<Transfer>(2, bind => bind.Column(t => t.ToId, "To", "Id").Column(t => t.ToId, 3))).Message, StringComparison.Ordinal);
      Assert.Equal("headerRows", Assert.Throws<ArgumentOutOfRangeException>(() => Table<Transfer>(0)).ParamName);
    }
  }
}
