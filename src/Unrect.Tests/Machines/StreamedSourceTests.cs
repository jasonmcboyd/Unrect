using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Machines
{
  /// <summary>
  /// The push interpreter over a source that does not retain: one forward pass, rows held only
  /// while an open machine may still read them, a cap that is a fault naming the holder, and a
  /// point read after its row left the buffer that says so.
  /// </summary>
  public class StreamedSourceTests
  {
    /// <summary>Numbered blocks of <paramref name="blockRows"/> rows separated by one blank row, <paramref name="blocks"/> of them.</summary>
    private static FakeSheet Blocks(int blocks, int blockRows)
    {
      var height = blocks * (blockRows + 1);

      return new FakeSheet("Data", height, 2, (column, row) =>
      {
        var withinBlock = row % (blockRows + 1);

        if (withinBlock == blockRows)
          return Cell.Blank;

        return column == 0 ? Cell.Of($"block {row / (blockRows + 1)}") : Cell.Of((decimal)row);
      });
    }

    private static ISheetCells Eagerly(FakeSheet sheet)
    {
      var cells = new Cell[sheet.RowCount, sheet.ColumnCount];

      for (var row = 0; row < sheet.RowCount; row++)
        for (var column = 0; column < sheet.ColumnCount; column++)
          cells[row, column] = sheet.At(column, row);

      return SheetGrid.Of(cells);
    }

    private static IProjectionDefinition<ISheetCells, IReadOnlyList<IReadOnlyList<decimal>>> BlockTotals()
      => VerticalRepeat(
        Sized(RowsWhileAnyValue()).Of(Range(block => (IReadOnlyList<decimal>)block.Rows.Select(row => row[1].Decimal()).ToList())),
        separatedBy: BlankRows());

    [Fact]
    public void AStreamedSheetReadsWhatTheEagerSheetReads()
    {
      var sheet = Blocks(blocks: 20, blockRows: 3);
      var expected = BlockTotals().Map(Eagerly(sheet));

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions());
      {
        var streamed = BlockTotals().Map(book.Sheet("Data"));

        Assert.Equal(expected.Select(block => block.Sum()), streamed.Select(block => block.Sum()));
        Assert.Equal(20, streamed.Count);
      }
    }

    [Fact]
    public void ARepeatHoldsOneOccurrenceAndItsGapAndNoMore()
    {
      // A hundred blocks of three rows: a forward pass holds one block, the gap after it and the
      // row in hand — never the sheet. What the declaration costs is readable off the sheet.
      var sheet = Blocks(blocks: 100, blockRows: 3);

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions());
      {
        Assert.Equal(100, BlockTotals().Map(book.Sheet("Data")).Count);

        var statistics = book.Statistics("Data")!.Value;
        Assert.True(statistics.PeakRetained <= 6, $"peak retained {statistics.PeakRetained} of {sheet.RowCount}");
        Assert.Equal(sheet.RowCount, statistics.RowsRead);
      }
    }

    [Fact]
    public void ACapExceededIsAFaultNamingTheHolder()
    {
      // A block lambda reads its extent at random, so it holds every row it is offered: fifty
      // valued rows, no blank to end the block. A cap smaller than that is a fault naming it, and
      // the Optional around it does not absorb a fault into an absent section.
      var sheet = new FakeSheet("Data", 50, 2);

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions { BufferRows = 5 });
      {
        var failure = Assert.Throws<ProjectionException>(() => Range(b => b.Height).Optional().Map(book.Sheet("Data")));

        Assert.True(failure.IsFault);
        Assert.Contains("Range is holding", failure.Message);
        Assert.Contains("more than the 5 the source allows", failure.Message);
      }
    }

    [Fact]
    public void ALeafUnderTheCapStreamsAHundredRowsThroughFive()
    {
      var sheet = Blocks(blocks: 100, blockRows: 3);

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions { BufferRows = 6 });
        Assert.Equal(100, BlockTotals().Map(book.Sheet("Data")).Count);
    }

    [Fact]
    public void APointReadAfterItsRowLeftTheBufferSaysSo()
    {
      // The address escapes its leaf and is read in the combiner, after a repeat has walked far
      // past its row: a forward pass cannot go back, and the failure says where the read belongs.
      var sheet = Blocks(blocks: 30, blockRows: 3);

      var late = VerticalFlow(v =>
      {
        var first = v.Next(Point());
        var rest = v.Next(Down(0).Of(BlockTotals()));

        return v.Build(read => $"{read.Of(first).AsText()}:{read.Of(rest).Count}");
      });

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions());
      {
        var failure = Assert.Throws<ProjectionException>(() => late.Map(book.Sheet("Data")));

        Assert.Contains("has left the buffer", failure.Message);
        Assert.Contains("read A1 inside the projection", failure.Message);
        Assert.Equal("A1", failure.Location.A1);
      }
    }

    [Fact]
    public void ReadingTheSamePointInsideTheLeafIsFine()
    {
      var sheet = Blocks(blocks: 30, blockRows: 3);

      var inside = VerticalFlow(v =>
      {
        var first = v.Next(Text());
        var rest = v.Next(BlockTotals());

        return v.Build(read => $"{read.Of(first)}:{read.Of(rest).Count}");
      });

      using var book = Workbook.Over(new FakeRowSource(sheet), new WorkbookOptions());
        Assert.Equal("block 0:30", inside.Map(book.Sheet("Data")));
    }

    [Fact]
    public void MapWorkbookReadsWhatTheEagerDoorReads()
    {
      var report = VerticalFlow(v =>
      {
        var title = v.Next(Text());
        var rows = v.Next(AfterBlankRows().Of(Table()));

        return v.Build(read => $"{read.Of(title)}:{read.Of(rows).Count}");
      });

      var path = System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "simple-report.xlsx");
      var eager = report.Map(Eager("simple-report.xlsx", "Report"));

        Assert.Equal(eager, report.MapWorkbook(path, "Report"));
    }
  }
}
