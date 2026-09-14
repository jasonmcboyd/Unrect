using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The <c>onBlank</c> blank-row terminator on <c>VerticalRepeat</c>/<c>HorizontalRepeat</c>: the
  /// repeat re-hosts a discovered block (<c>Stop</c>) or an edge block (<c>Skip</c>/<c>Tolerate</c>/
  /// <c>Fault</c>) and walks it a band at a time via <see cref="BoundedSpace.HasRow"/>, judging each
  /// fully-blank band by policy.
  /// <para>
  /// The composition under test is the canonical labelled-axes one — a header consumed by
  /// <see cref="Projection.ColumnLabels(int)"/>, its map pushed by
  /// <see cref="Projection.WithColumnLabels{T}(LabelMap, IProjection{T})"/>, and a
  /// <see cref="Projection.Record{T}(Func{TableRow, T})"/> repeated beneath it. Value-equivalence to
  /// the leaf <c>Table</c> is asserted only on the clean/trailing shape (the <c>Stop</c> case);
  /// interior-blank shapes are asserted against explicit records rather than leaf-parity.
  /// </para>
  /// </summary>
  public class RepeatBlankRowStrategyTests
  {
    private sealed record Line(string Investor, decimal Amount);

    private static Line ReadLine(TableRow row) => new Line(row.Text("Investor"), row.Decimal("Amount"));

    /// <summary>
    /// The canonical table composition parameterised over the blank-row policy: a header band read
    /// as a <see cref="LabelMap"/>, its labels pushed for the body, and a <c>Record</c> repeated under
    /// <paramref name="onBlank"/>. The flow wears the leaf table's own skip-to-first-non-blank-cell
    /// offset and its "Table" description; the block is re-hosted inside the repeat, so the flow
    /// carries no area.
    /// </summary>
    private static IProjection<IReadOnlyList<T>> Composition<T>(BlankRowStrategy onBlank, Func<TableRow, T> record)
      => new FlowProjection<IReadOnlyList<T>>(
        Orientation.Vertical,
        flow =>
        {
          var columns = flow.Next(ColumnLabels(1));

          return flow.Next(WithColumnLabels(columns, VerticalRepeat(Record(record), onBlank: onBlank)));
        },
        new Placement(OffsetStrategies.SkipToFirstNonBlankCell(), null),
        "Table");

    /// <summary>
    /// A clean two-row block with real content past a blank row — the shape that distinguishes a
    /// self-bounding <c>Stop</c> (ends at the blank) from a run-to-edge policy (reads the Total).
    /// </summary>
    private static ISpace CleanTrailing() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { "Beta", 20m },
      { null, null },
      { "Total", 30m },
    });

    /// <summary>
    /// Records on both sides of a single interior blank row (sheet row 2, A3), no trailing blank —
    /// the shape every non-Stop policy is distinguished on.
    /// </summary>
    private static ISpace InteriorBlank() => Mixed(new object?[,]
    {
      { "Investor", "Amount" },
      { "Acme", 10m },
      { null, null },
      { "Gamma", 30m },
    });

    private static int RowsTouched<T>(IProjection<IReadOnlyList<T>> projection, ISpace sheet)
    {
      var counter = new CountingSpace(sheet);
      projection.Map(counter);

      return counter.RowsTouched;
    }

    // --- 1. Stop streams and self-bounds -----------------------------------------------------------

    [Fact]
    public void StopStopsAtTheFirstBlankRowAndTouchesOnlyTheBlockAndItsTerminator()
    {
      var counter = new CountingSpace(CleanTrailing());

      var records = Composition(BlankRowStrategy.Stop, ReadLine).Map(counter);

      // The block ends at the blank; the Total past it is never read.
      Assert.Equal(new[] { new Line("Acme", 10m), new Line("Beta", 20m) }, records);

      // Header + the two block rows + the one blank row the walk peeks to discover the end — four,
      // not the five an up-front measurement would touch.
      Assert.Equal(4, counter.RowsTouched);
    }

    [Fact]
    public void StopTouchesTheSameRowsAsTheLeafTableOverTheSameSheet()
    {
      // The composition streams in step with the bespoke leaf: same rows touched by the time Map
      // returns, over the clean/trailing shape leaf-parity is scoped to.
      Assert.Equal(
        RowsTouched(Table(1, ReadLine, BlankRowStrategy.Stop), CleanTrailing()),
        RowsTouched(Composition(BlankRowStrategy.Stop, ReadLine), CleanTrailing()));
    }

    // --- 2. Skip -----------------------------------------------------------------------------------

    [Fact]
    public void SkipYieldsRecordsFromBothSidesOfAnInteriorBlankAndReportsNothing()
    {
      var read = Composition(BlankRowStrategy.Skip, ReadLine).MapWithDiagnostics(InteriorBlank());

      Assert.Equal(new[] { new Line("Acme", 10m), new Line("Gamma", 30m) }, read.Value);
      Assert.Empty(read.Diagnostics);
    }

    // --- 3. Tolerate -------------------------------------------------------------------------------

    [Fact]
    public void TolerateYieldsTheSameRecordsAsSkipAndRecordsOneInfoPerInteriorBlank()
    {
      var skip = Composition(BlankRowStrategy.Skip, ReadLine).Map(InteriorBlank());
      var read = Composition(BlankRowStrategy.Tolerate, ReadLine).MapWithDiagnostics(InteriorBlank());

      // Parsing continued exactly as Skip: the blank is omitted, the content past it read.
      Assert.Equal(skip, read.Value);

      // One nonterminal Info for the one interior blank, citing its A1 (sheet row 2 == A3).
      var infos = read.Diagnostics.Where(d => d.Message.Contains("is blank")).ToList();

      Assert.Single(infos);
      Assert.Equal(DiagnosticSeverity.Info, infos[0].Severity);
      Assert.Contains("A3", infos[0].Message);

      // Nothing was elevated past Info.
      Assert.DoesNotContain(read.Diagnostics, d => d.Severity != DiagnosticSeverity.Info);
    }

    // --- 4. Fault ----------------------------------------------------------------------------------

    [Fact]
    public void FaultThrowsAFaultCitingTheBlankRowsA1()
    {
      var failure = Assert.Throws<ProjectionException>(() => Composition(BlankRowStrategy.Fault, ReadLine).Map(InteriorBlank()));

      Assert.True(failure.IsFault);
      Assert.Contains("is blank", failure.Message);
      Assert.Contains("A3", failure.Message);
    }

    [Fact]
    public void AFaultInTheRepeatPassesThroughOptionalAndElse()
    {
      // A blank-row Fault is malformed data, not an absent section, so a tolerance boundary must let
      // it through rather than turning it into null or the fallback.
      var underOptional = Assert.Throws<ProjectionException>(
        () => Composition(BlankRowStrategy.Fault, ReadLine).Optional().Map(InteriorBlank()));

      Assert.True(underOptional.IsFault);

      var fallback = (IReadOnlyList<Line>)Array.Empty<Line>();
      var underElse = Assert.Throws<ProjectionException>(
        () => Composition(BlankRowStrategy.Fault, ReadLine).Else(fallback).Map(InteriorBlank()));

      Assert.True(underElse.IsFault);
    }

    // --- 4b. A declared extent is the bound the walk uses ------------------------------------------

    [Fact]
    public void ADeclaredExtentIsTheBoundRatherThanASecondSelfBinding()
    {
      // A policy re-hosts a block inside the repeat, but only where the placement declared none: a
      // repeat that bound itself a second time would narrow an extent somebody else had already
      // decided. The sheet is the wedge — its first column is empty, so a self-bound block would
      // take a zero-wide leading region and collect nothing at all, while the declared extent is the
      // full width and every record sees both columns.
      var sheet = Mixed(new object?[,]
      {
        { null, "Acme" },
        { null, "Beta" },
        { null, null },
      });

      var declared = Sized(RowsWhileAnyValue())
        .Of(VerticalRepeat(Record((TableRow row) => row.Count), onBlank: BlankRowStrategy.Stop));

      Assert.Equal(new[] { 2, 2 }, declared.Map(sheet));
    }

    /// <summary>A record per row, the blank row spelt out rather than skipped — what a blank band would read as.</summary>
    private static IProjection<string> BlankReadingRecord()
      => Record((TableRow row) => row.TextOrBlank(0) ?? "<blank>");

    /// <summary>Records on both sides of one interior blank, with no header row above them.</summary>
    private static ISpace HeaderlessInteriorBlank() => Mixed(new object?[,]
    {
      { "a", 1m },
      { null, null },
      { "b", 2m },
    });

    [Fact]
    public void StopEndsAtAnInteriorBlankWhetherOrNotThePlacementDeclaredTheExtent()
    {
      // The walk judges the band itself, so the terminator does not depend on the repeat having
      // bound its own block: under an extent somebody else declared — here the whole sheet, which
      // runs past the blank — the blank row ends the run rather than becoming a record.
      IReadOnlyList<string> underDeclaredExtent = Sized(WholeExtent())
        .Of(VerticalRepeat(BlankReadingRecord(), onBlank: BlankRowStrategy.Stop))
        .Map(HeaderlessInteriorBlank());

      Assert.Equal(new[] { "a" }, underDeclaredExtent);

      IReadOnlyList<string> selfBound = VerticalRepeat(BlankReadingRecord(), onBlank: BlankRowStrategy.Stop)
        .Map(HeaderlessInteriorBlank());

      Assert.Equal(new[] { "a" }, selfBound);
    }

    /// <summary>
    /// The same records on either side of an interior blank, plus a stray value far to the right of
    /// the block the repeat discovers for itself.
    /// </summary>
    private static ISpace SparseInteriorBlank() => Mixed(new object?[,]
    {
      { "a", 1m, null, null, null, null },
      { null, null, null, null, null, "X" },
      { "b", 2m, null, null, null, null },
    });

    [Fact]
    public void SelfBoundStopJudgesABlankRowAcrossTheFullExtentWidthRatherThanTheBlocksWidth()
    {
      // The rule as it stands, pinned because the two Stop paths measure blankness over different
      // widths: the self-bound path is judged by the discovered block's own row rule, which scans the
      // full extent width, so the stray value at F2 makes that row a row with a value and the run
      // reads straight through it. The declared-extent path judges the band across the width the walk
      // was handed, under which the same row is blank and ends the run — so the two paths are not
      // interchangeable on a sparse sheet.
      IReadOnlyList<string> selfBound = VerticalRepeat(BlankReadingRecord(), onBlank: BlankRowStrategy.Stop)
        .Map(SparseInteriorBlank());

      Assert.Equal(new[] { "a", "<blank>", "b" }, selfBound);
    }

    [Fact]
    public void SkipReadsPastAnInteriorBlankUnderADeclaredExtent()
    {
      // The same declared extent, the policy that runs to the edge: the blank is omitted and the
      // record past it is read.
      IReadOnlyList<string> read = Sized(WholeExtent())
        .Of(VerticalRepeat(BlankReadingRecord(), onBlank: BlankRowStrategy.Skip))
        .Map(HeaderlessInteriorBlank());

      Assert.Equal(new[] { "a", "b" }, read);
    }

    // --- 5. Bare repeat regression -----------------------------------------------------------------

    [Fact]
    public void ABareRepeatTreatsAnInteriorBlankAsAnOrdinaryItemAndReadsToTheEdge()
    {
      // With no onBlank the blank row is not a terminator: it becomes an ordinary item attempt, and
      // the repeat runs to the extent's edge exactly as before the terminator existed.
      var sheet = Mixed(new object?[,]
      {
        { "a" },
        { null },
        { "b" },
      });

      IReadOnlyList<string> read = VerticalRepeat(Record((TableRow row) => row.TextOrBlank(0) ?? "<blank>")).Map(sheet);

      Assert.Equal(new[] { "a", "<blank>", "b" }, read);
    }

    // --- 6. Construction guards --------------------------------------------------------------------

    [Fact]
    public void ASeparatorAndAnOnBlankPolicyTogetherAreRejectedAtConstruction()
    {
      var failure = Assert.Throws<ArgumentException>(
        () => VerticalRepeat(Record((TableRow row) => row.Index), separatedBy: BlankRows(), onBlank: BlankRowStrategy.Stop));

      Assert.Contains("separator", failure.Message);
      Assert.Contains("onBlank", failure.Message);
    }

    [Fact]
    public void HorizontalRepeatRejectsAnyOnBlankPolicy()
    {
      var failure = Assert.Throws<ArgumentException>(
        () => HorizontalRepeat(Record((TableRow row) => row.Index), onBlank: BlankRowStrategy.Stop));

      Assert.Contains("vertical", failure.Message);
      Assert.Contains("HorizontalRepeat", failure.Message);
    }

    [Fact]
    public void ABareHorizontalRepeatWithNoOnBlankStillWorks()
    {
      var grid = Grid(new[,] { { 1, 2, 3 } });

      IReadOnlyList<int> read = HorizontalRepeat(IntCell()).Map(grid);

      Assert.Equal(new[] { 1, 2, 3 }, read);
    }
  }
}
