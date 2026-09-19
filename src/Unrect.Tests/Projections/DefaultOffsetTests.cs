using System;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Where a projection starts when it does not say: past the blank spans in front of it.
  /// <para>
  /// One rule. A child of a flow or a repeat steps over blank spans along its container's axis —
  /// blank rows down a vertical one, blank columns across a horizontal one; the root and an
  /// overlay's children, which have no such axis, step along the one the run reads its source in.
  /// Nothing moves across. A projection that says where it starts — an anchor, a distance, an
  /// offset — is taken at its word and steps over nothing.
  /// </para>
  /// </summary>
  public class DefaultOffsetTests
  {
    public sealed record Summary(string Investor, decimal Commitment);

    public sealed record Transaction(DateTime Date, decimal Amount);

    /// <summary>
    /// A report as reports are laid out: a header card, a summary table, then a block per investor —
    /// a name over a table — with gaps of no particular height between every one of them and a few
    /// blank rows trailing.
    /// </summary>
    private static ISheetCells Report()
      => SheetGrid.Of(new object?[,]
      {
        { null, null },
        { "Quarterly Report", null },
        { new DateTime(2026, 6, 30), null },
        { "R-17", null },
        { null, null },
        { null, null },
        { "Investor", "Commitment" },
        { "Acme", 100m },
        { "Bolt", 250m },
        { null, null },
        { "Acme", null },
        { "Date", "Amount" },
        { new DateTime(2026, 1, 5), 10m },
        { new DateTime(2026, 2, 5), 20m },
        { null, null },
        { null, null },
        { "Bolt", null },
        { "Date", "Amount" },
        { new DateTime(2026, 3, 5), 30m },
        { null, null },
        { null, null },
      });

    [Fact]
    public void AReportIsDeclaredWithoutAWordAboutItsGaps()
    {
      // The acceptance case: no AfterBlankRows on the repeat, no separatedBy inside it. Every
      // section — the card, the table, the repeat, each block of it — steps over whatever gap is in
      // front of it, and the run of blocks ends at the trailing blank rows because the next attempt
      // finds nothing there to be.
      var report = VerticalFlow(v => new
      {
        Header = v.Next(Column(c => new { Title = c[0].Text(), Date = c[1].Date(), Id = c[2].Text() })),
        Summary = v.Next(Table<Summary>()),
        Details = v.Next(
          VerticalRepeat(
            VerticalFlow(block => new { Investor = block.Next(Text()), Transactions = block.Next(Table<Transaction>()) }),
            atLeast: 1)),
      });

      var read = report.Map(Report());

      Assert.Equal("R-17", read.Header.Id);
      Assert.Equal(new[] { "Acme", "Bolt" }, read.Summary.Select(s => s.Investor));
      Assert.Equal(new[] { "Acme", "Bolt" }, read.Details.Select(d => d.Investor));
      Assert.Equal(new[] { 2, 1 }, read.Details.Select(d => d.Transactions.Count));
    }

    [Fact]
    public void AHorizontalFlowStepsOverBlankColumns()
    {
      // Two cards side by side with a gap column between them: the second starts on the gap, and
      // steps over it along the flow's own axis.
      var cards = SheetGrid.Of(new object?[,] { { "a", null, "c" }, { "b", null, "d" } });
      var card = VerticalFlow(v => $"({v.Next(Text())},{v.Next(Text())})");

      Assert.Equal("(a,b)(c,d)", HorizontalFlow(h => $"{h.Next(card)}{h.Next(card)}").Map(cards));
    }

    [Fact]
    public void AFlowIsAPatternAndAStripIsAnIndex()
    {
      // Two ways to address the same row, and they mean different things. A flow divides a region
      // into blocks with gaps of any size between them, and a block one cell wide is still a block:
      // the blank between two cells is no more significant than the blank between two tables. A
      // strip addresses by position, where a blank at position 1 is what is AT position 1. Which one
      // a declaration wants is whether the blank is a gap or a value.
      var row = SheetGrid.Of(new object?[,] { { "Acme", null, "NY", "x" } });

      Assert.Equal(
        "Acme|NY|x",
        HorizontalFlow(h => $"{h.Next(Text())}|{h.Next(Text().OrBlank())}|{h.Next(Text())}").Map(row));

      // The strip says how long it is: a discovered one ends at the first blank cell — the same gap,
      // read as the end of a block.
      Assert.Equal(
        "Acme||NY",
        Row(4, c => $"{c[0].Text()}|{c[1].TextOrBlank()}|{c[2].Text()}").Map(row));
    }

    [Fact]
    public void AnOverlaysChildrenStepAlongTheRunsAxisAndNeverAcrossARow()
    {
      // An overlay is how a sparse row IS read, so its children must not step across it: the first
      // leaf reads the blank in column A as the blank it is.
      var row = SheetGrid.Of(new object?[,] { { null, "x", 5m } });

      var read = Overlay(o => $"{o.Next(Text().OrBlank()) ?? "<blank>"}|{o.Next(Right(2).Of(Decimal()))}").Map(row);

      Assert.Equal("<blank>|5", read);
    }

    [Fact]
    public void AProjectionThatSaysWhereItStartsStepsOverNothing()
    {
      // A distance is counted from where the container put the child, gap included: the default
      // belongs to a projection that declared nothing, and is not added to one that did.
      var sheet = SheetGrid.Of(new object?[,] { { "top" }, { null }, { "x" }, { "y" } });

      Assert.Equal(
        "top|x",
        VerticalFlow(v => $"{v.Next(Text())}|{v.Next(Down(1).Of(Text()))}").Map(sheet));
    }

    [Fact]
    public void NothingMovesAcross()
    {
      // A block indented under a block that is not: the flow starts at the left edge of what it was
      // handed whatever its first row looks like, so the second block's first column is still there.
      var sheet = SheetGrid.Of(new object?[,]
      {
        { null, "Indented" },
        { null, null },
        { "Code", "Amount" },
        { "A-1", 10m },
      });

      var read = VerticalFlow(v => new
      {
        Title = v.Next(Right(1).Of(Text())),
        Rows = v.Next(Table(r => r["Code"].Text())),
      }).Map(sheet);

      Assert.Equal(new[] { "A-1" }, read.Rows);
    }
  }
}
