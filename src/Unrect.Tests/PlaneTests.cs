using System;

using Unrect.Core;

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
    private static ISpace Grid() => ProjectionTestSpaces.Door("grid");

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

    [Fact]
    public void EachSpellingTakesTheSameRectangleAsTheLongOne()
    {
      // The two short forms are shorthand, not different operations: an offset with no area means
      // "the rest", and an area with no offset means "from the corner". Pinned beside their failure
      // mode so the three spellings read as one rule rather than as three unrelated facts.
      //
      // (Stated over one door because slicing is arithmetic on the locator: a plane composes the
      // same numbers whatever space it names. What varies by door is the READ, and that is
      // SpaceContractTests' theory.)
      var plane = Whole();      // three wide, two tall

      var remainder = plane.Slice(new Offset(1, 1));

      Assert.Equal(2, remainder.Area.Width);
      Assert.Equal(1, remainder.Area.Height);
      Assert.Equal("1,1", remainder[0, 0].AsText());

      var corner = plane.Slice(new Area(2, 1));

      Assert.Equal("0,0", corner[0, 0].AsText());
      Assert.Equal("1,0", corner[1, 0].AsText());
      Assert.Throws<OutOfBoundsException>(() => { _ = corner[2, 0]; });

      // ...and each refuses an oversized request as the bounds condition the long form does.
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(4, 0)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(0, 3)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(4, 0), new Area(1, 1)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Area(4, 1)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Area(1, 3)));
    }

    // --- Overflow is a bounds condition, not an argument bug ------------------------------------------
    //
    // The arithmetic hazard the checks are written around. Every component is non-negative, so
    // origin + area near int.MaxValue WRAPS to a negative — and a negative passes a naive "is it
    // bigger than the extent" test. The region would then be built past the edge and fail later as
    // ArgumentOutOfRangeException, which is on the engine's fault list and can never be absorbed by
    // a tolerance boundary. Running off the edge of a space is the bounds condition a declaration
    // recovers from, at int.MaxValue exactly as at 4.

    [Fact]
    public void AnOffsetThatWouldOverflowTheWidthIsRefusedAsAnOverrun()
    {
      // Width first, and with a zero-height area so nothing else could be doing the refusing.
      Assert.Throws<OutOfBoundsException>(() => Whole().Slice(new Offset(int.MaxValue, 0), new Area(1, 0)));
    }

    [Fact]
    public void AnOffsetThatWouldOverflowTheHeightIsRefusedAsAnOverrun()
    {
      // The twin, and the one the slice checks separately: a row sum that wraps is refused outright
      // rather than asked about, because a region reaching that far is past any edge there could be.
      Assert.Throws<OutOfBoundsException>(() => Whole().Slice(new Offset(0, int.MaxValue), new Area(0, 1)));
    }

    [Fact]
    public void TheConstructorRefusesAnOverflowingRegionTheSameWay()
    {
      // The other way in, and the one that matters most: the constructor is public, so a caller
      // outside the engine reaches it directly. Both axes, both refused as bounds conditions.
      var space = Grid();

      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, new Offset(int.MaxValue, 0), new Area(1, 0)));
      Assert.Throws<OutOfBoundsException>(() => new Plane<ISpace>(space, new Offset(0, int.MaxValue), new Area(0, 1)));
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

    public static TheoryData<string> Doors => ProjectionTestSpaces.Doors;

    private static ISpace Door(string door) => ProjectionTestSpaces.Door(door);

    [Theory]
    [MemberData(nameof(Doors))]
    public void APointFromASliceIsTheParentsPointAtTheTranslatedCoordinate(string door)
    {
      var plane = Plane<ISpace>.Of(Door(door));

      Assert.True(plane.Area.Width >= 3 && plane.Area.Height >= 2, "the law needs a 3x2 space");

      // Asymmetric on both axes, because an offset that composed by the wrong component — or by the
      // right one twice — is invisible wherever the two are equal.
      var slice = plane.Slice(new Offset(2, 1), new Area(1, 1));

      Assert.Same(plane.Space, slice.Space);
      Assert.Equal(plane[2, 1], slice[0, 0]);
      Assert.Equal(plane[2, 1].GetHashCode(), slice[0, 0].GetHashCode());
      Assert.Equal(plane.Space.AsTextAt(2, 1), slice[0, 0].AsText());

      // Non-vacuity for the identity above, and the transposed coordinate specifically: (1, 2) is
      // what a locator that had swapped its components would have landed on.
      Assert.NotEqual(plane[0, 0], slice[0, 0]);
      Assert.NotEqual(plane[1, 1], slice[0, 0]);
    }

    [Theory]
    [MemberData(nameof(Doors))]
    public void ASliceOfASliceComposesOnceAtEveryDoor(string door)
    {
      // Depth is where a translation that happened twice, or in the wrong frame, finally shows: the
      // inner slice is asked in the band's coordinates and must name the sheet's (1, 0) + (1, 0).
      var plane = Plane<ISpace>.Of(Door(door));

      Assert.True(plane.Area.Width >= 3 && plane.Area.Height >= 2, "the law needs a 3x2 space");

      var band = plane.Slice(new Offset(1, 0), new Area(2, 2));
      var inner = band.Slice(new Offset(1, 0), new Area(1, 2));

      Assert.Same(plane.Space, inner.Space);
      Assert.Equal(plane[2, 0], inner[0, 0]);
      Assert.Equal(plane[2, 1], inner[0, 1]);
      Assert.Equal(plane.Space.AsTextAt(2, 0), inner[0, 0].AsText());

      // The once-translated cell, which is where a composition that dropped the inner offset would
      // have landed, and the band's own corner, which is where one that dropped the outer would.
      Assert.NotEqual(plane[1, 0], inner[0, 0]);
      Assert.NotEqual(band[0, 0], inner[0, 0]);
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
