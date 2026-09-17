using System;
using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The tiler: <c>VerticalBands</c>/<c>HorizontalBands</c> cut the extent they are handed into bands
  /// of a fixed stride and apply one declared projection to each. Nothing about a band's content
  /// decides where the next one starts, so what is pinned here is the cutting itself — the band's
  /// extent, the trailing part-band that is not a band, the blank-band policy, the occurrence
  /// numbering, and the absence of a productivity guard.
  /// <para>
  /// A band is cut with the two-argument <c>GetSubspace</c>, which is what makes it a REAL measured
  /// space rather than a tail of the extent: a projection that derives its own extent measures itself
  /// inside a band of exactly the stride, and one that declares an area resolves it against a space
  /// that already knows how tall it is.
  /// </para>
  /// </summary>
  public class BandsProjectionTests
  {
    /// <summary>The extent a band projection was handed, as "WxH" — the whole of what a band is.</summary>
    private static IProjection<ISheetCells, string> BandExtent() => Range(WholeExtent(), block => $"{block.Width}x{block.Height}");

    /// <summary>The first cell of a band, so an assertion reads as the row or column it was cut from.</summary>
    private static IProjection<ISheetCells, int> FirstCell() => Range(WholeExtent(), block => block.Space[0, 0].IntegerOrBlank() ?? -1);

    // --- 1. What one band is -----------------------------------------------------------------------

    [Fact]
    public void ABandIsExactlyTheStrideTallAndAsWideAsTheExtent()
    {
      var sheet = CoordinateGrid(width: 3, height: 4);

      IReadOnlyList<string> ones = VerticalBands(1, BandExtent()).Map(sheet);
      IReadOnlyList<string> twos = VerticalBands(2, BandExtent()).Map(sheet);

      Assert.Equal(new[] { "3x1", "3x1", "3x1", "3x1" }, ones);
      Assert.Equal(new[] { "3x2", "3x2" }, twos);
    }

    [Fact]
    public void ABandProjectionThatDerivesItsExtentSeesTheBandAndNotTheTail()
    {
      // An overlay derives its extent from what its children place, so it is the shape that would
      // report the whole remaining tail if the band were a tail rather than a cut. The child reads
      // the extent the overlay was handed: one row, every time, and never the three below it.
      var sheet = CoordinateGrid(width: 3, height: 3);

      IReadOnlyList<string> seen = VerticalBands(1, Overlay(o =>
      {
        var bandExtent = o.Next(BandExtent());

        return o.Build(read => read.Of(bandExtent));
      })).Map(sheet);

      Assert.Equal(new[] { "3x1", "3x1", "3x1" }, seen);
    }

    [Fact]
    public void ABandProjectionWithADeclaredAreaSeesTheBandWithoutForcingADiscoveredBound()
    {
      // The band is measured, so a declared area resolves against it rather than against the bound
      // the tiler is walking: the first band projects having read only the row the walk has reached.
      var counter = new CountingSpace(Grid(new[,]
      {
        { 1, 2 },
        { 3, 4 },
        { 5, 6 },
        { 0, 0 },
      }));

      var observations = new List<int>();

      IReadOnlyList<string> bands = Sized(RowsWhileAnyValue()).Of(VerticalBands(1, Range(WholeExtent(), block =>
      {
        observations.Add(counter.RowsTouched);

        return $"{block.Width}x{block.Height}";
      }))).Map(counter);

      Assert.Equal(new[] { "2x1", "2x1", "2x1" }, bands);
      Assert.Equal(1, observations[0]);
    }

    // --- 2. Where the tiling ends ------------------------------------------------------------------

    [Fact]
    public void ATrailingPartBandIsNotABandAtAll()
    {
      // Five rows at a stride of two: the fifth row is not a band, so it is neither projected nor
      // consumed, and the shape after this one starts at it.
      var applied = VerticalBands(2, BandExtent()).Apply(CoordinateGrid(width: 2, height: 5));

      Assert.Equal(new[] { "2x2", "2x2" }, applied.Value);
      Assert.Equal(4, applied.Consumed.Height);
      Assert.Equal(2, applied.Consumed.Width);
    }

    [Fact]
    public void HorizontalBandsCutsTheOtherAxisTheSameWay()
    {
      // The across axis is the full height, and the stride runs left to right: three bands over six
      // columns, each starting one stride further along.
      var sheet = CoordinateGrid(width: 6, height: 2);

      IReadOnlyList<string> extents = HorizontalBands(2, BandExtent()).Map(sheet);
      IReadOnlyList<int> firstCells = HorizontalBands(2, FirstCell()).Map(sheet);

      Assert.Equal(new[] { "2x2", "2x2", "2x2" }, extents);
      Assert.Equal(new[] { 1, 3, 5 }, firstCells);
    }

    [Fact]
    public void AHorizontalPartBandIsNotABandEither()
    {
      var applied = HorizontalBands(2, BandExtent()).Apply(CoordinateGrid(width: 5, height: 2));

      Assert.Equal(new[] { "2x2", "2x2" }, applied.Value);
      Assert.Equal(4, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    // --- 3. The blank-band policy ------------------------------------------------------------------

    /// <summary>A value, a fully blank row, and a value: the shape every policy is distinguished on.</summary>
    private static ISheetCells InteriorBlank() => Grid(new[,]
    {
      { 1 },
      { 0 },
      { 3 },
    });

    [Fact]
    public void WithNoPolicyEveryBandIsAnOccurrenceIncludingABlankOne()
    {
      var applied = VerticalBands(1, FirstCell()).Apply(InteriorBlank());

      Assert.Equal(new[] { 1, -1, 3 }, applied.Value);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void StopEndsAtTheFirstBlankBandAndDoesNotConsumeIt()
    {
      var applied = VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Stop).Apply(InteriorBlank());

      Assert.Equal(new[] { 1 }, applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void SkipOmitsABlankBandAndAdvancesPastIt()
    {
      var read = VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Skip).MapWithDiagnostics(InteriorBlank());

      Assert.Equal(new[] { 1, 3 }, read.Value);
      Assert.Empty(read.Diagnostics);

      // The band the policy omitted was still cut out of the extent.
      Assert.Equal(3, VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Skip).Apply(InteriorBlank()).Consumed.Height);
    }

    [Fact]
    public void TolerateReadsWhatSkipReadsAndRecordsOneInfoPerBlankBand()
    {
      var skipped = VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Skip).Map(InteriorBlank());
      var read = VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Tolerate).MapWithDiagnostics(InteriorBlank());

      Assert.Equal(skipped, read.Value);

      var info = Assert.Single(read.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("the row at A2 is blank; it was skipped", info.Message);
      Assert.Equal("A2", info.Location.A1);
    }

    [Fact]
    public void AMultiRowBandCallsItselfABandRatherThanARow()
    {
      // The noun follows the stride: a one-row band is a row, and anything taller is a band.
      var sheet = Grid(new[,]
      {
        { 1 },
        { 2 },
        { 0 },
        { 0 },
      });

      var read = VerticalBands(2, FirstCell(), onBlank: BlankRowStrategy.Tolerate).MapWithDiagnostics(sheet);

      var info = Assert.Single(read.Diagnostics);

      Assert.Equal("the band at A3 is blank; it was skipped", info.Message);
    }

    [Fact]
    public void FaultThrowsAFaultThatNoToleranceBoundaryAbsorbs()
    {
      var tiler = VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Fault);

      var failure = Assert.Throws<ProjectionException>(() => tiler.Map(InteriorBlank()));

      Assert.True(failure.IsFault);
      Assert.Equal("the row at A2 is blank, which is not allowed here", Problem(failure));

      // A blank band is malformed data rather than an absent section, so Optional must let it out.
      var underOptional = Assert.Throws<ProjectionException>(() => tiler.Optional().Map(InteriorBlank()));

      Assert.True(underOptional.IsFault);
    }

    [Fact]
    public void HorizontalBandsRejectsAVerticalBlankRowPolicy()
    {
      var failure = Assert.Throws<ArgumentException>(() => HorizontalBands(2, FirstCell(), onBlank: BlankRowStrategy.Stop));

      Assert.Contains("vertical", failure.Message);
      Assert.Contains("HorizontalBands", failure.Message);
    }

    // --- 4. Which band a reading came from ---------------------------------------------------------

    [Fact]
    public void TheOccurrenceIndexSitsOnTheTilersOwnSegmentAndTheLabelOnTheBands()
    {
      // The third band fails: the index belongs to the tiler's segment, and the hoisted local names
      // every band, exactly as a repeat labels its occurrences.
      var sheet = Mixed(new object?[,]
      {
        { 10m },
        { 20m },
        { "oops" },
      });

      var allocation = Decimal();

      var failure = Assert.Throws<ProjectionException>(() => VerticalBands(1, allocation).Map(sheet));

      Assert.Equal("VerticalBands[2] -> 'allocation' (Decimal)", failure.Path);
      Assert.Equal("'allocation'", failure.Subject);
    }

    [Fact]
    public void ARecordsIndexIsTheBandOrdinal()
    {
      var sheet = Mixed(new object?[,]
      {
        { "a" },
        { "b" },
        { "c" },
      });

      IReadOnlyList<int> indices = VerticalBands(1, Record((TableRow<ISheetCells> row) => row.Index)).Map(sheet);

      Assert.Equal(new[] { 0, 1, 2 }, indices);
    }

    [Fact]
    public void AndCountsTheBandsAPolicySkipped()
    {
      // The ordinal is the band's, not the record's: a record after one skipped blank is band 2. It
      // is the number a reader would use to find the row in the file, which is the point of it.
      var sheet = Mixed(new object?[,]
      {
        { "a" },
        { null },
        { "b" },
      });

      IReadOnlyList<int> indices = VerticalBands(1, Record((TableRow<ISheetCells> row) => row.Index), onBlank: BlankRowStrategy.Skip).Map(sheet);

      Assert.Equal(new[] { 0, 2 }, indices);
    }

    // --- 5. The stride is the only thing that ends the walk ----------------------------------------

    [Fact]
    public void ABandThatConsumesNothingDoesNotEndTheTiling()
    {
      // A repeat stops when its item stops being productive; a tiler has no such guard, because its
      // boundaries never came from the item in the first place. Every row is a band even though
      // every band's reading was absorbed and consumed nothing.
      var sheet = Mixed(new object?[,]
      {
        { 1 },
        { 2 },
        { 3 },
      });

      var applied = VerticalBands(1, Text().Optional()).Apply(sheet);

      Assert.Equal(new string?[] { null, null, null }, applied.Value);
      Assert.Equal(3, applied.Consumed.Height);
    }

    // --- 6. Presence --------------------------------------------------------------------------------

    [Fact]
    public void AnExtentWithNoWholeBandInItIsEmptyRatherThanAFailure()
    {
      var sheet = CoordinateGrid(width: 2, height: 3);

      Assert.Equal(Presence.Empty, VerticalBands(5, BandExtent()).Apply(sheet).Presence);
      Assert.Equal(Presence.Read, VerticalBands(3, BandExtent()).Apply(sheet).Presence);

      // A tiler stopped at its first band by policy read nothing, and says so the same way.
      Assert.Equal(
        Presence.Empty,
        VerticalBands(1, FirstCell(), onBlank: BlankRowStrategy.Stop).Apply(Grid(new int[2, 2])).Presence);
    }

    // --- 7. Construction refusals -------------------------------------------------------------------

    [Fact]
    public void ANullBandProjectionIsRejectedAtConstruction()
    {
      Assert.Throws<ArgumentNullException>(() => VerticalBands(1, (IProjection<ISheetCells, int>)null!));
      Assert.Throws<ArgumentNullException>(() => HorizontalBands(1, (IProjection<ISheetCells, int>)null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AStrideBelowOneIsRejectedAtConstruction(int stride)
    {
      var vertical = Assert.Throws<ArgumentOutOfRangeException>(() => VerticalBands(stride, FirstCell()));
      var horizontal = Assert.Throws<ArgumentOutOfRangeException>(() => HorizontalBands(stride, FirstCell()));

      Assert.Contains("A band is at least one row or column across.", vertical.Message);
      Assert.Equal("rows", vertical.ParamName);
      Assert.Equal("columns", horizontal.ParamName);
    }

    // --- 8. Inspection -------------------------------------------------------------------------------

    [Fact]
    public void ATilerDescribesItselfAndDeclaresItsBandProjection()
    {
      var allocation = FirstCell();

      Assert.Equal("VerticalBands", VerticalBands(1, allocation).Description);
      Assert.Equal("HorizontalBands", HorizontalBands(1, allocation).Description);
      Assert.Same(allocation, Assert.Single(VerticalBands(2, allocation).Children).Projection);
    }
  }
}
