using System;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The engine's half of a region whose bottom edge is discovered while it is read: the scan, the
  /// position it advances through, and the shifted view of it a region offset into a bounded one
  /// carries.
  /// <para>
  /// <see cref="PlaneTests"/> states the same rules through a stand-in bound, which is right for
  /// laws about what a <em>plane</em> does with one. These are about the real implementation, and
  /// the arithmetic here is the reason: a shift is added to every row number that arrives, so the
  /// guards live in <c>Bound</c> and a double that mirrored them could only prove itself.
  /// </para>
  /// </summary>
  public class BoundTests
  {
    /// <summary>
    /// A scan that admits a fixed number of rows and counts what it was asked, so a test can say
    /// "this settled nothing" and "this read no further than it had to".
    /// </summary>
    private sealed class CountingScan : IAreaScan
    {
      private readonly int _height;

      internal CountingScan(int height, int width)
      {
        _height = height;
        Width = width;
      }

      public int Width { get; }

      /// <summary>The furthest row the scan was ever asked about, or -1 where it was never asked.</summary>
      internal int Asked { get; private set; } = -1;

      public bool IncludesRow(ICellValues space, int row)
      {
        if (row > Asked)
          Asked = row;

        return row < _height;
      }
    }

    /// <summary>A four-row bound over a ten-row grid — three columns, every cell a number.</summary>
    private static (Bound Bound, CountingScan Scan) Discovering(int height = 4, int width = 3)
    {
      var scan = new CountingScan(height, width);

      return (new Bound(CoordinateGrid(width, 10), scan, exception => throw exception), scan);
    }

    [Fact]
    public void ABoundAdvancesThroughTheRowItIsAskedAboutAndNoFurther()
    {
      var (bound, scan) = Discovering();

      Assert.True(bound.HasRow(0));
      Assert.Equal(0, scan.Asked);

      Assert.True(bound.HasRow(2));
      Assert.Equal(2, scan.Asked);

      Assert.False(bound.HasRow(4));
      Assert.Equal(4, scan.Asked);
    }

    [Fact]
    public void ANegativeRowIsAbsentAndSettlesNothing()
    {
      // There is no row before the first one, and saying so must cost nothing: a bound asked about
      // a negative row has been asked a question no reading could answer, and reading the file to
      // find that out would be the worst of both. The guard matters because a shifted bound adds
      // its offset to whatever arrives, so a negative can reach a bound that nobody meant to ask.
      var (bound, scan) = Discovering();

      Assert.False(bound.HasRow(-1));
      Assert.False(bound.HasRow(int.MinValue));

      // "Settled nothing", told from the scan rather than from the bound: asking the bound would
      // settle it in the asking, and a scan that was never asked anything cannot have been read to
      // exhaustion. The bound is still discoverable afterwards, which is the other half of it.
      Assert.Equal(-1, scan.Asked);
      Assert.True(bound.HasRow(0));
      Assert.Equal(0, scan.Asked);
    }

    [Fact]
    public void ForcingReadsToExhaustionAndIsIdempotent()
    {
      var (bound, scan) = Discovering();

      Assert.Equal(4, bound.Force());
      Assert.Equal(4, scan.Asked);

      Assert.Equal(4, bound.Force());
      Assert.Equal(4, scan.Asked);
    }

    [Fact]
    public void AScanThatNeverStopsIsEndedByRunningOutOfRows()
    {
      // The loop's other exit. A scan whose rule admits everything would spin forever against a
      // height it is never told, so the bound stops it at the space's own last row.
      var scan = new CountingScan(int.MaxValue, 3);
      var bound = new Bound(CoordinateGrid(3, 10), scan, exception => throw exception);

      Assert.Equal(10, bound.Force());
      Assert.False(bound.HasRow(10));
    }

    // --- The shift ------------------------------------------------------------------------------------

    [Fact]
    public void AShiftedBoundIsTheSameDiscoveryCountedFromFurtherDown()
    {
      // Two bounds over one scan is the point: a region stepped through in pieces reads each row
      // exactly once, so the shifted view must share the discovery rather than restart it.
      var (bound, scan) = Discovering();

      var rest = bound.Shift(1);

      Assert.True(rest.HasRow(2));       // the bound's row 3, the last it admits
      Assert.Equal(3, scan.Asked);

      Assert.False(rest.HasRow(3));
      Assert.Equal(3, rest.Force() + 0);
      Assert.Equal(4, bound.Force());
    }

    [Fact]
    public void ShiftingByNothingIsTheSameBound()
    {
      // A no-op shift must not cost an object, because a flow hands its first child the tail at
      // offset zero and would otherwise wrap one per child.
      var (bound, _) = Discovering();

      Assert.Same(bound, bound.Shift(0));
    }

    [Fact]
    public void ShiftingATwiceShiftedBoundStillCountsFromTheOriginalDiscovery()
    {
      // A flow steps its tail one child at a time, so the shifts compose — and a composition that
      // wrapped instead of adding would ask the scan about a row it had already passed.
      var (bound, scan) = Discovering();

      var rest = bound.Shift(1).Shift(2);

      Assert.True(rest.HasRow(0));       // the bound's row 3
      Assert.Equal(3, scan.Asked);

      Assert.False(rest.HasRow(1));
      Assert.Equal(1, rest.Force());
    }

    [Fact]
    public void ARowNumberThatWrapsPastTheShiftIsAbsent()
    {
      // The arithmetic hazard the shift creates. Both terms are non-negative, so a row number near
      // int.MaxValue asked of a shifted bound wraps to a negative — and answering from the wrapped
      // number would say YES, admitting a row past the end of everything and handing the scan a
      // negative row it was never promised.
      var (bound, scan) = Discovering();

      var rest = bound.Shift(1);

      Assert.False(rest.HasRow(int.MaxValue));

      // ...and it said so without asking the scan anything at all.
      Assert.Equal(-1, scan.Asked);
    }

    // --- A scan that breaks ---------------------------------------------------------------------------

    [Fact]
    public void AScanThatBreaksWearsThePlacementsFailure()
    {
      // A strategy that throws while the projection consumes broke for the same reason it would
      // have broken up front, so the bound hands it to the failure the placement was deferred from
      // rather than letting it surface as whatever it happens to be.
      Exception? handed = null;

      var bound = new Bound(
        CoordinateGrid(3, 10),
        new BreakingScan(),
        exception =>
        {
          handed = exception;

          return new ProjectionException(
            "'section'", "its area ran past the space available here", "section", "section",
            default, null, Projection.Text(), exception);
        });

      var failure = Assert.Throws<ProjectionException>(() => bound.HasRow(0));

      Assert.Equal("'section'", failure.Subject);
      Assert.Same(handed, failure.InnerException);
      Assert.IsType<InvalidOperationException>(handed);
    }

    /// <summary>A scan whose rule throws the moment it is asked anything.</summary>
    private sealed class BreakingScan : IAreaScan
    {
      public int Width => 3;

      public bool IncludesRow(ICellValues space, int row) => throw new InvalidOperationException("the scan broke");
    }
  }
}
