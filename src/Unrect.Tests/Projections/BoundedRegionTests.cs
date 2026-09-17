using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// What a strategy is handed when the region it is measuring has no settled bottom edge yet.
  /// <para>
  /// The two things such a thing can silently get wrong are <em>when</em> it forces and <em>what it
  /// hides</em>, and both used to belong to a decorator. What it hides has not moved and must not:
  /// the region reads through the same space and shows the same capabilities the decorator did, and
  /// those figures are the decorator's own, measured at 3c59cde.
  /// </para>
  /// <para>
  /// <em>When</em> it forces has moved, once and deliberately. A region answers a width without
  /// answering a height, which a space could not do — <c>Area</c> is one struct and has no answering
  /// half — so a strategy that only ever needed a width stopped settling the boundary. The pair at
  /// the top of this class is where that shows, and it carries both the old figure and the new one
  /// so the change reads as a decision rather than as drift.
  /// </para>
  /// </summary>
  public class BoundedRegionTests
  {
    /// <summary>
    /// A hundred rows of values over three blank ones, with "Marker" in the first column of row 5 —
    /// the sheet the offset strategies below go looking through. The bound is a hundred rows, and
    /// settling it costs 101: the blank row that ends the scan has to be read to end it.
    /// </summary>
    private static ISheetCells MarkedSheet()
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
    /// The two spellings of "start somewhere a strategy has to look for", with what each of them
    /// costs — which is the point of the pair rather than a figure they happen to share.
    /// <para>
    /// <b>The law: a width-only rule never settles a boundary; a rule that asks how TALL the region
    /// is still does.</b> <c>SkipBlankRows</c> walks rows asking each one whether every cell of it
    /// is blank, so all it needs across is a width — and a width is free on a region still being
    /// discovered. <c>RowContaining</c> loops to the region's height, which is the forcing question
    /// by definition. Two strategies that look like the same kind of thing and are not.
    /// </para>
    /// <para>
    /// <b>Both numbers, so the improvement is on record.</b> When the offset strategies read
    /// <c>Area.Width</c> — one struct, no answering half — asking for a width asked for a height
    /// too, and <em>every</em> offset settled the boundary: both cases cost 101 rows and reached 100
    /// back. Reading <c>Width</c> instead moved the <c>offsetBy</c> case to 1 and 0. The
    /// <c>below</c> case is unchanged at 101 and 100, deliberately: it is the contrast, and it is
    /// what stops "nothing forces any more" being mistaken for the law.
    /// </para>
    /// </summary>
    private static readonly (string Offset, int RowsTouched, int BackwardReach)[] Offsets =
    {
      ("below", RowsToExhaustion, 100),
      ("offsetBy", 1, 0),
    };

    /// <summary>What each offset costs the child, in rows of the sheet read before it runs.</summary>
    public static TheoryData<string, int> RowsTouchedByOffset => Table(each => each.RowsTouched);

    /// <summary>How far back each offset's reading ever reached — the order, which no count shows.</summary>
    public static TheoryData<string, int> BackwardReachByOffset => Table(each => each.BackwardReach);

    /// <summary>One table, projected: a case cannot appear in one law and be forgotten in the other.</summary>
    private static TheoryData<string, int> Table(Func<(string Offset, int RowsTouched, int BackwardReach), int> select)
    {
      var cases = new TheoryData<string, int>();

      foreach (var each in Offsets)
        cases.Add(each.Offset, select(each));

      return cases;
    }

    /// <summary>
    /// <paramref name="child"/> placed by <paramref name="offset"/>, inside a flow whose own extent
    /// is discovered rather than measured — which is what puts the bounded region at the strategy's
    /// door.
    /// </summary>
    private static IProjection<ISheetCells, int> InsideADiscoveredBound(string offset, IProjection<ISheetCells, int> child)
    {
      var placed = offset switch
      {
        "below" => Below(RowContaining("Marker")).Of(child),
        "offsetBy" => OffsetBy(OffsetStrategies.SkipBlankRows()).Of(child),
        _ => throw new ArgumentOutOfRangeException(nameof(offset), offset, "No such offset.")
      };

      return Sized(RowsWhileAnyValue()).Of(VerticalFlow(v =>
      {
        var placed2 = v.Next(placed);

        return v.Build(read => read.Of(placed2));
      }));
    }

    [Theory]
    [MemberData(nameof(RowsTouchedByOffset))]
    public void AnOffsetStrategySettlesTheBoundaryOnlyIfItAsksHowTallTheRegionIs(string offset, int rowsTouched)
    {
      // Measured from inside the child, because that is the only moment the difference exists: by
      // the time Apply returns the engine has consumed the declared area in full either way.
      //
      // One row for the width-only rule — the row it actually looked at — against a hundred and one
      // for the rule that asks the height. See the theory data above for what each number was
      // before the width read was separated from the height read.
      var counter = new CountingSpace(MarkedSheet());
      var observed = -1;

      InsideADiscoveredBound(offset, Range(1, 1, block =>
      {
        observed = counter.RowsTouched;

        return 0;
      })).Apply(counter);

      Assert.Equal(rowsTouched, observed);

      // And the declaration still consumed everything it declared, whichever way the offset was
      // spelled — which is the half that must NOT have moved. Laziness changes when rows are read,
      // never how much of the sheet a declared area takes.
      Assert.Equal(RowsToExhaustion, counter.RowsTouched);
    }

    [Theory]
    [MemberData(nameof(BackwardReachByOffset))]
    public void AndReachesBackNoFurtherThanItSettled(string offset, int backwardReach)
    {
      // The other half, and the one no count can make: a windowed reader is only as cheap as the
      // walk over it is monotone, so what matters is the ORDER. A rule that settles the boundary
      // runs the scan to row 100 and then starts its own search again from row 0 — a hundred rows
      // behind, and a hundred rows of window it may have to re-read. A width-only rule never leaves
      // row 0, so it never reaches back at all.
      //
      // The high-water mark is 100 either way: the engine consumes the declared area before Apply
      // returns, so the sheet is read to the boundary regardless. Only the ORDER differs, which is
      // exactly the thing this instrument exists to see and the counters cannot.
      var watermark = new WatermarkSpace(MarkedSheet());

      InsideADiscoveredBound(offset, Range(1, 1, block => 0)).Apply(watermark);

      Assert.Equal(100, watermark.HighWaterMark);
      Assert.Equal(backwardReach, watermark.BackwardReach);
    }

    // --- The canonical four, through the view ----------------------------------------------------------

    /// <summary>
    /// A ten-row grid whose last three rows are empty, under a rule that stops at the first of them
    /// — so the region is seven rows tall and settling it costs eight, the extra one being the row
    /// that ends the scan. Handed back as the view a strategy would be given, over a counter.
    /// </summary>
    private static (Plane<ISpace> View, CountingSpace Counter) Discovering()
    {
      var values = new int[10, 2];

      for (var row = 0; row < 7; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      var counter = new CountingSpace(Grid(values));
      var scan = new StopAtBlankScan();

      return (counter.Region().Bounded(new Bound(counter.Region(), scan, exception => throw exception), 2), counter);
    }

    /// <summary>The rule "rows while any cell has a value", spelled out so the view has one to obey.</summary>
    private sealed class StopAtBlankScan : IAreaScan
    {
      public int Width => 2;

      public bool IncludesRow(Plane<ISpace> space, int row) => !space[0, row].IsBlank || !space[1, row].IsBlank;
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

      Assert.False(view[0, row].IsBlank);
      Assert.False(view[0, row].IsText);
      Assert.Equal($"{row + 1}", view[0, row].AsText());

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

      Assert.Throws<OutOfBoundsException>(() => { _ = view[0, 7]; });
    }

    [Fact]
    public void AskingTheViewHowBigItIsSettlesTheBoundaryAsItAlwaysHas()
    {
      // The forcing question, and the one every strategy in the tree asks: reading the region's
      // extent reads the scan to exhaustion. That is not a regression to be fixed — it is the
      // behaviour the decorator had, and it is why Width and HasRow exist beside Area.
      var (view, counter) = Discovering();

      Assert.Equal(7, view.Area.Height);
      Assert.Equal(2, view.Area.Width);
      Assert.Equal(8, counter.RowsTouched);
    }

    // --- The definitional fold, over a region whose bottom edge is not settled ---------------------
    //
    // New behaviour, and the reason it is worth a section. Scans.Fold used to ask the region how
    // TALL it was before each step, which on a region still being discovered settles the boundary
    // before the fold has read a row — so every eager reading of every incremental strategy forced
    // whatever it was folded over, and laziness stopped at the strategy's door. It now asks whether
    // there IS a row, one row at a time, so a fold that stops early has read only that far.
    //
    // The claim is entirely about WHEN, so the instrument is a bound that counts the questions it is
    // asked rather than a space that counts cells.

    /// <summary>
    /// A bottom edge that admits a fixed number of rows, counting how far it was asked to look and
    /// how many times it was settled — the two questions a fold could get wrong.
    /// </summary>
    private sealed class CountingBound : IBound
    {
      private readonly int _height;

      internal CountingBound(int height) => _height = height;

      /// <summary>The furthest row anything asked about, or -1 where nothing did.</summary>
      internal int Reached { get; private set; } = -1;

      /// <summary>How many times the whole extent was settled.</summary>
      internal int Forced { get; private set; }

      public bool HasRow(int row)
      {
        if (row > Reached)
          Reached = row;

        return row < _height;
      }

      public int Force()
      {
        Forced++;
        Reached = _height;

        return _height;
      }

      public IBound Shift(int rows) => throw new NotSupportedException("The folds here never shift.");
    }

    /// <summary>A ten-row grid of values under a bottom edge that will admit eight of them.</summary>
    private static (Plane<ISpace> Region, CountingBound Bound) Discovered(int height = 8)
    {
      var bound = new CountingBound(height);

      return (CoordinateGrid(2, 10).Region().Bounded(bound, 2), bound);
    }

    /// <summary>A scan that stops after a fixed number of rows, whatever it is folded over.</summary>
    private sealed class StopsAfter : IAreaScan
    {
      private readonly int _rows;

      internal StopsAfter(int rows) => _rows = rows;

      public int Width => 2;

      public bool IncludesRow(Plane<ISpace> space, int row) => row < _rows;
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    public void AFoldThatStopsEarlyAdvancesTheBoundaryOnlyThatFar(int stopsAfter, int reached)
    {
      // The rule, and its whole point: a fold reads no further than the fold itself reaches. The
      // scan gives up after n rows, so the boundary has been asked about row n and no other — and
      // it has not been settled at all, which is the half that used to be impossible.
      var (region, bound) = Discovered();

      Assert.Equal(stopsAfter, Scans.Fold(new StopsAfter(stopsAfter), region));

      Assert.Equal(reached, bound.Reached);
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void AFoldThatRunsPastTheBoundaryStopsAtItWithoutSettlingIt()
    {
      // The other exit. A rule that never says no is ended by the region rather than by itself, and
      // even then the question asked is "is there a row here" — the boundary answers no at row 8 and
      // is never asked how tall it is.
      var (region, bound) = Discovered();

      Assert.Equal(8, Scans.Fold(new StopsAfter(int.MaxValue), region));

      Assert.Equal(8, bound.Reached);
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void TheSizeAndAreaFoldsCarryTheSameLaziness()
    {
      // Both lifts are the row fold with a width beside it, so neither may settle anything the row
      // fold would not. Stated separately because each is a one-line delegation that could stop
      // being one.
      var (forSize, sizeBound) = Discovered();
      var (forArea, areaBound) = Discovered();

      var size = Scans.FoldSize(new StopsAfter(3), forSize);
      var area = Scans.FoldArea(new StopsAfter(3), forArea);

      Assert.Equal(2, size.Width);
      Assert.Equal(3, size.Height);
      Assert.Equal(3, sizeBound.Reached);
      Assert.Equal(0, sizeBound.Forced);

      Assert.Equal(size.Width, area.Width);
      Assert.Equal(size.Height, area.Height);
      Assert.Equal(3, areaBound.Reached);
      Assert.Equal(0, areaBound.Forced);
    }

    [Fact]
    public void AFoldOverAMeasuredRegionIsUnchanged()
    {
      // The control. Nothing about the fold is conditional on the region being discovered, so over
      // an ordinary measured one it must count exactly what it always counted — otherwise every
      // eager reading in the library would have moved.
      var measured = CoordinateGrid(2, 10).Region();

      Assert.Equal(3, Scans.Fold(new StopsAfter(3), measured));
      Assert.Equal(10, Scans.Fold(new StopsAfter(int.MaxValue), measured));
      Assert.Equal(new Size(2, 3), Scans.FoldSize(new StopsAfter(3), measured));
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
      var declaration = ProjectionBuilders<ISpreadsheetSpace>.Down(7)
        .Sized(RowsWhileAnyValue())
        .Of(ProjectionBuilders<ISpreadsheetSpace>.VerticalFlow(v =>
        {
          var projectionBuilders = v.Next(
            ProjectionBuilders<ISpreadsheetSpace>.On(SpreadsheetProjections.RowWithFormula("LOG10"))
              .Of(ProjectionBuilders<ISpreadsheetSpace>.Range(1, 1, block => block.Location.A1)));

          return v.Build(read2 => read2.Of(projectionBuilders));
        }));

      var read = declaration.Map(FormulaSheet());

      // Non-vacuous: the section landed on row 9 rather than on the region's own first row, so the
      // matcher really did read formulas through the bounded region. A region that named anything
      // but the sheet itself would not have got this far — the matcher's cast would have failed as
      // an InvalidCastException, which no tolerance can absorb.
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
      var formula = ProjectionBuilders<ISpreadsheetSpace>.Sized(RowsWhileAnyValue())
        .Of(ProjectionBuilders<ISpreadsheetSpace>.Overlay(o =>
        {
          var projectionBuilders = o.Next(
            ProjectionBuilders<ISpreadsheetSpace>.Down(1)
              .Right(3)
              .Of(SpreadsheetProjections.Formula<ISpreadsheetSpace>()));

          return o.Build(read2 => read2.Of(projectionBuilders));
        }));

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
