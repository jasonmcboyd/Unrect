using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;
using Unrect.Tests.Streaming;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjections;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The first place root coordinates are visible from outside: a region that does not start at its
  /// space's own corner, handed to the strategy calculus.
  /// <para>
  /// Everything the calculus is given is a <c>Plane&lt;ISpace&gt;</c>, and a plane is a locator
  /// rather than a wrapper — it names a rectangle of a space and does not hide the rest of it. So a
  /// point minted from a region carries the SPACE's coordinates, and anything that reaches past the
  /// region to the space (a capability, most of all) must be addressed in those coordinates and not
  /// in the region's. A translation dropped anywhere along that path reads the wrong cells and looks
  /// entirely plausible doing it, which is the failure mode this file exists for.
  /// </para>
  /// <para>
  /// <b>Most regions here are sliced by hand</b>, which is how they were written when a hand-cut
  /// region was the only one with a non-zero origin: through phase 5 the engine cut real subspace
  /// objects and handed down planes whose origin was (0, 0), because the streaming window's locus
  /// rode on the subspace's own extent. Placement is arithmetic now, so the hand-cut and the placed
  /// region answer alike, and <see cref="APlacedRegionsPointsCarryTheSheetsCoordinatesToo"/> — which
  /// was the tripwire for exactly this change — says so through the engine.
  /// </para>
  /// </summary>
  public class RootCoordinateTests
  {
    // --- A point in a predicate is an address in the sheet ------------------------------------------

    /// <summary>Every coordinate a landmark's predicate was shown, as <c>"column,row"</c>.</summary>
    private static (int? Found, IReadOnlyList<string> Seen) Watching(Plane<ISpace> region, string stopAt)
    {
      var seen = new List<string>();

      var found = RowLandmarks.RowWithCell(point =>
      {
        seen.Add($"{point.Column},{point.Row}");

        return point.AsText() == stopAt;
      }).FindRow(region);

      return (found, seen);
    }

    /// <summary>Both doors onto the same five-by-eight coordinate grid, whose cells say their own row and column.</summary>
    public static TheoryData<string> Doors => new TheoryData<string> { "grid", "windowed" };

    /// <summary>
    /// A grid whose every cell is <c>row * 10 + column + 1</c>, at each door — so a point's address
    /// and the cell it names can be checked against each other without a table of literals.
    /// </summary>
    private static ICellSpace Sheet(string door)
      => door == "grid"
        ? CoordinateGrid(5, 8)
        : Streamed(FakeSheet.Of("Data", Rows()));

    private static object?[][] Rows()
    {
      var rows = new object?[8][];

      for (var row = 0; row < 8; row++)
      {
        rows[row] = new object?[5];

        for (var column = 0; column < 5; column++)
          rows[row][column] = (row * 10) + column + 1;
      }

      return rows;
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void APointInAPredicateCarriesTheSheetsCoordinatesAndNotTheRegions(string door)
    {
      // The region starts at (2, 3) and is two by two, so its own corner is the sheet's (2, 3). A
      // predicate that was shown 0,0 would be looking at a locator that had forgotten where it was —
      // and every read through it would still be right, because the region reads the right cells.
      // Only the ADDRESS would be wrong, which is why this is worth a test of its own.
      var region = Plane<ISpace>.Of(Sheet(door)).Slice(new Offset(2, 3), new Area(2, 2));

      var (_, seen) = Watching(region, "never");

      Assert.Equal(new[] { "2,3", "3,3", "2,4", "3,4" }, seen);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void AndThePointStillReadsTheCellThatAddressNames(string door)
    {
      // The other half, and the one that makes the first half meaningful: root coordinates are only
      // worth carrying if they are the coordinates the reading uses. Cell (2, 3) of this grid says
      // 3 * 10 + 2 + 1 = 33, so the address and the content have to agree.
      var region = Plane<ISpace>.Of(Sheet(door)).Slice(new Offset(2, 3), new Area(2, 2));

      var corner = region[0, 0];

      Assert.Equal(2, corner.Column);
      Assert.Equal(3, corner.Row);
      Assert.Equal("33", corner.AsText());
      Assert.Equal(corner.AsText(), Sheet(door).AsTextAt(2, 3));
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void AndTheIndexTheLandmarkReportsIsStillTheRegionsOwn(string door)
    {
      // The distinction that has to survive both ways round. A POINT is an address in the sheet; an
      // INDEX is a position in the region, because that is what a placement composes with. A landmark
      // that had started answering in sheet coordinates would place every anchored section three rows
      // too low, and nothing about the reading would look wrong until the rows ran out.
      var region = Plane<ISpace>.Of(Sheet(door)).Slice(new Offset(2, 3), new Area(2, 3));

      // Row 4 of the sheet, which is row 1 of this region: its column 2 says 4 * 10 + 2 + 1 = 43.
      var (found, _) = Watching(region, "43");

      Assert.Equal(1, found);
    }

    [Fact]
    public void APlacedRegionsPointsCarryTheSheetsCoordinatesToo()
    {
      // The half the hand-cut regions above were the tripwire for, now landed. Placement is
      // arithmetic: the engine slices the plane it was handed instead of cutting a subspace object,
      // so a region placed three down and two right has origin (2, 3) in the sheet and every point
      // minted inside it says so. There is no second frame left in the system to translate between.
      var seen = new List<string>();

      var probe = Down(3).Right(2).Of(
        Range(2, 2, block =>
        {
          for (var row = 0; row < block.Height; row++)
            for (var column = 0; column < block.Width; column++)
              seen.Add($"{block.Space.Erased()[column, row].Column},{block.Space.Erased()[column, row].Row}");

          return 0;
        }));

      probe.Map(CoordinateGrid(5, 8));

      Assert.Equal(new[] { "2,3", "3,3", "2,4", "3,4" }, seen);
    }

    // --- A capability reaches past the region, so it is addressed in the space's coordinates --------

    /// <summary>The formula fixture, through the door that carries formulas.</summary>
    private static ISpreadsheetSpace FormulaSheet()
      => SpreadsheetSpace.CreateWithFormulas(
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "TestData", "formulas.xlsx"),
        "Formulas");

    [Fact]
    public void AFormulaLandmarkInsideASlicedRegionFindsTheRightCell()
    {
      // The one place a translation can be dropped without any test noticing. A capability answers
      // in the SPACE's coordinates — it is the sheet's formula table, not the region's — while a
      // landmark walks the region's own indices. So the walk has to mint a point and ask the
      // capability where the point says, and nowhere else.
      //
      // The band is B7:D10, four rows starting at the sheet's row 6. LOG10 lives at B9:D9, the
      // sheet's row 8, which is row 2 of the band. A landmark that asked FormulaAt(column, row) with
      // the band's own indices would look at A1:C4 instead — where there is no formula at all — and
      // report an honest, wrong "not found".
      var band = Plane<ISpace>.Of(FormulaSheet()).Slice(new Offset(1, 6), new Area(3, 4));

      Assert.Equal(2, RowWithFormula("LOG10").Landmark.FindRow(band));

      // Non-vacuity, twice over. The whole sheet puts the same formula at row 8, so the band's
      // answer is the same cell reached from a different corner...
      Assert.Equal(8, RowWithFormula("LOG10").Landmark.FindRow(Plane<ISpace>.Of(FormulaSheet())));

      // ...and the coordinates the untranslated reading would have used hold a formula too, so the
      // wrong answer would not have been an obvious zero: D7's SUM sits at the band's own (2, 0).
      Assert.Equal(0, RowWithFormula("SUM(D2:D5)").Landmark.FindRow(band));
    }

    [Fact]
    public void AndTheColumnTwinTranslatesTheSameWay()
    {
      // The transpose, over the same band: LOG10 appears in B9, C9 and D9, so the first column of the
      // band that carries one is the band's column 0 — which is the sheet's column 1.
      var band = Plane<ISpace>.Of(FormulaSheet()).Slice(new Offset(1, 6), new Area(3, 4));

      Assert.Equal(0, ColumnWithFormula("LOG10").Landmark.FindColumn(band));

      // And a needle that only column 2 of the band carries, so the answer is not the corner by
      // accident: C9's formula mentions C8 and no other does.
      Assert.Equal(1, ColumnWithFormula("LOG10(C8)").Landmark.FindColumn(band));
    }

    [Fact]
    public void ARegionNamesItsSpaceWhereverItStarts()
    {
      // The seam underneath both, and what replaced asking a space what it can do: a region does not
      // WRAP its space, it names it — so the sheet a band reads through is the sheet, from any
      // corner of it, and whatever that sheet can be asked is what the band can be asked.
      var sheet = FormulaSheet();
      var whole = Plane<ISpace>.Of(sheet);
      var band = whole.Slice(new Offset(1, 6), new Area(3, 4));

      Assert.Same(sheet, whole.Space);
      Assert.Same(whole.Space, band.Space);
      Assert.True(band.Space is IFormulaSpace);
    }
  }
}
