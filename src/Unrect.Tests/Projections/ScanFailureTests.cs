using System;
using System.Collections.Generic;
using System.IO;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// When a size rule's scan throws. The scan is the placement's, so its failure is reported as the
  /// placement's — the sentence names the area strategy, the path is the projection's and the cell
  /// is where the scan was — and never as the projection's own. Two consequences get their own
  /// facts because they are the ones that would hurt: a disk that stops answering inside a scan is
  /// a fault no tolerance boundary absorbs, and a rule that merely breaks on the data is absorbed
  /// like any other disagreement.
  /// </summary>
  public class ScanFailureTests
  {
    private static ICellSpace Sheet() => Grid(new[,]
    {
      { 1, 2, 3 },
      { 4, 5, 6 },
      { 7, 8, 9 },
      { 0, 0, 0 },
      { 0, 0, 0 },
    });

    /// <summary>A cell rule that breaks, absorbably, on the cell holding <paramref name="marker"/>.</summary>
    private static Func<Point<ICellSpace>, bool> BreaksOn(int marker)
      => cell => cell.IsDouble() && cell.Integer() == marker
        ? throw new InvalidOperationException("no")
        : true;

    /// <summary>A cell rule whose failure is the environment's, not the data's.</summary>
    private static Func<Point<ICellSpace>, bool> FaultsOn(int marker)
      => cell => cell.IsDouble() && cell.Integer() == marker
        ? throw new IOException("the disk stopped answering")
        : true;

    /// <summary>The sheet's 7 is the first cell of row 2, so a rule that breaks on it survives two rows first.</summary>
    private const int LateMarker = 7;

    private static ProjectionException Failure<T>(IProjectionDefinition<ICellSpace, T> projection)
      => Assert.Throws<ProjectionException>(() => projection.MapWithDiagnostics(Sheet()));

    // --- A broken scan is the placement's failure --------------------------------------------------

    [Fact]
    public void AScanThatBreaksIsReportedAsTheAreaStrategysFailure_NotTheProjections()
    {
      // The sentence names the area strategy, even though the break surfaced while the projection's
      // region was being placed. If this ever read "the projection threw", a declaration would be
      // blamed for its data source's rule.
      var failure = Failure(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0));

      Assert.Equal("its area strategy threw InvalidOperationException: no", Problem(failure));
      Assert.Equal("Range", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
    }

    [Fact]
    public void ABreakInsideAFlowChildCarriesTheChildsPath()
    {
      var projection = VerticalFlow(v => v.Next(Range(1, 1, b => b[0, 0].Integer())) + v.Next(Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Named("body")));

      // The last segment carries the kind alongside the name because the failure is the leaf's own.
      Assert.Equal("VerticalFlow -> 'body' (Range)", Failure(projection).Path);
    }

    // --- A fault is never tolerance ----------------------------------------------------------------

    [Theory]
    [InlineData("Optional")]
    [InlineData("Else")]
    [InlineData("Choice")]
    public void AnIoFaultInAScanIsNotAbsorbedByAToleranceBoundary(string boundary)
    {
      var broken = Range(RowsWhileAny(FaultsOn(LateMarker)), b => b.Height);

      IProjectionDefinition<ICellSpace, int> projection = boundary switch
      {
        "Optional" => broken.Optional(),
        "Else" => broken.Else(-1),
        "Choice" => Choice(broken, Range(WholeExtent(), b => b.Height)),

        _ => throw new ArgumentOutOfRangeException(nameof(boundary), boundary, "No such boundary."),
      };

      var failure = Failure(projection);

      Assert.True(failure.IsFault);
      Assert.IsType<IOException>(failure.InnerException);
    }

    // --- An absorbable break is absorbed -----------------------------------------------------------

    [Theory]
    [InlineData("Optional")]
    [InlineData("Else")]
    public void AnAbsorbableBreakInAScanIsAbsorbed(string boundary)
    {
      // The warning names the projection that failed, not the boundary that caught it.
      var broken = Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0);
      var projection = boundary == "Optional" ? broken.Optional() : broken.Else(-1);

      var result = projection.MapWithDiagnostics(Sheet());
      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal(boundary == "Optional" ? 0 : -1, result.Value);
      Assert.Equal("its area strategy threw InvalidOperationException: no", warning.Message);
      Assert.Equal("Range", warning.Path);
    }

    [Fact]
    public void AChoiceWhoseFirstAlternativeBreaksLeavesNothingBehind()
    {
      // Diagnostic rollback: the losing alternative got as far as being placed before its scan
      // broke, so if the collector were not rewound its near-miss would be joined by whatever it
      // noticed on the way.
      var projection = Choice(
        Range(RowsWhileAny(BreaksOn(LateMarker)), _ => 0).Named("first"),
        Range(WholeExtent(), b => b.Height).Named("second"));

      var result = projection.MapWithDiagnostics(Sheet());

      Assert.Equal(5, result.Value);

      var note = Assert.Single(result.Diagnostics);
      Assert.Equal(DiagnosticSeverity.Info, note.Severity);
      Assert.Contains("alternative 1", note.Message);
      Assert.Contains("its area strategy threw InvalidOperationException: no", note.Message);
    }

    // --- A repeat's item that runs out of room is a stop, not a failure ----------------------------

    /// <summary>Two blocks of values with one blank row between them, so a repeat finds exactly two.</summary>
    private static ICellSpace TwoBlocks() => Grid(new[,]
    {
      { 1, 2 },
      { 3, 4 },
      { 5, 6 },
      { 0, 0 },
      { 7, 8 },
      { 9, 10 },
    });

    [Fact]
    public void ARepeatOfDiscoveredItemsStopsRatherThanThrowing()
    {
      var repeat = VerticalRepeat(Range(RowsWhileAnyValue(), b => b.Height), separatedBy: BlankRows());

      IReadOnlyList<int> items = repeat.Map(TwoBlocks());

      // Two blocks, of three rows and two, and then the sheet runs out — a stop, not a failure.
      Assert.Equal(new[] { 3, 2 }, items);
    }
  }
}
