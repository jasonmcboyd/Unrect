using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// A composed area strategy settles on the same rectangle through either door. The composition
  /// that matters is one whose first half reads nothing — two rows taken outright — so every cell
  /// the second half reads is read inside a region the first half never touched; the contrast is a
  /// first half that walks the sheet itself.
  /// </summary>
  public class ComposedAreaAcrossDoorsTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    /// <summary><paramref name="area"/> applied to the tall ledger through the streaming door: the extent it settled on.</summary>
    private static string Streamed(IAreaStrategy area)
    {
      using var book = Workbook.Open(Path("tall-ledger.xlsx"));

      return Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(book.Sheet("Ledger"));
    }

    /// <summary>The same declaration through the eager door.</summary>
    private static string Eager(IAreaStrategy area)
      => Sized(area).Of(Range(block => $"{block.Width}x{block.Height}")).Map(
        SpreadsheetSpace.Create(Path("tall-ledger.xlsx"), "Ledger"));

    /// <summary>Two rows taken without reading anything, then the columns measured inside them.</summary>
    private static IAreaStrategy FirstHalfReadsNothing()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRows(2), ColumnStrategies.TakeColumnsWhileAnyIsNotBlank());

    /// <summary>The contrast: a first half that walks the sheet itself, so the reading is its own.</summary>
    private static IAreaStrategy FirstHalfReads()
      => AreaStrategies.RowsThenColumns(RowStrategies.TakeRowsWhileAnyIsNotBlank(), ColumnStrategies.TakeColumnsWhileAnyIsNotBlank());

    [Theory]
    [InlineData("first-half-reads-nothing", "3x2")]
    [InlineData("first-half-reads", "3x1201")]
    public void AndTheAnswerIsTheSameThroughEitherDoor(string composition, string extent)
    {
      // The two doors settle on exactly the same rectangle: a composed area strategy whose first
      // half reads nothing, or reads the whole sheet, means the same through a stream as whole.
      var area = composition == "first-half-reads-nothing" ? FirstHalfReadsNothing() : FirstHalfReads();

      Assert.Equal(extent, Eager(area));
      Assert.Equal(extent, Streamed(area));
    }
  }
}
