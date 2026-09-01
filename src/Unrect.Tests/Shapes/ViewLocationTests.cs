using System;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Views know where they sit, so a caller's own complaints about the data can cite a cell the way
  /// the framework's do. The framework's diagnostics stay structural; a rule like "this total does
  /// not add up" is the caller's, and now it can say where.
  /// <para>
  /// Every address is absolute — relative to the space <c>Map</c> was called with — and must stay
  /// so through padding, overlays, flows, and successive repeat items.
  /// </para>
  /// </summary>
  public class ViewLocationTests
  {
    // 4 columns by 3 rows of (row * 10 + column + 1): 1 2 3 4 / 11 12 13 14 / 21 22 23 24.
    private static ISpace CoordinateGrid(int width = 4, int height = 3)
    {
      var values = new int[height, width];

      for (var row = 0; row < height; row++)
        for (var column = 0; column < width; column++)
          values[row, column] = row * 10 + column + 1;

      return Grid(values);
    }

    // --- CellStrip -------------------------------------------------------------------------------

    [Fact]
    public void ARowStripKnowsWhereItStartsAndWhereEachCellIs()
    {
      var addresses = Row(3, r => new[] { r.Location.A1, r.AddressOf(0).A1, r.AddressOf(2).A1 })
        .Down(1)
        .Map(CoordinateGrid());

      Assert.Equal(new[] { "A2", "A2", "C2" }, addresses);
    }

    [Fact]
    public void AColumnStripIsAddressedDownItsOwnAxis()
    {
      var addresses = Column(3, c => new[] { c.Location.A1, c.AddressOf(0).A1, c.AddressOf(2).A1 })
        .Right(1)
        .Map(CoordinateGrid());

      Assert.Equal(new[] { "B1", "B1", "B3" }, addresses);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void AStripRefusesToAddressACellItDoesNotHave(int index)
    {
      var strip = Row(3, r => r).Map(CoordinateGrid());

      Assert.Throws<ArgumentOutOfRangeException>(() => strip.AddressOf(index));
    }

    // --- CellBlock -------------------------------------------------------------------------------------

    [Fact]
    public void ABlockKnowsWhereItStartsAndWhereEachCellIs()
    {
      var addresses = Range(b => new[] { b.Location.A1, b.AddressOf(0, 0).A1, b.AddressOf(2, 1).A1 })
        .Map(CoordinateGrid());

      Assert.Equal(new[] { "A1", "A1", "C2" }, addresses);
    }

    [Fact]
    public void ABlocksRowsAndColumnsCarryTheirOwnAddresses()
    {
      var (rowStart, columnStart) = Range(b => (b.Row(1).Location.A1, b.Column(2).Location.A1)).Map(CoordinateGrid());

      Assert.Equal("A2", rowStart);
      Assert.Equal("C1", columnStart);
    }

    [Fact]
    public void ABlockRefusesToAddressACellItDoesNotHave()
    {
      var block = Range(b => b).Map(CoordinateGrid());

      Assert.Throws<ArgumentOutOfRangeException>(() => block.AddressOf(4, 0));
      Assert.Throws<ArgumentOutOfRangeException>(() => block.AddressOf(0, 3));
      Assert.Throws<ArgumentOutOfRangeException>(() => block.AddressOf(-1, 0));
    }

    // --- Absolute through every kind of nesting -----------------------------------------------------------

    [Fact]
    public void AddressesAreAbsoluteThroughPadding()
    {
      // Padding shifts where the inner shape reads, so it must shift what the inner shape reports.
      var addresses = Range(b => new[] { b.Location.A1, b.AddressOf(1, 0).A1 }).Padded(1).Map(CoordinateGrid());

      Assert.Equal(new[] { "B2", "C2" }, addresses);
    }

    [Fact]
    public void AddressesAreAbsoluteThroughAFlow()
    {
      var band = Range(4, 1, b => b.Location.A1);

      Assert.Equal("A1|A2", VerticalFlow(v => $"{v.Next(band)}|{v.Next(band)}").Map(CoordinateGrid()));
    }

    [Fact]
    public void AddressesAreAbsoluteInsideOverlayChildren()
    {
      // Overlay children share an extent but sit in different places inside it.
      var corner = Range(1, 1, b => b.Location.A1);
      var inset = Range(1, 1, b => b.Location.A1).Down(1).Right(2);

      Assert.Equal("A1|C2", Overlay(o => $"{o.Next(corner)}|{o.Next(inset)}").Map(CoordinateGrid()));
    }

    [Fact]
    public void AddressesAreAbsoluteInSuccessiveRepeatItems()
    {
      var addresses = Repeat(Range(4, 1, b => b.Location.A1)).Map(CoordinateGrid());

      Assert.Equal(new[] { "A1", "A2", "A3" }, addresses);
    }

    [Fact]
    public void AddressesSurviveEveryLayerAtOnce()
    {
      // An overlay inside a flow, padded, a row down: the arithmetic has to compose.
      var band = Range(4, 1, b => b.Location.A1);
      var padded = Range(b => b.Location.A1).Padded(1, 0, 0, 0);
      var corner = Range(1, 1, b => b.Location.A1);

      var address = VerticalFlow(v =>
        $"{v.Next(band)}|{v.Next(Overlay(o => $"{o.Next(padded)}/{o.Next(corner)}"))}")
        .Map(CoordinateGrid());

      Assert.Equal("A1|B2/A2", address);
    }

    // --- TableView and TableRow -----------------------------------------------------------------------------

    private static ISpace Table() => Mixed(new object?[,]
    {
      { null, null },
      { "Name", "Amount" },
      { "Acme", 10 },
      { "Beta", 20 },
    });

    [Fact]
    public void ATableKnowsWhereItStartsHeaderIncluded()
    {
      // The blank row above is skipped by the table's default offset, so the table starts at A2.
      Assert.Equal("A2", Shape.Table(t => t.Location.A1).Map(Table()));
    }

    [Fact]
    public void ATableRowKnowsWhereItStarts()
    {
      Assert.Equal(new[] { "A3", "A4" }, TableRows(r => r.Location.A1).Map(Table()));
    }

    [Fact]
    public void ATableRowAddressesItsCellsByIndex()
    {
      Assert.Equal(new[] { "B3", "B4" }, TableRows(r => r.AddressOf(1).A1).Map(Table()));
    }

    [Fact]
    public void ATableRowAddressesItsCellsByColumnName()
    {
      // The point of the whole feature: "the Amount in row 2 is wrong" can name B4.
      Assert.Equal(new[] { "B3", "B4" }, TableRows(r => r.AddressOf("Amount").A1).Map(Table()));
      Assert.Equal(new[] { "B3", "B4" }, TableRows(r => r.AddressOf("  amount  ").A1).Map(Table()));
    }

    [Fact]
    public void AddressOfName_FollowsTheIndexersResolutionRules()
    {
      var unknown = Assert.Throws<ShapeException>(() => TableRows(r => r.AddressOf("Net").A1).Map(Table()));
      Assert.Contains("there is no column named 'Net'; available columns: 'Name', 'Amount'.", unknown.Message);

      var headerless = Assert.Throws<ShapeException>(() => TableRows(0, r => r.AddressOf("Name").A1).Map(Table()));
      Assert.Contains("the table was declared without a header row; use column indices.", headerless.Message);

      var ambiguous = Assert.Throws<ShapeException>(() =>
        TableRows(r => r.AddressOf("Amount").A1).Map(Mixed(new object?[,]
        {
          { "Amount", "Amount" },
          { 1, 2 },
        })));
      Assert.Contains("appears at indices 0 and 1; use the index.", ambiguous.Message);
    }

    [Fact]
    public void AddressOfIndex_FollowsTheIndexersRangeRules()
    {
      var failure = Assert.Throws<ShapeException>(() => TableRows(r => r.AddressOf(4).A1).Map(Table()));

      Assert.Contains("column index 4 is out of range; the table has 2 columns.", failure.Message);
    }

    [Fact]
    public void ATablesAddressesAreAbsoluteWhenItIsNested()
    {
      var caption = Range(2, 1, b => b.Location.A1);
      var amounts = TableRows(r => r.AddressOf("Amount").A1);

      var address = VerticalFlow(v => $"{v.Next(caption)}|{string.Join(",", v.Next(amounts))}").Map(Mixed(new object?[,]
      {
        { "ignored", "header" },
        { "Name", "Amount" },
        { "Acme", 10 },
      }));

      Assert.Equal("A1|B3", address);
    }
  }
}
