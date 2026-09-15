using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;
using Unrect.Tests.Streaming;

using Xunit;

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
  /// <b>The regions here are sliced by hand, not placed by the engine</b>, and that is not a
  /// convenience. Through phase 5 the engine still cuts real subspace objects and hands down planes
  /// whose origin is (0, 0), because the streaming window's locus rides on the subspace's own
  /// extent; arithmetic placement arrives in phase 6. So a hand-cut region is the only region with a
  /// non-zero origin there is, and pinning the calculus against one now is what stops phase 6 from
  /// being the first time anybody finds out. <see cref="APlacedRegionStillReportsItsOwnCoordinates"/>
  /// records the transitional half and re-bases when that lands.
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
    private static ICellValues Sheet(string door)
      => door == "grid"
        ? CoordinateGrid(5, 8)
        : Windowed(FakeSheet.Of("Data", Rows()));

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
      Assert.Equal(corner.AsText(), Sheet(door).AsText(2, 3));
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
    public void APlacedRegionStillReportsItsOwnCoordinates()
    {
      // The transitional half, recorded rather than left to be discovered. Through the engine a
      // region is a real subspace object with its origin at that object's corner, so a predicate
      // inside a placed section is shown 0,0 — the region's frame, not the sheet's. That is
      // deliberate for as long as the streaming store learns which band is open from the subspace's
      // own extent, and it changes when the store is told directly.
      //
      // RE-BASE IN PHASE 6: when placement becomes arithmetic this expectation becomes the sheet's
      // coordinates, and this test is the tripwire that says so rather than a silent behaviour swap.
      var seen = new List<string>();

      var probe = Projection.Down(3).Right(2).Of(
        Projection.Range(2, 2, block =>
        {
          for (var row = 0; row < block.Height; row++)
            for (var column = 0; column < block.Width; column++)
              seen.Add($"{block.Space.AsCanonical()[column, row].Column},{block.Space.AsCanonical()[column, row].Row}");

          return 0;
        }));

      probe.Map(CoordinateGrid(5, 8));

      Assert.Equal(new[] { "0,0", "1,0", "0,1", "1,1" }, seen);
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
    public void ACapabilityIsFoundThroughARegionWhereverItStarts()
    {
      // The seam underneath both: a region does not wrap its space, so asking the space for a
      // capability is asking the sheet — the same answer from any corner of it.
      var whole = Plane<ISpace>.Of(FormulaSheet());
      var band = whole.Slice(new Offset(1, 6), new Area(3, 4));

      Assert.NotNull(whole.Space.Capability<IFormulaSpace>());
      Assert.Same(whole.Space.Capability<IFormulaSpace>(), band.Space.Capability<IFormulaSpace>());
    }
  }
}
