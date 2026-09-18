using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A discovered extent is a region of the sheet with an edge the declaration's rule drew, and the
  /// projection is handed that region and nothing past it. A cell below the edge is an overrun the
  /// declaration may recover from — the region refuses it, not the sheet, which has the row — and a
  /// wrong index into the view is the reader's own bug, which is a fault and stays one under any
  /// tolerance boundary.
  /// </summary>
  public class DiscoveredExtentTests
  {
    /// <summary>A hundred rows of values over three blank ones, so the rule stops at 100 and the sheet goes on to 103.</summary>
    private static ISheetCells TallSheet()
    {
      var values = new int[103, 2];

      for (var row = 0; row < 100; row++)
      {
        values[row, 0] = row + 1;
        values[row, 1] = (row + 1) * 2;
      }

      return Grid(values);
    }

    private const int BoundHeight = 100;

    [Fact]
    public void ARowBelowTheDiscoveredExtentIsAnOrdinaryOverrun()
    {
      // The extent is 100 rows and the sheet is 103, so row 100 exists and is still outside this
      // region — exactly as it would be outside a declared one. Nothing broke; the declaration ran
      // out of room, which is OutOfBounds and not the scan's own failure.
      var extent = Range(RowsWhileAnyValue(), block => block.Space[0, BoundHeight].IntegerOrBlank());

      var failure = Assert.Throws<ProjectionException>(() => extent.Map(TallSheet()));

      Assert.IsType<OutOfBoundsException>(failure.InnerException);
      Assert.False(failure.IsFault);

      // It is the REGION that refuses, not the space: the sheet underneath has a row 100 and
      // would have handed it over.
      Assert.Equal(103, TallSheet().Area.Height);
      Assert.True(TallSheet().IsBlank(0, BoundHeight));
    }

    [Fact]
    public void MintingAPointPastTheDiscoveredExtentRefusesWhereTheAddressIsMade()
    {
      // The refusal happens at the mint, which is the difference between "there is no such row
      // here" and "let me go and look": row 102 is a real row of the sheet and outside the region.
      Exception? refused = null;

      Range(RowsWhileAnyValue(), block =>
      {
        try
        {
          _ = block.Space[0, 102];
        }
        catch (OutOfBoundsException overrun)
        {
          refused = overrun;
        }

        return 0;
      }).Map(TallSheet());

      Assert.IsType<OutOfBoundsException>(refused);
    }

    [Fact]
    public void AnIndexPastTheDiscoveredExtentIsTheReadersBugAndNotTheFilesShape()
    {
      // The classification matters more than the message. block.Space[0, 100] is an overrun the
      // declaration may recover from; block[0, 100] is a wrong index into the view, which is
      // ArgumentOutOfRangeException and therefore on the fault list. Same row, same edge, different
      // verdict.
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(RowsWhileAnyValue(), block => block[0, BoundHeight].Integer()).Named("bad").Map(TallSheet()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.True(failure.IsFault);
    }

    [Fact]
    public void AnIndexPastTheDiscoveredExtentIsNotAbsorbedByATolerance()
    {
      // A reading bug reported as "this section was absent" would be the worst outcome a boundary
      // could produce.
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(RowsWhileAnyValue(), block => block[0, BoundHeight].Integer()).Named("bad").Optional().Map(TallSheet()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.GetBaseException());
      Assert.Equal("'bad'", failure.Subject);
    }
  }
}
