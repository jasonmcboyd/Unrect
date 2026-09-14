using Unrect.Core;
using Unrect.Projections;

using Xunit;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The one place 0-based coordinates become the 1-based address a failure message cites — asked of
  /// a bare offset, of a point and of a plane, which must all answer the same.
  /// <para>
  /// A locator carries its space's root coordinates, so locating one is the same conversion the
  /// engine has always done and not a second rule that could drift from it.
  /// </para>
  /// </summary>
  public class ProjectionLocationTests
  {
    private static ISpace Grid() => GridSpace.Create(new[,]
    {
      { "0,0", "1,0", "2,0" },
      { "0,1", "1,1", "2,1" },
    });

    [Fact]
    public void APointLocatesWhereItsOwnCoordinatesSay()
    {
      var plane = Plane<ISpace>.Of(Grid());

      var located = ProjectionLocation.At(plane[1, 1]);

      Assert.Equal(ProjectionLocation.At(new Offset(1, 1), plane.Area.Size).ToString(), located.ToString());
      Assert.Equal("B2", located.A1);
      Assert.Equal(plane.Area.Size.Width, located.Available.Width);
    }

    [Fact]
    public void APointFromASliceLocatesWhereTheParentsPointDoes()
    {
      // The address is the cell's, not the region's: two locators naming the same cell cite the same
      // A1, which is what stops a nested declaration reporting a failure at the wrong place.
      var plane = Plane<ISpace>.Of(Grid());
      var slice = plane.Slice(new Offset(2, 1), new Area(1, 1));

      Assert.Equal(
        ProjectionLocation.At(plane[2, 1]).ToString(),
        ProjectionLocation.At(slice[0, 0]).ToString());

      Assert.Equal("C2", ProjectionLocation.At(slice[0, 0]).A1);
    }

    [Fact]
    public void APlaneLocatesAtItsOriginAndCitesItsOwnExtent()
    {
      // A region is cited at the cell it starts on, and what was "available" is the region — the
      // room the declaration had rather than the room the sheet had.
      var band = Plane<ISpace>.Of(Grid()).Slice(new Offset(1, 1), new Area(2, 1));

      var located = ProjectionLocation.At(band);

      Assert.Equal(ProjectionLocation.At(new Offset(1, 1), new Size(2, 1)).ToString(), located.ToString());
      Assert.Equal("B2", located.A1);
      Assert.Equal(2, located.Available.Width);
      Assert.Equal(1, located.Available.Height);
    }
  }
}
