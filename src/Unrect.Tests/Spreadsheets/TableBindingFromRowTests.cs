using System;
using System.IO;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// <c>Column(f =&gt; f.IsDeprecated, row =&gt; …)</c>: one member of a bound record filled from the
  /// row rather than from a column, every other member still binding by its caption with nothing
  /// said. The case it exists for is a ledger whose deprecated records are the rows set in red.
  /// </summary>
  public class TableBindingFromRowTests
  {
    private static ISpreadsheetSpace Ledger()
      => SpreadsheetSpace.CreateWithFormulas(Path.Combine(AppContext.BaseDirectory, "TestData", "formatting.xlsx"), "Ledger");

    public sealed record Entry(string Account, decimal Amount, string Note, bool IsDeprecated);

    public sealed class EntryClass
    {
      public string Account { get; init; } = "";

      public decimal Amount { get; init; }

      public string Note { get; init; } = "";

      public CellColor Ink { get; init; }
    }

    [Fact]
    public void OneMemberIsDeclaredAndTheRestBindAsTheyAlwaysDid()
    {
      var ledger = Table<Entry>(bind => bind
        .Column(e => e.IsDeprecated, row => row["Account"].Font().Color == CellColor.Red));

      var entries = ledger.Map(Ledger());

      Assert.Equal(new[] { "1000", "1001", "1002", "1003" }, entries.Select(e => e.Account));
      Assert.Equal(new[] { "1001" }, entries.Where(e => e.IsDeprecated).Select(e => e.Account));
      Assert.Equal("deprecated", entries.Single(e => e.IsDeprecated).Note);
    }

    [Fact]
    public void AMemberFilledFromTheRowIsNotAskedForACaption()
    {
      // Nothing in the header says "IsDeprecated", and strictness would say so of any other member.
      var unbound = Assert.Throws<ProjectionException>(() => Table<Entry>().Map(Ledger()));

      Assert.Contains("no column binds Entry.IsDeprecated", unbound.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TheReadingMayBeOfAnyTypeAndThroughInitProperties()
    {
      // Not one of the six kinds a column can be: the member's type is whatever its reading returns.
      var ledger = Table<EntryClass>(bind => bind.Column(e => e.Ink, row => row[0].Font().Color));

      Assert.Equal(
        new[] { CellColor.Automatic, CellColor.Red, CellColor.Automatic, CellColor.Automatic },
        ledger.Map(Ledger()).Select(e => e.Ink));
    }

    [Fact]
    public void AReadingThatFailsIsLocatedUnderTheMembersName()
    {
      // "Note" holds text, so reading it as a number fails the way any cell read does: the path
      // names the member, the message the cell.
      var ledger = Table<Entry>(bind => bind
        .Column(e => e.IsDeprecated, row => row["Note"].Decimal() > 0));

      var failure = Assert.Throws<ProjectionException>(() => ledger.Map(Ledger()));

      Assert.Equal("Table<Entry>[0] -> member 'IsDeprecated'", failure.Path);
      Assert.Contains("expected Number at C2, found Text", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABindingIsRefusedWhereItIsWritten()
    {
      Assert.Equal("read", Assert.Throws<ArgumentNullException>(() =>
        Table<Entry>(bind => bind.Column(e => e.IsDeprecated, (Func<TableRow<ISpreadsheetSpace>, bool>)null!))).ParamName);

      Assert.Contains("bound twice", Assert.Throws<ArgumentException>(() =>
        Table<Entry>(bind => bind.Column(e => e.IsDeprecated, row => true).Column(e => e.IsDeprecated, 3))).Message, StringComparison.Ordinal);

      Assert.Contains("both bound and ignored", Assert.Throws<ArgumentException>(() =>
        Table<EntryClass>(bind => bind.Column(e => e.Ink, row => default(CellColor)).Ignore(e => e.Ink))).Message, StringComparison.Ordinal);
    }
  }
}
