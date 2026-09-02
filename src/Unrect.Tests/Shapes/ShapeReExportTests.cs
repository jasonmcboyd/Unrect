using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The single-import claim: <c>using static Unrect.Shapes.Shape;</c> is all a declaration needs.
  /// Every re-export here forwards to a strategy factory, and each test proves the forwarding by
  /// behaviour rather than by reference — a re-export wired to the wrong strategy would compile.
  /// <para>
  /// The forwarding tests name the strategy factories they compare against, so they import
  /// <c>Unrect.Strategies</c>. The spelling tests below them do not use a single member of it — that
  /// is where the single-import claim is actually made, and where it would break.
  /// </para>
  /// </summary>
  public class ShapeReExportTests
  {
    private sealed record Line(string Client, DateTime When, decimal Amount);

    // 3 columns by 2 rows: 1 0 3 / 2 0 4 — a blank middle column, so column-wise and row-wise
    // discovery give different answers and a mis-wired re-export cannot hide.
    private static ISpace Patchy() => Grid(new[,] { { 1, 0, 3 }, { 2, 0, 4 } });

    private static ISpace Block() => Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

    /// <summary>The extent a strategy resolves to on the patchy grid, as "WxH".</summary>
    private static string Measure(IAreaStrategy area)
    {
      var size = area.GetArea(Patchy()).Size;

      return $"{size.Width}x{size.Height}";
    }

    // --- Extents ------------------------------------------------------------------------------------

    [Fact]
    public void TheExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(Measure(AreaStrategies.MaxArea()), Measure(WholeExtent()));
      Assert.Equal(Measure(AreaStrategies.MinArea()), Measure(NoExtent()));
      Assert.Equal(Measure(AreaStrategies.ExplicitArea(2, 1)), Measure(Extent(2, 1)));
    }

    [Fact]
    public void TheRowExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.RowsWhileAnyValue().ToAreaStrategy()),
        Measure(RowsWhileAnyValue()));

      Assert.Equal(
        Measure(SizeStrategies.RowsWhileAny(v => v.TryGetInt() == 1).ToAreaStrategy()),
        Measure(RowsWhileAny(v => v.TryGetInt() == 1)));
    }

    [Fact]
    public void TheColumnExtentReExportsForwardToTheirStrategies()
    {
      Assert.Equal(
        Measure(SizeStrategies.ColumnsWhileAnyValue().ToAreaStrategy()),
        Measure(ColumnsWhileAnyValue()));

      Assert.Equal(
        Measure(SizeStrategies.ColumnsWhileAny(v => v.TryGetInt() == 1).ToAreaStrategy()),
        Measure(ColumnsWhileAny(v => v.TryGetInt() == 1)));
    }

    [Fact]
    public void TheRowAndColumnExtentsDisagreeOnThisGrid()
    {
      // The guard on the two tests above: if both re-exports were wired to the same strategy they
      // would still pass, so pin that the two axes genuinely see different things here.
      Assert.Equal("3x2", Measure(RowsWhileAnyValue()));
      Assert.Equal("1x2", Measure(ColumnsWhileAnyValue()));
    }

    // --- Axis selectors -----------------------------------------------------------------------------

    [Fact]
    public void TheSelectorReExportsForwardToTheirStrategies()
    {
      Assert.Equal(RowStrategies.TakeRows(1).SelectRows(Block()), TakeRows(1).SelectRows(Block()));
      Assert.Equal(ColumnStrategies.TakeColumns(2).SelectColumns(Block()), TakeColumns(2).SelectColumns(Block()));
      Assert.Equal(RowStrategies.AllRows().SelectRows(Block()), AllRows().SelectRows(Block()));
      Assert.Equal(ColumnStrategies.AllColumns().SelectColumns(Block()), AllColumns().SelectColumns(Block()));
    }

    [Fact]
    public void AllRowsAndAllColumnsSeeTheWholeExtent()
    {
      Assert.Equal(2, AllRows().SelectRows(Block()));
      Assert.Equal(3, AllColumns().SelectColumns(Block()));
    }

    // --- The spellings the re-exports exist for --------------------------------------------------------

    [Fact]
    public void AFullWidthRowIsSpelledWithAllColumns()
    {
      // The spelling that replaced an opaque (space, column) => true at the call site: a leaf
      // overload that already takes a column strategy, handed the one that means "all of them".
      Assert.Equal(3, Row(AllColumns(), s => s.Count).Map(Patchy()));

      // ...where the discovered default would have stopped at the blank column.
      Assert.Equal(1, Row(s => s.Count).Map(Patchy()));
    }

    [Fact]
    public void AFullHeightColumnIsSpelledWithAllRows()
    {
      var space = Grid(new[,] { { 1 }, { 0 }, { 3 } });

      Assert.Equal(3, Column(AllRows(), s => s.Count).Map(space));
      Assert.Equal(1, Column(s => s.Count).Map(space));
    }

    // --- The lifts ------------------------------------------------------------------------------------

    [Fact]
    public void TheLiftReExportsForwardToTheirStrategies()
    {
      var rows = Grid(new[,] { { 1 }, { 2 }, { 3 } });
      var columns = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(
        OffsetStrategies.To(RowLandmarks.RowWhere((s, r) => s[0, r].GetInt() == 2)).GetOffset(rows).Size.Height,
        To(RowWhere((s, r) => s[0, r].GetInt() == 2)).GetOffset(rows).Size.Height);

      Assert.Equal(
        OffsetStrategies.Past(RowLandmarks.RowWhere((s, r) => s[0, r].GetInt() == 2)).GetOffset(rows).Size.Height,
        Past(RowWhere((s, r) => s[0, r].GetInt() == 2)).GetOffset(rows).Size.Height);

      Assert.Equal(
        OffsetStrategies.To(ColumnLandmarks.ColumnWhere((s, c) => s[c, 0].GetInt() == 2)).GetOffset(columns).Size.Width,
        To(ColumnWhere((s, c) => s[c, 0].GetInt() == 2)).GetOffset(columns).Size.Width);

      Assert.Equal(
        OffsetStrategies.Past(ColumnLandmarks.ColumnWhere((s, c) => s[c, 0].GetInt() == 2)).GetOffset(columns).Size.Width,
        Past(ColumnWhere((s, c) => s[c, 0].GetInt() == 2)).GetOffset(columns).Size.Width);
    }

    [Fact]
    public void ACaptionedSectionIsDeclarableFromTheOneImport()
    {
      // Where the single-import claim is actually made: Caption, Under, To, Past and RowContaining,
      // with no member of Unrect.Strategies anywhere in the declaration.
      var space = Mixed(new object?[,]
      {
        { "junk" },
        { "Detail" },
        { "a" },
        { "b" },
      });

      var section = Range(b => b.Height).Under(Caption("Detail"));
      var anchored = Cell(c => c.GetString()).After(Past(RowContaining("Detail")));

      Assert.Equal(2, section.Map(space));
      Assert.Equal("a", anchored.Map(space));
      Assert.Equal("Detail", Cell(c => c.GetString()).After(To(RowContaining("Detail"))).Map(space));
    }

    [Fact]
    public void ATypedTableAndALabelledBlockAreDeclarableFromTheOneImport()
    {
      // The phase C vocabulary, declared with nothing but `using static Unrect.Shapes.Shape`:
      // the typed leaves, TableRows<T>, its binding lambda, Fields and Field.
      var card = Mixed(new object?[,]
      {
        { "EIN:", "12-3456789", null },
        { null, null, null },
        { "Client", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 3, 4), 10m },
      });

      var report = VerticalFlow(v => new
      {
        Entity = v.Next(Fields(Field("EIN"))),
        Lines = v.Next(TableRows<Line>(bind => bind.Column(t => t.When, "Transaction Date"))),
      }).Map(card);

      Assert.Equal("12-3456789", report.Entity["EIN"].GetString());
      Assert.Equal(new DateTime(2026, 3, 4), report.Lines[0].When);
      Assert.Equal(10m, report.Lines[0].Amount);

      // ...and the typed leaves, which are the other half of the phase's vocabulary.
      Assert.Equal("Acme", Text().Down(3).Map(card));
      Assert.Equal(10m, Decimal().Down(3).Right(2).Map(card));
    }

    [Fact]
    public void AReExportedExtentResolvesInsideSized()
    {
      // The single-import claim where it is most load-bearing: a modifier taking an IAreaStrategy,
      // handed a re-export, with no strategies import in scope at the call site.
      var shape = Range(b => $"{b.Width}x{b.Height}").Sized(ColumnsWhileAnyValue());

      Assert.Equal("1x2", shape.Map(Patchy()));
      Assert.Equal("3x2", Range(b => $"{b.Width}x{b.Height}").Sized(RowsWhileAnyValue()).Map(Patchy()));
      Assert.Equal("2x1", Range(b => $"{b.Width}x{b.Height}").Sized(Extent(2, 1)).Map(Patchy()));
    }
  }
}
