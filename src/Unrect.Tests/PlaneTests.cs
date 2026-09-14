using System;

using Unrect.Core;
using Unrect.Spreadsheets;
using Unrect.Tests.Streaming;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// What a plane is: a region of a space, named rather than wrapped. It holds a root origin and an
  /// extent, so slicing is arithmetic and a point it mints is an address in the space itself.
  /// <para>
  /// It is also where coordinates are checked. The space refuses what is off its own edge; the
  /// plane refuses what is off the region a declaration was handed, which is the question a
  /// declaration is actually asking when it runs out of room.
  /// </para>
  /// </summary>
  public class PlaneTests
  {
    /// <summary>Three wide, two tall; every cell says its own coordinate.</summary>
    private static ISpace Grid() => GridSpace.Create(new[,]
    {
      { "0,0", "1,0", "2,0" },
      { "0,1", "1,1", "2,1" },
    });

    private static Plane<ISpace> Whole() => Plane<ISpace>.Of(Grid());

    // --- Construction -------------------------------------------------------------------------------

    [Fact]
    public void ThePlaneOfAWholeSpaceIsTheWholeOfIt()
    {
      var space = Grid();

      var plane = Plane<ISpace>.Of(space);

      Assert.Same(space, plane.Space);
      Assert.Equal(0, plane.Origin.Width);
      Assert.Equal(0, plane.Origin.Height);
      Assert.Equal(3, plane.Area.Width);
      Assert.Equal(2, plane.Area.Height);
    }

    [Fact]
    public void APlaneMustNameARegionItsSpaceActuallyHas()
    {
      // The invariant everything else rests on: a plane that reached past its space would mint
      // addresses the space would refuse, and the refusal would arrive at the read rather than here.
      var space = Grid();

      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, default, new Area(4, 2)));
      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, default, new Area(3, 3)));
      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, new Offset(2, 0), new Area(2, 1)));
      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, new Offset(0, 2), new Area(1, 1)));
    }

    [Fact]
    public void APlaneNeedsASpace()
    {
      // Not a bounds condition — a declaration cannot recover from having no document at all, so it
      // is the argument bug it looks like.
      Assert.Throws<ArgumentNullException>(() => new Plane<ISpace>(null!, default, default));
      Assert.Throws<ArgumentNullException>(() => Plane<ISpace>.Of(null!));
    }

    // --- Slicing ------------------------------------------------------------------------------------

    [Fact]
    public void SlicingComposesTheOriginOnce()
    {
      // Twice-composed offsets are where a locator that translated in the wrong frame — or twice —
      // would finally show up, and it is the only arithmetic a plane does.
      var band = Whole().Slice(new Offset(1, 0), new Area(2, 2));
      var corner = band.Slice(new Offset(1, 1), new Area(1, 1));

      Assert.Equal(1, band.Origin.Width);
      Assert.Equal(2, corner.Origin.Width);
      Assert.Equal(1, corner.Origin.Height);
      Assert.Equal("2,1", corner[0, 0].AsText());
    }

    [Fact]
    public void SliceRefusesARectangleThatDoesNotFit()
    {
      // The contract the spaces' own GetSubspace kept, asked of the plane instead: the same edges,
      // the same exception, so a declaration bounded against a region rather than a space discovers
      // its overrun the same way.
      var plane = Whole();

      Assert.Throws<OutOfBoundsException>(() => plane.Slice(default, new Area(4, 2)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(default, new Area(3, 3)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(2, 0), new Area(2, 1)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(0, 1), new Area(1, 2)));

      // ...and the region's edge, not the space's: this rectangle fits the sheet and not the band.
      var band = plane.Slice(new Offset(1, 0), new Area(2, 2));

      Assert.Throws<OutOfBoundsException>(() => band.Slice(default, new Area(3, 1)));
    }

    [Fact]
    public void AnEmptySliceIsARealPlaneRatherThanAFailure()
    {
      // The boundary the check has to fall on the right side of. A slice that arrives exactly at the
      // far edge has not run off it, and what is left is a real, empty region — a check written with
      // >= would turn "there is nothing after this" into an exception, which is a much worse answer
      // for a declaration asking precisely that.
      var plane = Whole();

      var nothing = plane.Slice(new Offset(3, 2), default);

      Assert.Equal(0, nothing.Area.Width);
      Assert.Equal(0, nothing.Area.Height);
      Assert.Equal(3, nothing.Origin.Width);

      // A zero-width band over real rows is legitimate too, the way a sheet with rows and no columns
      // is: the rows are there to be bounded against even though nothing can be read from them.
      var rowsOnly = plane.Slice(default, new Area(0, 2));

      Assert.Equal(0, rowsOnly.Area.Width);
      Assert.Equal(2, rowsOnly.Area.Height);
      Assert.Throws<OutOfBoundsException>(() => { _ = rowsOnly[0, 0]; });
    }

    // --- Minting ------------------------------------------------------------------------------------

    [Fact]
    public void TheIndexerRefusesACoordinateOutsideThePlane()
    {
      var plane = Whole();
      var band = plane.Slice(new Offset(1, 0), new Area(2, 1));

      Assert.Throws<OutOfBoundsException>(() => { _ = band[-1, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = band[2, 0]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = band[0, -1]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = band[0, 1]; });

      // (2, 0) of the band is a real cell of the sheet and is not in the band. A plane that checked
      // against the space instead of against itself would have handed it over.
      Assert.Equal("2,0", plane[2, 0].AsText());
    }

    [Fact]
    public void APointIsMintedInTheSpacesOwnCoordinates()
    {
      // The indexer is asked in the plane's frame and answers in the space's: (1, 0) of a band that
      // starts at (1, 1) is the sheet's (2, 1). A point that kept the local coordinates would read
      // the wrong cell the moment anyone but this plane held it.
      var band = Whole().Slice(new Offset(1, 1), new Area(2, 1));

      var point = band[1, 0];

      Assert.Equal(2, point.Column);
      Assert.Equal(1, point.Row);
      Assert.Equal("2,1", point.AsText());
    }

    // --- The slice law, at every door ---------------------------------------------------------------
    //
    // The fact rev 4's contract could not state and this one makes trivial: a slice is not another
    // space, so a point minted from it IS the parent's point at the translated coordinate — the same
    // object identity, the same coordinates, equal as addresses. A locator that lost or doubled its
    // origin fails this without any content needing to differ.

    public static TheoryData<string> Doors => new TheoryData<string> { "grid", "windowed", "xlsx" };

    private static ISpace Door(string door)
    {
      switch (door)
      {
        case "grid":
          return Grid();

        case "windowed":
          var source = new FakeRowSource(new FakeSheet("Data", 2, 3));
          var pool = new ReaderPool(source, 1, warmReaders: false);

          return new WindowedSpace(new SheetStore(pool, 0, "Data", 2, 3, chunkRows: 1, windowChunks: 4));

        default:
          return SpreadsheetSpace.Create(
            System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "simple-report.xlsx"),
            "Report");
      }
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void APointFromASliceIsTheParentsPointAtTheTranslatedCoordinate(string door)
    {
      var plane = Plane<ISpace>.Of(Door(door));

      Assert.True(plane.Area.Width >= 2 && plane.Area.Height >= 2, "the law needs a 2x2 space");

      var slice = plane.Slice(new Offset(1, 1), new Area(1, 1));

      Assert.Same(plane.Space, slice.Space);
      Assert.Equal(plane[1, 1], slice[0, 0]);
      Assert.Equal(plane[1, 1].GetHashCode(), slice[0, 0].GetHashCode());
      Assert.Equal(plane.Space.AsText(1, 1), slice[0, 0].AsText());

      // Non-vacuity for the identity above: the slice's own corner is not the parent's corner.
      Assert.NotEqual(plane[0, 0], slice[0, 0]);
    }

    // --- Equality -----------------------------------------------------------------------------------

    [Fact]
    public void TwoPlanesAreEqualWhenTheyNameTheSameRegionOfTheSameSpace()
    {
      var space = Grid();

      var first = new Plane<ISpace>(space, new Offset(1, 0), new Area(2, 2));
      var second = Plane<ISpace>.Of(space).Slice(new Offset(1, 0), new Area(2, 2));

      Assert.Equal(first, second);
      Assert.True(first == second);
      Assert.False(first != second);
      Assert.True(first.Equals((object)second));
      Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void APlaneDiffersFromOneNamingAnotherRegionOrAnotherSpace()
    {
      var space = Grid();
      var region = new Plane<ISpace>(space, new Offset(1, 0), new Area(2, 2));

      Assert.NotEqual(region, new Plane<ISpace>(space, new Offset(0, 0), new Area(2, 2)));
      Assert.NotEqual(region, new Plane<ISpace>(space, new Offset(1, 0), new Area(2, 1)));

      // Two grids built from the same literals hold the same cells and are still two documents.
      Assert.NotEqual(region, new Plane<ISpace>(Grid(), new Offset(1, 0), new Area(2, 2)));
    }

    [Fact]
    public void APlaneRendersItsOriginAndExtent()
    {
      // Diagnostics only, and in the space's own coordinates: a plane does not know where its space
      // sits in a workbook, so this is never an A1 address.
      Assert.Equal("(0,0) 3x2", Whole().ToString());
      Assert.Equal("(1,1) 2x1", Whole().Slice(new Offset(1, 1), new Area(2, 1)).ToString());
    }
  }
}
