using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The one place a region is turned back into a space: the adapter the engine puts a
  /// still-being-discovered extent inside before handing it to a strategy, because the strategy
  /// layer still takes spaces rather than regions.
  /// <para>
  /// It is temporary scaffolding, and scaffolding is exactly what needs pinning: it stands where a
  /// decorator used to stand, and the two things a decorator can silently get wrong are <em>when</em>
  /// it forces and <em>what it hides</em>. A strategy handed one of these forces it, and always has
  /// — strategies read <c>Area</c> freely — so the claim is that it forces at the same moment, reads
  /// through the same space, and shows the same capabilities as the decorator it replaced.
  /// </para>
  /// </summary>
  public class BoundedViewTests
  {
    /// <summary>
    /// A hundred rows of values over three blank ones, with "Marker" in the first column of row 5 —
    /// the sheet the offset strategies below go looking through. The bound is a hundred rows, and
    /// settling it costs 101: the blank row that ends the scan has to be read to end it.
    /// </summary>
    private static ICellValues MarkedSheet()
    {
      var values = new object?[103, 2];

      for (var row = 0; row < 100; row++)
      {
        values[row, 0] = row == MarkerRow ? "Marker" : $"row {row}";
        values[row, 1] = row + 1;
      }

      return Mixed(values);
    }

    private const int MarkerRow = 5;
    private const int RowsToExhaustion = 101;

    /// <summary>
    /// The two spellings of "start somewhere a strategy has to look for", as theory data: an anchor
    /// on a landmark, and the strategy door itself.
    /// </summary>
    public static TheoryData<string> AreaReadingOffsets => new TheoryData<string> { "below", "offsetBy" };

    /// <summary>
    /// <paramref name="child"/> placed by <paramref name="offset"/>, inside a flow whose own extent
    /// is discovered rather than measured — which is what puts the bounded region at the strategy's
    /// door.
    /// </summary>
    private static IProjection<int> InsideADiscoveredBound(string offset, IProjection<int> child)
    {
      var placed = offset switch
      {
        "below" => Below(RowContaining("Marker")).Of(child),
        "offsetBy" => OffsetBy(OffsetStrategies.SkipBlankRows()).Of(child),
        _ => throw new ArgumentOutOfRangeException(nameof(offset), offset, "No such offset.")
      };

      return Sized(RowsWhileAnyValue()).Of(VerticalFlow(v => v.Next(placed)));
    }

    [Theory]
    [MemberData(nameof(AreaReadingOffsets))]
    public void AnAreaReadingOffsetStrategyCostsExactlyWhatItCostBefore(string offset)
    {
      // The preservation claim, measured. Both of these strategies read the space's own extent, so
      // both settle the bound where they stand — and settling it reads the scan to exhaustion, 101
      // rows for a bound of 100. The figures are the ones the decorator produced at 3c59cde,
      // measured in a worktree at that commit before this was written; a view that forced later
      // would show fewer, and one that forced through a differently-cut space would show more.
      var counter = new CountingSpace(MarkedSheet());
      var observed = -1;

      InsideADiscoveredBound(offset, Range(1, 1, _ =>
      {
        observed = counter.RowsTouched;

        return 0;
      })).Apply(counter);

      Assert.Equal(RowsToExhaustion, observed);
      Assert.Equal(RowsToExhaustion, counter.RowsTouched);
    }

    [Theory]
    [MemberData(nameof(AreaReadingOffsets))]
    public void AndReachesNoFurtherBackThanItDidBefore(string offset)
    {
      // The other half, and the one no count can make: a windowed reader is only as cheap as the
      // walk over it is monotone, so what matters is the ORDER. The scan runs to row 100 to settle
      // the bound and the strategy then starts again from row 0, which is a backward reach of
      // exactly 100 — the decorator's own figure at 3c59cde. A view cut to the settled height would
      // tell the streaming store a different band was open and move this without moving an answer.
      var watermark = new WatermarkSpace(MarkedSheet());

      InsideADiscoveredBound(offset, Range(1, 1, _ => 0)).Apply(watermark);

      Assert.Equal(100, watermark.HighWaterMark);
      Assert.Equal(100, watermark.BackwardReach);
    }

    // --- The canonical four, through the view ----------------------------------------------------------

    /// <summary>
    /// A ten-row grid whose last three rows are empty, under a rule that stops at the first of them
    /// — so the region is seven rows tall and settling it costs eight, the extra one being the row
    /// that ends the scan. Handed back as the view a strategy would be given, over a counter.
    /// </summary>
    private static (ICellValues View, CountingSpace Counter) Discovering()
    {
      var values = new int[10, 2];

      for (var row = 0; row < 7; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      var counter = new CountingSpace(Grid(values));
      var scan = new StopAtBlankScan();

      return (counter.Extent().Bounded(new Bound(counter, scan, exception => throw exception), 2).AsSpace(), counter);
    }

    /// <summary>The rule "rows while any cell has a value", spelled out so the view has one to obey.</summary>
    private sealed class StopAtBlankScan : IAreaScan
    {
      public int Width => 2;

      public bool IncludesRow(ICellValues space, int row) => !space.IsBlank(0, row) || !space.IsBlank(1, row);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(3, 4)]
    [InlineData(6, 7)]
    public void TheViewsCanonicalFourCostTheRowTheyName(int row, int rowsTouched)
    {
      // A strategy that only ever asks about cells must cost only the rows it asked about. The
      // decorator this replaced read through the same seam, so these are its numbers too — and a
      // view that answered any of the three by settling its own extent first would show eight here
      // for every row, which is the whole scan.
      var (view, counter) = Discovering();

      Assert.False(view.IsBlank(0, row));
      Assert.False(view.IsText(0, row));
      Assert.Equal($"{row + 1}", view.AsText(0, row));
      Assert.Equal(row + 1, view[0, row].GetInt());

      Assert.Equal(rowsTouched, counter.RowsTouched);
    }

    [Fact]
    public void TheViewRefusesARowPastTheBoundaryAsAnOverrun()
    {
      // Row 7 is a real row of the grid underneath and outside the region, so the answer is the
      // bounds condition a declaration recovers from — not a blank cell, which is what forwarding
      // to the space would have produced and is the far worse answer: a scan told "there is nothing
      // here" cannot tell that from "you are past the end".
      var (view, _) = Discovering();

      Assert.Throws<OutOfBoundsException>(() => { _ = view.IsBlank(0, 7); });
      Assert.Throws<OutOfBoundsException>(() => { _ = view.IsText(0, 7); });
      Assert.Throws<OutOfBoundsException>(() => { _ = view.AsText(0, 7); });
      Assert.Throws<OutOfBoundsException>(() => { _ = view[0, 7]; });
    }

    [Fact]
    public void AskingTheViewHowBigItIsSettlesTheBoundaryAsItAlwaysHas()
    {
      // The forcing question, and the one every strategy in the tree asks: the view's extent is the
      // region's, so reading it reads the scan to exhaustion. That is not a regression to be fixed
      // here — it is the behaviour the decorator had, and it is why the strategy signatures taking
      // regions directly is the next phase rather than this one.
      var (view, counter) = Discovering();

      Assert.Equal(7, view.Area.Height);
      Assert.Equal(2, view.Area.Width);
      Assert.Equal(8, counter.RowsTouched);
    }

    [Fact]
    public void ACapabilityIsStillFoundThroughARegionStillBeingDiscovered()
    {
      // The regression this guards is the silent kind. A capability is asked for by walking the
      // chart chain, and a wrapper that did not join it would answer "this file has no formulas" —
      // one bounded extent down, in the one voice a declaration cannot argue with, because absence
      // at a projection site is a fact about the cell.
      //
      // The strategy door is where it would happen: a formula matcher is handed the bounded space
      // itself, so the demand it makes is made of the view rather than of the sheet.
      // Rows 8 and 9 of the fixture — "Base" and "Scaled" — with the second carrying the
      // column-shifted group whose text mentions LOG10, and nothing below them until row 11. So the
      // discovered region is two rows tall and the matcher has to look inside it.
      var declaration = Down(7).Sized(RowsWhileAnyValue()).Of(
        VerticalFlow(Formulas, v => v.Next(On(RowWithFormula("LOG10")).Of(Range(1, 1, block => block.Location.A1)))));

      var read = declaration.Map(FormulaSheet());

      // Non-vacuous: the section landed on row 9 rather than on the region's own first row, so the
      // matcher really did read formulas through the bounded region. A view that had dropped out of
      // the chart chain would not have got this far — the demand would have failed as a
      // MissingCapabilityException, which no tolerance can absorb.
      Assert.Equal("A9", read);
    }

    [Fact]
    public void AndAProjectionInsideOneReadsFormulasThroughItsOwnRegion()
    {
      // The projection site's twin of the demand above, and the spelling the leaf itself uses: a
      // Formula() leaf reads the capability off the space its region names. Inside a discovered
      // bound that region is one the engine cut for real, so the capability is right there — but a
      // cut that had shed it, or a chart that had hidden it, would report every formula as absent
      // rather than fail.
      var formula = Sized(RowsWhileAnyValue()).Of(
        Overlay(Formulas, o => o.Next(Down(1).Right(3).Of(Formula()))));

      var read = formula.Map(FormulaSheet());

      // D2, the master of the shared group — a real expression, so "absent" would be a lie rather
      // than a shrug.
      Assert.NotNull(read);
      Assert.Contains("ROUND", read, StringComparison.Ordinal);
    }

    /// <summary>The formula fixture, through the door that carries formulas.</summary>
    private static ISpreadsheetSpace FormulaSheet()
      => SpreadsheetSpace.CreateWithFormulas(
        System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");
  }
}
