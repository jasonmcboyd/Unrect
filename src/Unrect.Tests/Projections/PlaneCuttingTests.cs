
using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// How the engine cuts the region it is working in — <see cref="Plane{TSpace}"/>'s own verbs,
  /// stated here over the sheet door and through the engine, where <c>PlaneTests</c> states them
  /// over the canonical one. A cut is the same space with a composed origin, allocating nothing.
  /// </summary>
  public class PlaneCuttingTests
  {
    // --- Slice(offset): the rest of a region, still being discovered ------------------------------------

    // --- Slice(offset, area): a named rectangle is a measured one ---------------------------------------

    // --- Narrowed: the horizontal twin ------------------------------------------------------------------

    // --- The origin rule --------------------------------------------------------------------------------

    [Fact]
    public void EveryCutIsAnArithmeticTranslationOfTheSameSpace()
    {
      // The rule, the other way up. A cut used to have to hand back a DIFFERENT space with its
      // origin back at zero, because the streaming store learned which band was open from the
      // subspace object's own extent. The store is told directly now, once per placement, so a cut
      // is what it always wanted to be: the same space with a composed origin, allocating nothing.
      var extent = Plane<ISheetCells>.Of(CoordinateGrid(4, 10));

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

        var rangeSlot = v.Next(Range(WholeExtent(), block => block.Space.Origin));

        return rangeSlot;
      }).Map(CoordinateGrid(4, 10));

      Assert.Equal(0, origin.Width);
      Assert.Equal(2, origin.Height);
    }
  }
}
