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
  /// How the engine cuts the region it is working in — <see cref="Plane{TSpace}"/>'s own verbs, and
  /// which of them carries a bottom edge still being discovered.
  /// <para>
  /// The distinction is the whole of it. <c>Slice(offset)</c> and <c>Narrowed(width)</c> say "the
  /// rest of this", which is still a question about the region whose end nobody knows;
  /// <c>Slice(offset, area)</c> names a rectangle, and a named rectangle is a measured one. Getting
  /// it the other way round would either settle every bound at the first child (no streaming at all)
  /// or hand a nested declaration a region that silently outgrew what it asked for.
  /// </para>
  /// <para>
  /// The verbs are the plane's since the <c>Extents</c> helper was deleted; the laws are unchanged,
  /// and they are stated here over the sheet door and through the engine, where
  /// <c>PlaneTests</c> states them over the canonical one.
  /// </para>
  /// <para>
  /// The other rule pinned here is what replaced the phase's own. Until phase 6 every plane the
  /// engine made had its origin at (0, 0) over a real subspace object, because the streaming
  /// window's locus rode on that object's extent — so a cut was forbidden from translating
  /// arithmetically, and three tests enforced that (see the note further down, where they were).
  /// The store is told which band is open directly now (<c>ISweepAware</c>), so a cut is what it
  /// always wanted to be: the same space with a composed origin, allocating nothing.
  /// </para>
  /// </summary>
  public class PlaneCuttingTests
  {
    /// <summary>
    /// A bottom edge that admits a fixed number of rows and remembers how far it was asked to look,
    /// so a test can say "this settled nothing".
    /// </summary>
    private sealed class CountingBound : IBound
    {
      private readonly int _height;

      internal CountingBound(int height) => _height = height;

      /// <summary>How many times the whole extent was settled.</summary>
      internal int Forced { get; private set; }

      /// <summary>The furthest row anything asked about, or -1 where nothing did.</summary>
      internal int Reached { get; private set; } = -1;

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

      public IBound Shift(int rows) => new Shifted(this, rows);

      private sealed class Shifted : IBound
      {
        private readonly CountingBound _owner;
        private readonly int _rows;

        internal Shifted(CountingBound owner, int rows)
        {
          _owner = owner;
          _rows = rows;
        }

        public bool HasRow(int row) => _owner.HasRow(_rows + row);

        public int Force() => _owner.Force() - _rows;

        public IBound Shift(int rows) => new Shifted(_owner, _rows + rows);
      }
    }

    /// <summary>A four-wide, ten-tall grid under a bottom edge that will admit only six of its rows.</summary>
    private static (Plane<ISheetCells> Extent, CountingBound Bound) Discovering(int height = 6)
    {
      var bound = new CountingBound(height);

      return (Plane<ISheetCells>.Of(CoordinateGrid(4, 10)).Bounded(bound, 4), bound);
    }

    // --- Slice(offset): the rest of a region, still being discovered ------------------------------------

    [Fact]
    public void TheTailOfARegionKeepsItsBottomEdgeUnsettled()
    {
      // What a flow hands its next child and a repeat its next occurrence. If this measured, a flow
      // of two children over a discovered extent would settle the whole scan to place the second —
      // which is exactly the forcing the bound exists to avoid.
      var (extent, bound) = Discovering();

      var rest = extent.Slice(new Offset(1, 2));

      Assert.NotNull(rest.Bound);
      Assert.Equal(3, rest.Width);
      Assert.Equal(0, bound.Forced);

      // The tail's row r is the parent's row r + 2, so nothing adds an origin to a row number.
      for (var row = 0; row < 8; row++)
        Assert.Equal(extent.HasRow(row + 2), rest.HasRow(row));

      Assert.True(rest.HasRow(3));
      Assert.False(rest.HasRow(4));
      Assert.Equal(0, bound.Forced);
    }

    [Fact]
    public void TheTailReadsThroughASpaceThatIsStillMeasured()
    {
      // The subtlety that makes the tail work at all: the bound hides the rows below the boundary,
      // so the space underneath must stay its own full height — otherwise the scan would have
      // nowhere left to look when it goes hunting for the boundary it has not found yet. It is
      // structural now rather than something a cut has to preserve: a tail names the SHEET, at an
      // origin two rows down, so the ten rows are all still there to look through.
      var (extent, _) = Discovering();

      var rest = extent.Slice(new Offset(0, 2));

      // Ten rows of sheet to look through, four rows of region: the boundary is at the parent's
      // row 6, and the tail starts two rows into it.
      Assert.Same(extent.Space, rest.Space);
      Assert.Equal(10, rest.Space.Area.Height);
      Assert.Equal(2, rest.Origin.Height);
      Assert.Equal(4, rest.Area.Height);
    }

    // --- Slice(offset, area): a named rectangle is a measured one ---------------------------------------

    [Fact]
    public void ANamedRectangleCarriesNoBottomEdgeToDiscover()
    {
      // Asking for part of a region is not a question about the whole of it. The rows asked for are
      // admitted one at a time on the way in — so this still cannot reach past the boundary — and
      // what comes back answers from its own height thereafter.
      var (extent, bound) = Discovering();

      var cut = extent.Slice(new Offset(0, 1), new Area(2, 3));

      Assert.Null(cut.Bound);
      Assert.Equal(3, cut.Area.Height);
      Assert.Equal(2, cut.Width);
      Assert.Equal(0, bound.Forced);

      // The parent was advanced exactly as far as the rectangle reaches and no further: the last
      // row asked about is offset + height - 1, and the row after that is still nobody's business.
      Assert.Equal(3, bound.Reached);
    }

    [Fact]
    public void ARectangleReachingPastTheBoundaryIsRefusedWithoutSettlingIt()
    {
      var (extent, bound) = Discovering();

      Assert.Throws<OutOfBoundsException>(() => extent.Slice(new Offset(0, 4), new Area(2, 4)));
      Assert.Equal(0, bound.Forced);
    }

    // --- Narrowed: the horizontal twin ------------------------------------------------------------------

    [Fact]
    public void NarrowingKeepsTheBottomEdgeUnshifted()
    {
      // Columns are not rows: taking the leading columns of a region says nothing about where it
      // ends, so the discovery is carried across unchanged rather than re-based.
      var (extent, bound) = Discovering();

      var narrow = extent.Narrowed(2);

      Assert.NotNull(narrow.Bound);
      Assert.Same(extent.Bound, narrow.Bound);
      Assert.Equal(2, narrow.Width);
      Assert.Equal(0, bound.Forced);

      for (var row = 0; row < 8; row++)
        Assert.Equal(extent.HasRow(row), narrow.HasRow(row));
    }

    [Fact]
    public void AWidthPastTheEdgeIsAnOverrun_AndANegativeWidthIsAFault()
    {
      // The two halves of one rule, stated together because the division between them is the whole
      // point and is invisible when either is pinned alone.
      //
      // Four columns is one too many for a four-wide region, and running off an edge is a statement
      // about the DATA: a declaration may recover from it, a repeat stops on it, a tolerance
      // boundary may absorb it. Minus one is not a narrower region — it is not a region at all, and
      // nothing about the sheet could have produced the request. That is an argument bug, so it
      // arrives as ArgumentOutOfRangeException, which is on the engine's fault list and can never be
      // absorbed. Getting them the same way round would either make a bug look like an absent
      // section or make an ordinary overrun unrecoverable.
      var (extent, bound) = Discovering();

      Assert.Throws<OutOfBoundsException>(() => extent.Narrowed(extent.Width + 1));
      Assert.Throws<ArgumentOutOfRangeException>(() => extent.Narrowed(-1));

      // And the same division one level down, where the width arrives beside the bound: a plane
      // narrowed to nothing-at-all is the caller's mistake, not the sheet's.
      Assert.Throws<OutOfBoundsException>(() => extent.Bounded(bound, extent.Width + 1));
      Assert.Throws<ArgumentOutOfRangeException>(() => extent.Bounded(bound, -1));

      // Neither refusal settled anything: a region does not have to know where it ends to say that
      // minus one columns is not a width.
      Assert.Equal(0, bound.Forced);
    }

    // --- The origin rule --------------------------------------------------------------------------------

    [Fact]
    public void EveryCutIsAnArithmeticTranslationOfTheSameSpace()
    {
      // The rule, the other way up. A cut used to have to hand back a DIFFERENT space with its
      // origin back at zero, because the streaming store learned which band was open from the
      // subspace object's own extent. The store is told directly now, once per placement, so a cut
      // is what it always wanted to be: the same space with a composed origin, allocating nothing.
      var (extent, _) = Discovering();

      foreach (var (cut, origin) in new[]
      {
        (extent.Slice(new Offset(1, 1), new Area(2, 2)), new Offset(1, 1)),
        (extent.Slice(new Area(2, 2)), default(Offset)),
        (extent.Slice(new Offset(1, 1)), new Offset(1, 1)),
        (extent.Narrowed(2), default(Offset)),
      })
      {
        Assert.Same(extent.Space, cut.Space);
        Assert.Equal(origin.Width, cut.Origin.Width);
        Assert.Equal(origin.Height, cut.Origin.Height);
      }
    }

    // Three tests stood here and are gone with the rule they enforced: `Extents` REFUSED to cut a
    // translated region at all — every plane the engine made had its origin at (0, 0) over a real
    // subspace object, and a cut of one that did not was an EngineInvariantException, a fault no
    // tolerance absorbed. They were a theory over the three cuts, the same theory over every door,
    // and the classification pin beneath them. The refusal, the exception type and the transitional
    // rule are all deleted: a region is a locator over the root space now, translation is the normal
    // case, and the band a declaration is sweeping is announced rather than inferred.

    [Fact]
    public void MapMintsARootPlaneOverTheWholeSpace()
    {
      // Where every region in a reading comes from. Map does not wrap, adapt or measure anything: it
      // mints the one plane that names the whole of the space at its own corner, and hands it to the
      // declaration. Everything below is arithmetic on that.
      //
      // Both halves matter. The ORIGIN is the default — a root region starts where the space does,
      // which is what makes every coordinate underneath it the sheet's own — and the AREA is the
      // space's, unqualified, so nothing is hidden from a declaration before it has said anything.
      var space = CoordinateGrid(4, 10);

      var root = Range(WholeExtent(), block => block.Space).Map(space);

      Assert.Same(space, root.Space);
      Assert.Equal(default(Offset).Width, root.Origin.Width);
      Assert.Equal(default(Offset).Height, root.Origin.Height);
      Assert.Equal(space.Area.Size.Width, root.Area.Size.Width);
      Assert.Equal(space.Area.Size.Height, root.Area.Size.Height);
    }

    [Fact]
    public void AProjectionIsHandedARegionWhoseOriginIsWhereThePlacementPutIt()
    {
      // The same rule seen from the declaration's side, through the engine rather than the helper: a
      // block placed after two consumed rows reports (0, 2), because what it was handed is a locator
      // over the sheet and not a space of its own. That IS the A1 the engine cites — one origin, one
      // frame, nothing to add it to.
      //
      // Until phase 6 this read (0, 0): a placed region was a fresh subspace object whose corner was
      // its own, and the engine carried the sheet position separately in ProjectionContext.Origin.
      // The context no longer accumulates one, so a plane that reported (0, 0) here would now lose
      // the two rows rather than duplicate them.
      var origin = VerticalFlow(v =>
      {
        v.Next(Row(cells => cells.Count));
        v.Next(Row(cells => cells.Count));

        return v.Next(Range(WholeExtent(), block => block.Space.Origin));
      }).Map(CoordinateGrid(4, 10));

      Assert.Equal(0, origin.Width);
      Assert.Equal(2, origin.Height);
    }
  }
}
