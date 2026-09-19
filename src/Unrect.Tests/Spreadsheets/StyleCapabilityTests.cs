using System;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// What a cell looks like, through a declaration. The case it exists for is a ledger in which the
  /// rows somebody coloured red are records deprecated in the system they came from: nothing about
  /// their content says so, and the reader decides what red means.
  /// </summary>
  public class StyleCapabilityTests
  {
    private static string Workbook => Path.Combine(AppContext.BaseDirectory, "TestData", "formatting.xlsx");

    public sealed record Entry(string Account, decimal Amount, bool Deprecated);

    [Fact]
    public void ARowsColourBecomesAPropertyTheCallerFiltersOn()
    {
      var ledger = Table(r => new Entry(
        r["Account"].Text(),
        r["Amount"].Decimal(),
        Deprecated: r["Account"].Font().Color == CellColor.Red));

      var entries = ledger.Map(SpreadsheetSpace.CreateWithFormulas(Workbook, "Ledger"));

      Assert.Equal(new[] { "1000", "1001", "1002", "1003" }, entries.Select(e => e.Account));
      Assert.Equal(new[] { "1001" }, entries.Where(e => e.Deprecated).Select(e => e.Account));
      Assert.Equal(80m, entries.Where(e => !e.Deprecated).Sum(e => e.Amount));
    }

    [Fact]
    public void ASpreadsheetSpaceAnswersForItsFormattingInRootCoordinates()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(Workbook, "Ledger");

      Assert.IsAssignableFrom<IStyleSpace>(sheet);
      Assert.True(sheet.FontAt(0, 0).Bold);
      Assert.Equal(CellColor.Red, sheet.FontAt(1, 2).Color);
      Assert.Equal(CellColor.FromRgb(0xFFFF00), sheet.FillAt(2, 4).Color);
      Assert.True(sheet.FontAt(0, 4).Strikethrough);

      // A point read is the same read, whatever region the point was minted from.
      var lower = Plane<ISpreadsheetSpace>.Of(sheet).Slice(new Offset(1, 2));

      Assert.Equal(CellColor.Red, lower[0, 0].Font().Color);
      Assert.Throws<OutOfBoundsException>(() => sheet.FontAt(3, 0));
    }

    [Fact]
    public void ASheetOpenedForItsValuesAloneDoesNotClaimToKnow()
    {
      // Honest absence: the plain door never looked at the formatting, so what it hands back is not
      // a style space at all — and a declaration that asks what a cell looks like does not compile
      // against it, rather than being told "black" everywhere.
      var plain = SpreadsheetSpace.Create(Workbook, "Ledger");

      Assert.False(plain is IStyleSpace);
    }
  }
}
