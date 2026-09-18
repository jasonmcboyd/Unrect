using System;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests
{
  /// <summary>
  /// How an extent prints, and the congruence that makes it worth pinning: <c>WxH</c>, width first,
  /// the same characters wherever the number pair is printed from.
  /// <para>
  /// The point is not that <see cref="Size"/> has a <c>ToString</c> — every struct does — but that
  /// <see cref="Size"/>, <see cref="Area"/> and the extent half of <see cref="Plane{TSpace}"/> agree
  /// on one rendering. A diagnostic quotes whichever of the three it happens to be holding, and a
  /// reader comparing two messages must not have to know which. The default renderings they replaced
  /// were <c>Unrect.Core.Size</c> and <c>Unrect.Core.Area</c>: the type's name, three times, saying
  /// nothing about the extent at all.
  /// </para>
  /// <para>
  /// The congruence stops in exactly one place, deliberately, and that place is stated below: a
  /// region whose bottom edge is still being discovered renders <c>?</c> rather than settling, because
  /// rendering must never be the thing that reads the file.
  /// </para>
  /// </summary>
  public class ExtentRenderingTests
  {
    [Fact]
    public void ASizeRendersAsWidthByHeight()
    {
      // Width first, and asymmetric, because a renderer that had the components the wrong way round
      // is invisible on any square extent.
      Assert.Equal("4x2", new Size(4, 2).ToString());
      Assert.Equal("2x4", new Size(2, 4).ToString());
    }

    [Fact]
    public void AnExtentOfNothingStillRendersAsAPairOfNumbers()
    {
      // Zero is a real extent — an empty remainder is what a declaration asking "is there anything
      // after this" gets back — so it prints like any other rather than as a special case.
      Assert.Equal("0x0", default(Size).ToString());
      Assert.Equal("0x3", new Size(0, 3).ToString());
      Assert.Equal("0x0", default(Area).ToString());
    }

    [Fact]
    public void AnAreaRendersAsItsSizeDoesByWhicheverDoorItWasBuilt()
    {
      // An Area IS a Size with a name, and its two constructors are two spellings of one value, so
      // all three of these are the same extent and must read as one.
      Assert.Equal("4x2", new Area(4, 2).ToString());
      Assert.Equal("4x2", new Area(new Size(4, 2)).ToString());
      Assert.Equal(new Size(4, 2).ToString(), new Area(4, 2).ToString());
    }

    [Fact]
    public void APlaneRendersItsExtentTheWayASizeDoes()
    {
      // The congruence itself: a plane prints where it is and then how big it is, and the second
      // half is character-for-character what the extent would print on its own. A message quoting a
      // plane and one quoting the Area a strategy returned describe the same region in the same
      // words.
      var plane = Plane<ISheetCells>.Of(CoordinateGrid(4, 2));

      Assert.Equal("(0,0) 4x2", plane.ToString());
      Assert.EndsWith(new Size(4, 2).ToString(), plane.ToString(), StringComparison.Ordinal);
      Assert.EndsWith(plane.Area.ToString(), plane.ToString(), StringComparison.Ordinal);
    }
  }
}
