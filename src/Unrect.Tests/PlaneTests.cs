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

    [Fact]
    public void AnOverflowingSliceOfARegionStillBeingDiscoveredSettlesNothing()
    {
      // The worse half: on a region whose bottom edge is not yet known, a check that asked "how tall
      // are you" in order to refuse would read the file to exhaustion merely to say no — and to say
      // no to a row number no discovery could ever admit.
      var (plane, bound) = Bounded();

      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(0, int.MaxValue), new Area(0, 1)));
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void ANegativeRowIsAbsentWithoutTheBoundBeingAskedAtAll()
    {
      // The plane's own half of the shifted-row arithmetic. A bound counts from its own first row,
      // so a plane one row down asks about row + 1 — and it must refuse a negative row itself
      // rather than hand one over, because a bound asked about a negative row would add its shift
      // and answer about a perfectly ordinary one.
      //
      // (The other half, where a row number near int.MaxValue wraps as the shift is ADDED, belongs
      // to the bound and is pinned on the real one: BoundTests.ARowNumberThatWrapsPastTheShiftIsAbsent.)
      var (plane, bound) = Bounded();

      var rest = plane.Slice(new Offset(0, 1));

      Assert.False(rest.HasRow(-1));
      Assert.False(plane.HasRow(-1));

      // Row 0 was reached by the slice itself, which admits the row it starts on; the refusals
      // above added nothing to that, and settled nothing.
      Assert.Equal(0, bound.Reached);
      Assert.Equal(0, bound.Forced);
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
      Assert.Equal(plane.Space.AsText(2, 1), slice[0, 0].AsText());

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
      Assert.Equal(plane.Space.AsText(2, 0), inner[0, 0].AsText());

      // The once-translated cell, which is where a composition that dropped the inner offset would
      // have landed, and the band's own corner, which is where one that dropped the outer would.
      Assert.NotEqual(plane[1, 0], inner[0, 0]);
      Assert.NotEqual(band[0, 0], inner[0, 0]);
    }

    // --- A region whose bottom edge is still being discovered -----------------------------------------
    //
    // The half of the extent that is free, and the half that costs. A plane may carry a rule for
    // where its region ends instead of a height, and then Width and HasRow answer without reading
    // anything they do not have to, while Area reads the rule to exhaustion. Everything below is
    // about WHEN, not about what — which is why the bound here counts the questions it is asked.

    /// <summary>
    /// A bottom edge that admits a fixed number of rows and remembers how far it was ever asked to
    /// look. Forcing it is a separate count, so a test can say "this settled nothing".
    /// <para>
    /// <b>It is a faithful double and not the thing itself.</b> Its shifted view guards the same
    /// arithmetic the real one does — a row number near <c>int.MaxValue</c> wraps negative once the
    /// shift is added, and answering from the wrapped number would admit a row past the end of
    /// everything — so a law stated here over large row numbers is a law about <em>the plane</em>,
    /// not about the bound underneath it. The real guard is pinned on the real type, in
    /// <c>BoundTests.ARowNumberThatWrapsPastTheShiftIsAbsent</c>; without the mirror here a future
    /// law written against <see cref="Bounded"/> would pass for the wrong reason, and with it a
    /// green result here still proves nothing about production.
    /// </para>
    /// </summary>
    private sealed class CountingBound : IBound
    {
      private readonly int _height;
      private readonly int _shift;

      internal CountingBound(int height)
        : this(height, 0)
      {
      }

      private CountingBound(int height, int shift)
      {
        _height = height;
        _shift = shift;
      }

      /// <summary>The furthest row anything has asked about, or -1 where nothing has.</summary>
      internal int Reached { get; private set; } = -1;

      /// <summary>How many times the whole extent was settled.</summary>
      internal int Forced { get; private set; }

      public bool HasRow(int row)
      {
        if (_shift + row > Reached)
          Reached = _shift + row;

        return _shift + row < _height;
      }

      public int Force()
      {
        Forced++;
        Reached = _height;

        return _height - _shift;
      }

      public IBound Shift(int rows) => new Shifted(this, _shift + rows);

      /// <summary>A view of the same discovery counted further down, reporting through its owner.</summary>
      private sealed class Shifted : IBound
      {
        private readonly CountingBound _owner;
        private readonly int _shift;

        internal Shifted(CountingBound owner, int shift)
        {
          _owner = owner;
          _shift = shift;
        }

        public bool HasRow(int row)
        {
          var actual = _shift + row;

          // Mirrors the real Shifted's guard; see the class summary for why the mirror is here and
          // why a green test over it is not a statement about production.
          return actual >= 0 && _owner.HasRow(actual);
        }

        public int Force() => _owner.Force() - _shift;

        public IBound Shift(int rows) => new Shifted(_owner, _shift + rows);
      }
    }

    /// <summary>A 3x9 grid under a bottom edge that will admit only four of its rows.</summary>
    private static (Plane<ISpace> Plane, CountingBound Bound) Bounded(int height = 4)
    {
      var space = GridSpace.Create(new string?[9, 3]);
      var bound = new CountingBound(height);

      return (Plane<ISpace>.Of(space).Bounded(bound, 3), bound);
    }

    [Fact]
    public void WidthIsFreeOnARegionStillBeingDiscovered()
    {
      var (plane, bound) = Bounded();

      Assert.Equal(3, plane.Width);
      Assert.Equal(-1, bound.Reached);
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void HasRowReadsOnlyAsFarAsItTakesToAnswer()
    {
      // The whole point of carrying a rule instead of a height: a walk asks about the row it is
      // about to read and the discovery advances one row, not to the end of the sheet.
      var (plane, bound) = Bounded();

      Assert.True(plane.HasRow(0));
      Assert.Equal(0, bound.Reached);

      Assert.True(plane.HasRow(2));
      Assert.Equal(2, bound.Reached);

      Assert.False(plane.HasRow(4));
      Assert.Equal(4, bound.Reached);
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void AskingHowBigTheRegionIsSettlesIt()
    {
      // The forcing question, and the reason Width and HasRow exist beside it: an extent is a pair
      // of numbers and there is no answering half of one.
      var (plane, bound) = Bounded();

      var settled = plane.Area;

      Assert.Equal(3, settled.Width);
      Assert.Equal(4, settled.Height);
      Assert.Equal(1, bound.Forced);

      // Idempotent, but not free twice over: what it costs is the reading, and the reading is done.
      Assert.Equal(4, plane.Area.Height);
    }

    [Fact]
    public void RenderingARegionStillBeingDiscoveredSettlesNothing()
    {
      // A diagnostic must never be the thing that reads the file.
      var (plane, bound) = Bounded();

      Assert.Equal("(0,0) 3x?", plane.ToString());
      Assert.Equal(0, bound.Forced);
      Assert.Equal(-1, bound.Reached);
    }

    [Fact]
    public void TheIndexerRefusesARowPastTheDiscoveredEdge()
    {
      // A row below the bottom edge is an ordinary overrun, exactly as reading past a measured
      // extent is — which is how a declaration discovers it has run out of room.
      var (plane, bound) = Bounded();

      _ = plane[0, 3];

      Assert.Throws<OutOfBoundsException>(() => { _ = plane[0, 4]; });
      Assert.Throws<OutOfBoundsException>(() => { _ = plane[3, 0]; });
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void ANamedRectangleIsMeasuredEvenInsideARegionStillBeingDiscovered()
    {
      // Asking for part of a region is not a question about the whole of it: the rows asked for are
      // admitted one at a time, and what comes back has had its extent named, so it forces nothing
      // and answers from its own height thereafter.
      var (plane, bound) = Bounded();

      var cut = plane.Slice(new Offset(0, 1), new Area(2, 2));

      Assert.Equal(2, cut.Area.Height);
      Assert.Equal(0, bound.Forced);
      Assert.Equal(2, bound.Reached);

      Assert.Throws<OutOfBoundsException>(() => plane.Slice(default, new Area(2, 5)));
    }

    [Fact]
    public void TheRestOfARegionKeepsItsBottomEdgeUnsettled()
    {
      // The one slice that carries the discovery, and the arithmetic that makes the carried one
      // agree: the tail's row 0 is the bound's row 1, so nothing adds an origin to a row number.
      var (plane, bound) = Bounded();

      var rest = plane.Slice(new Offset(1, 1));

      Assert.Equal(2, rest.Width);
      Assert.Equal(0, bound.Forced);

      Assert.True(rest.HasRow(2));
      Assert.False(rest.HasRow(3));
      Assert.Equal(3, rest.Area.Height);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(9)]
    public void TheRestOfARegionStillFitsInsideItsSpaceOnceItSettles(int height)
    {
      // The ceiling a carried bound must not lift. A tail keeps the ORIGINAL extent's height and
      // moves its origin down, and its bottom edge is then whatever the shifted discovery says — so
      // if the shift were dropped anywhere, or the extent re-based instead of the bound, the settled
      // region would run off the end of the space it reads through. That is the shape of the bug
      // this catches, and it is invisible until somebody asks for the last row.
      //
      // Stated over three heights, including one that reaches the space's very last row, because the
      // failure is an off-by-one and the boundary case is where it would show.
      var space = GridSpace.Create(new string?[9, 3]);
      var plane = Plane<ISpace>.Of(space).Bounded(new CountingBound(height), 3);

      var rest = plane.Slice(new Offset(0, 3));

      var settled = rest.Area;

      Assert.Equal(3, rest.Origin.Height);
      Assert.Equal(height - 3, settled.Height);
      Assert.True(
        rest.Origin.Height + settled.Height <= rest.Space.Area.Height,
        $"the tail settled at rows {rest.Origin.Height}..{rest.Origin.Height + settled.Height - 1} "
        + $"of a space {rest.Space.Area.Height} rows tall");

      // Non-vacuous: the last row it admits really is readable through the space it names.
      Assert.True(rest.HasRow(settled.Height - 1));
      _ = rest[0, settled.Height - 1];
    }

    [Fact]
    public void AnOversizedOffsetIsABoundsConditionBeforeItIsASubtraction()
    {
      // The 2026-09-03 rule, kept here: without the check an oversized offset produces a negative
      // extent, and Size reports that as ArgumentOutOfRangeException — a fault, which can never be
      // absorbed — where running off the edge is the bounds condition a declaration recovers from.
      // And on a region still being discovered the check must not settle it merely to refuse.
      var (plane, bound) = Bounded();

      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(4, 0)));
      Assert.Throws<OutOfBoundsException>(() => plane.Slice(new Offset(0, 5)));
      Assert.Equal(0, bound.Forced);

      var measured = Whole();      // three wide, two tall

      Assert.Throws<OutOfBoundsException>(() => measured.Slice(new Offset(4, 0)));
      Assert.Throws<OutOfBoundsException>(() => measured.Slice(new Offset(0, 3)));

      // ...and an offset that arrives exactly at the edge is the empty remainder, not a failure.
      Assert.Equal(0, measured.Slice(new Offset(3, 2)).Area.Width);
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
