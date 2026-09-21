using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// What a typed predicate is handed when the calculus finally runs it. The lowering re-names a
  /// cell and a region over the file's space on the way in, and everything a predicate can read off
  /// either of them has to survive that: a point names the cell it really is, in the sheet's own
  /// coordinates, and a region names its own corner and keeps a bottom edge that is still being
  /// discovered.
  /// <para>
  /// It matters most exactly where it is least visible. A declaration nested under a movement is
  /// handed a region cut out of the sheet, and a predicate that saw the cut's own 0,0 instead of the
  /// sheet's would read the right cells and report the wrong places — and would read the WRONG cells
  /// the moment it asked the space directly.
  /// </para>
  /// </summary>
  public class TypedPredicateLoweringTests
  {
    /// <summary>Six rows of two values each over four blank ones, so a discovered bound has somewhere to stop.</summary>
    private static ICellSpace Ledger()
    {
      var values = new int[10, 2];

      for (var row = 0; row < 6; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      return Grid(values);
    }

    // --- A cell predicate ---------------------------------------------------------------------------

    [Fact]
    public void ACellPredicateIsHandedThePointsPlaceInTheSheetRatherThanInTheRegion()
    {
      // The region here starts at column 1, row 2 of a four-by-six sheet, so every coordinate a
      // predicate sees is at least that far in. A lowering that rebuilt the point from the region's
      // own origin would hand back 0,0 for the first cell — the same reading, a different cell.
      var sheet = CoordinateGrid(4, 6);
      var seen = new List<Point<ICellSpace>>();

      var region = Sized(RowsWhileAny(cell =>
      {
        seen.Add(cell);

        return cell.HasValue;
      })).Of(Range(block => block.Height));

      Assert.Equal(4, Down(2).Right(1).Of(region).Map(sheet));

      Assert.All(seen, cell => Assert.Same(sheet, cell.Space));
      Assert.Equal(new Point<ICellSpace>(sheet, 1, 2), seen[0]);
      Assert.Equal(1, seen.Min(cell => cell.Column));
      Assert.Equal(2, seen.Min(cell => cell.Row));

      // ...and the point really addresses that cell: a point minted by the region the predicate is
      // measuring reads the same value as one minted from the whole sheet.
      Assert.Equal(sheet.AsText(1, 2), seen[0].AsText());
    }

    [Fact]
    public void ACellPredicateDeepInAFlowStillSeesTheRootSpace()
    {
      // Nesting is where a lowering that carried a subspace rather than a cast would drift: every
      // level down is another region, and the predicate is written over the space named at the top
      // of the file — which is the one the sheet was handed to Map as.
      var sheet = CoordinateGrid(4, 6);
      var spaces = new List<ICellSpace>();

      var inner = Sized(RowsWhileAny(cell =>
      {
        spaces.Add(cell.Space);

        return cell.HasValue;
      })).Of(Range(block => block.Height));

      var declaration = VerticalFlow(outer =>
      {
        var down = outer.Next(
          Down(1).Of(VerticalFlow(middle =>
          {
            var right = middle.Next(Right(2).Of(inner));

            return right;
          })));

        return down;
      });

      declaration.Map(sheet);

      Assert.NotEmpty(spaces);
      Assert.All(spaces, space => Assert.Same(sheet, space));
    }

    // --- A region predicate -------------------------------------------------------------------------

    [Fact]
    public void ARegionPredicateIsHandedTheRegionsOwnCornerAndWidth()
    {
      var sheet = CoordinateGrid(4, 6);
      var seen = new List<Plane<ICellSpace>>();

      var region = Sized(SelectArea(plane =>
      {
        seen.Add(plane);

        return new Size(plane.Width, 1);
      })).Of(Range(block => $"{block.Width}x{block.Height}"));

      Assert.Equal("3x1", Down(2).Right(1).Of(region).Map(sheet));

      var measured = Assert.Single(seen);

      Assert.Same(sheet, measured.Space);
      Assert.Equal(new Offset(1, 2), measured.Origin);
      Assert.Equal(3, measured.Width);
    }

    [Fact]
    public void ARegionPredicateReadsTheSameCellsThroughTheRegionAsThroughTheSheet()
    {
      // A region indexes in its own coordinates and mints a point in the sheet's, and a retyped
      // region has to keep both halves of that. Read the same row two ways and the answers agree.
      var sheet = CoordinateGrid(4, 6);
      var read = new List<string?>();

      var region = Sized(SelectArea(plane =>
      {
        read.Add(plane[0, 0].AsText());
        read.Add(plane[2, 1].AsText());

        return new Size(plane.Width, 1);
      })).Of(Range(block => block.Height));

      Down(2).Right(1).Of(region).Map(sheet);

      Assert.Equal(new[] { sheet.AsText(1, 2), sheet.AsText(3, 3) }, read);
    }

    // --- A matcher's predicate ----------------------------------------------------------------------
  }
}
