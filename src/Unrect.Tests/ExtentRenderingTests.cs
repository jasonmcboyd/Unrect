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
  /// <see cref="Size"/>, <see cref="Size"/> and the extent half of <see cref="Plane{TSpace}"/> agree
  /// on one rendering. A diagnostic quotes whichever of the three it happens to be holding, and a
  /// reader comparing two messages must not have to know which. The default renderings they replaced
  /// were <c>Unrect.Core.Size</c> and <c>Unrect.Core.Extent</c>: the type's name, three times, saying
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
    }

    [Fact]
    public void AnOffsetRendersAsAColumnRowPairLikeAPlanesOrigin()
    {
      // A displacement is a position, not an extent, so it prints the way a plane prints its origin
      // and never as WxH: an offset and a size of the same two numbers must not read as one.
      Assert.Equal("(1,2)", new Offset(1, 2).ToString());
      Assert.Equal("(0,0)", default(Offset).ToString());
      Assert.NotEqual(new Size(1, 2).ToString(), new Offset(1, 2).ToString());
    }

    [Fact]
    public void SizesAndOffsetsCompareByValue()
    {
      // Two sizes are the same extent when their numbers agree; two offsets, the same displacement.
      // Pinned because a plane's own equality is built from these, and because a plain struct
      // without them compares by reflection and boxes on the way.
      Assert.True(new Size(4, 2) == new Size(4, 2));
      Assert.True(new Size(4, 2) != new Size(2, 4));
      Assert.Equal(new Size(4, 2).GetHashCode(), new Size(4, 2).GetHashCode());
      Assert.True(new Size(4, 2).Equals((object)new Size(4, 2)));

      Assert.True(new Offset(1, 2) == new Offset(1, 2));
      Assert.True(new Offset(1, 2) != new Offset(2, 1));
      Assert.Equal(new Offset(1, 2).GetHashCode(), new Offset(1, 2).GetHashCode());
      Assert.True(new Offset(1, 2).Equals((object)new Offset(1, 2)));

      // A size and an offset of the same numbers are different things and are never equal.
      Assert.False(new Size(1, 2).Equals((object)new Offset(1, 2)));
    }

    [Fact]
    public void APlaneRendersItsExtentTheWayASizeDoes()
    {
      // The congruence itself: a plane prints where it is and then how big it is, and the second
      // half is character-for-character what the extent would print on its own. A message quoting a
      // plane and one quoting the Size a strategy returned describe the same region in the same
      // words.
      var plane = Plane<ICellSpace>.Of(CoordinateGrid(4, 2));

      Assert.Equal("(0,0) 4x2", plane.ToString());
      Assert.EndsWith(new Size(4, 2).ToString(), plane.ToString(), StringComparison.Ordinal);
      Assert.EndsWith(plane.Extent.ToString(), plane.ToString(), StringComparison.Ordinal);
    }
  }
}
