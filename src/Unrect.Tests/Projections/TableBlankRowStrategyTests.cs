using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The <c>onBlank</c> blank-row strategy on the leaf <c>Table</c> — the five presets and the
  /// <c>blankRecord</c> escape hatch, per <c>docs/design/table-onblank-impl-spec.md §5</c> and the
  /// semantics in <c>docs/design/table-extent-and-blank-rows.md</c>.
  /// <para>
  /// A fully-blank body row (every cell <see cref="CellValue.IsBlank"/>) is treated by policy:
  /// <c>Stop</c> is self-bounding and ends the block at the blank (today's behaviour, unchanged);
  /// <c>Skip</c> omits the record and runs to a declared bound or the enclosing edge; <c>Fault</c>
  /// is a terminal error no tolerance boundary may absorb; <c>Tolerate</c> is <c>Skip</c> plus a
  /// nonterminal Info; and the <c>blankRecord</c> rung produces a record for the blank rather than
  /// omitting one.
  /// </para>
  /// <para>
  /// The additive-safety keystone is <see cref="TheDefaultIsStopAndReadsIdenticallyToOmittingItAltogether"/>:
  /// the whole change is gated on <c>default(BlankRowStrategy) == Stop</c> routing to the untouched
  /// code path, so a caller who never mentions <c>onBlank</c> reads byte-identically to before.
  /// </para>
  /// </summary>
  public class TableBlankRowStrategyTests
  {
    private sealed record Line(string Name, decimal Amount);

    private sealed record Named(string Text);

    /// <summary>What <c>Table&lt;T&gt;()</c> and the bind rung read a well-formed body into.</summary>
    private sealed record Entry(string Name, decimal Amount);

    /// <summary>
    /// A header over a body carrying an interior blank row and a trailing one, with real content
    /// <em>past</em> the interior blank — the shape every policy below is distinguished on.
    /// <code>
    ///   r0  Name    Amount     header
    ///   r1  Alpha   100        body index 0
    ///   r2  (blank)            body index 1 — interior blank
    ///   r3  Gamma   300        body index 2 — content past the blank
    ///   r4  (blank)            body index 3 — trailing blank
    /// </code>
    /// </summary>
    private static ISpace Gapped() => Mixed(new object?[,]
    {
      { "Name", "Amount" },
      { "Alpha", 100m },
      { null, null },
      { "Gamma", 300m },
      { null, null },
    });

    /// <summary>
    /// The same shape with a totals landmark below it and a note past the landmark, so a bounded
    /// policy has something to stop at and something to leave undescribed.
    /// <code>
    ///   r0  Name    Amount     header
    ///   r1  Alpha   100
    ///   r2  (blank)            interior blank
    ///   r3  Gamma   300
    ///   r4  Total   400        landmark
    ///   r5  note                past the landmark
    /// </code>
    /// </summary>
    private static ISpace GappedWithTotal() => Mixed(new object?[,]
    {
      { "Name", "Amount" },
      { "Alpha", 100m },
      { null, null },
      { "Gamma", 300m },
      { "Total", 400m },
      { "note", null },
    });

    private static IProjection<IReadOnlyList<Line>> Lines(BlankRowStrategy onBlank)
      => Table(r => new Line(r.Text("Name"), r.Decimal("Amount")), onBlank);

    // --- B. default(BlankRowStrategy) == Stop -------------------------------------------------------

    [Fact]
    public void TheStructsDefaultIsStop()
    {
      // The linchpin, stated on the value itself: every new overload takes onBlank = default, and
      // default must be Stop for the omit-it path to route to the untouched code.
      Assert.True(default(BlankRowStrategy).IsStop);
      Assert.True(BlankRowStrategy.Stop.IsStop);

      Assert.False(BlankRowStrategy.Skip.IsStop);
      Assert.False(BlankRowStrategy.Fault.IsStop);
      Assert.False(BlankRowStrategy.Tolerate.IsStop);
    }

    [Fact]
    public void TheDefaultIsStopAndReadsIdenticallyToOmittingItAltogether()
    {
      // The additive-safety keystone: over a sheet with an interior blank, a trailing blank, and
      // content past the interior blank — the case that would diverge if Stop had moved — the
      // no-onBlank spelling, onBlank: default, and onBlank: Stop denote the same reading at L3
      // (value, extent consumed and from where, diagnostics in order, and any failure's path).
      var control = Observe(Table(r => new Line(r.Text("Name"), r.Decimal("Amount"))), Gapped());

      AssertL3(control, Observe(Table(r => new Line(r.Text("Name"), r.Decimal("Amount")), onBlank: default), Gapped()));
      AssertL3(control, Observe(Lines(BlankRowStrategy.Stop), Gapped()));
    }

    // --- C. Stop -----------------------------------------------------------------------------------

    [Fact]
    public void StopEndsTheBlockAtTheFirstBlankRow()
    {
      // Self-bounding: the interior blank is the end, so the content past it is not read at all —
      // the behaviour that predates the knob.
      var records = Lines(BlankRowStrategy.Stop).Map(Gapped());

      Assert.Equal(new[] { new Line("Alpha", 100m) }, records);
    }

    // --- C. Skip -----------------------------------------------------------------------------------

    [Fact]
    public void SkipOmitsTheBlankRowAndKeepsReadingToTheEnclosingEdge()
    {
      // Non-self-bounding and no boundary declared: the block runs to the sheet's used rows, the
      // interior blank contributes no record, and the content past it is read.
      var records = Lines(BlankRowStrategy.Skip).Map(Gapped());

      Assert.Equal(new[] { new Line("Alpha", 100m), new Line("Gamma", 300m) }, records);
    }

    [Fact]
    public void SkipRunsToADeclaredUntilBoundaryAndConsumesItInFull()
    {
      // With a boundary, the run ends at the landmark; the interior blank is still skipped, and the
      // bound is consumed in full so a following sibling lands ON the landmark.
      var report = VerticalFlow(v =>
      {
        var lines = v.Next(Until(RowContaining("Total")).Of(Lines(BlankRowStrategy.Skip)));
        var total = v.Next(HorizontalFlow(h => new Line(h.Next(Text()), h.Next(Decimal()))));

        return (Lines: lines, Total: total);
      });

      var read = report.Map(GappedWithTotal());

      Assert.Equal(new[] { new Line("Alpha", 100m), new Line("Gamma", 300m) }, read.Lines);
      Assert.Equal(new Line("Total", 400m), read.Total);
    }

    // --- C. Fault ----------------------------------------------------------------------------------

    [Fact]
    public void FaultFailsTerminallyOnAnInteriorBlankRow()
    {
      var failure = Assert.Throws<ProjectionException>(() => Lines(BlankRowStrategy.Fault).Map(Gapped()));

      Assert.True(failure.IsFault);
      Assert.Contains("is blank", failure.Message);
      Assert.Contains("A3", failure.Message);         // the interior blank row, cited in the message
    }

    [Fact]
    public void AFaultIsNotAbsorbedByOptional()
    {
      // The crucial property: a Fault is malformed data, not an absent section, so a tolerance
      // boundary must let it through. .Optional() would turn an absorbable failure into null.
      var failure = Assert.Throws<ProjectionException>(() => Lines(BlankRowStrategy.Fault).Optional().Map(Gapped()));

      Assert.True(failure.IsFault);
    }

    [Fact]
    public void AndNotByElse()
    {
      var fallback = (IReadOnlyList<Line>)Array.Empty<Line>();

      var failure = Assert.Throws<ProjectionException>(() => Lines(BlankRowStrategy.Fault).Else(fallback).Map(Gapped()));

      Assert.True(failure.IsFault);
    }

    // --- C. Tolerate -------------------------------------------------------------------------------

    [Fact]
    public void TolerateSkipsTheBlankAndRecordsANonterminalInfoForEachOne()
    {
      var read = Lines(BlankRowStrategy.Tolerate).MapWithDiagnostics(Gapped());

      // Parsing continued, exactly as Skip: the two blanks are omitted, the content past them read.
      Assert.Equal(new[] { new Line("Alpha", 100m), new Line("Gamma", 300m) }, read.Value);

      // ...and each blank left a nonterminal Info, its message citing the blank row's A1 (the
      // diagnostic's own Location is the table's origin; the row is named in the sentence).
      var infos = read.Diagnostics.Where(d => d.Message.Contains("is blank")).ToList();

      Assert.Equal(2, infos.Count);
      Assert.All(infos, info => Assert.Equal(DiagnosticSeverity.Info, info.Severity));
      Assert.Contains(infos, info => info.Message.Contains("A3"));
      Assert.Contains(infos, info => info.Message.Contains("A5"));

      // Nothing was elevated past Info — a tolerated blank is a remark, not a warning or a failure.
      Assert.DoesNotContain(read.Diagnostics, d => d.Severity != DiagnosticSeverity.Info);
    }

    // --- C. blankRecord (the Project preset) -------------------------------------------------------

    [Fact]
    public void BlankRecordProducesARecordForABlankRowKeyedByItsOccurrenceIndex()
    {
      // Where Skip omits, blankRecord includes — and the row a blank offers is usually just its
      // Index, the step-2 occurrence ordinal counted over the body from zero. So Alpha is index 0,
      // the interior blank index 1, Gamma index 2, and the trailing blank index 3.
      var records = Table(r => new Named(r.Text(0)), blankRecord: b => new Named($"blank#{b.Index}")).Map(Gapped());

      Assert.Equal(
        new[]
        {
          new Named("Alpha"),
          new Named("blank#1"),
          new Named("Gamma"),
          new Named("blank#3"),
        },
        records);
    }

    [Fact]
    public void TheExplicitHeaderRowsBlankRecordRungReadsTheSame()
    {
      // Item 5 delegates to item 6 with headerRows: 1, so the two spellings agree.
      var oneArg = Table(r => new Named(r.Text(0)), blankRecord: b => new Named($"blank#{b.Index}")).Map(Gapped());
      var twoArg = Table(1, r => new Named(r.Text(0)), blankRecord: b => new Named($"blank#{b.Index}")).Map(Gapped());

      Assert.Equal(oneArg, twoArg);
    }

    // --- C. The reflection rungs carry the knob too ------------------------------------------------

    [Fact]
    public void TheTypedBindRungSkipsInteriorBlanks()
    {
      // Table<T>(onBlank:) threads the policy through the reflection binder unchanged; a blank row
      // is skipped rather than ending the block.
      var records = Table<Entry>(BlankRowStrategy.Skip).Map(Gapped());

      Assert.Equal(new[] { new Entry("Alpha", 100m), new Entry("Gamma", 300m) }, records);
    }

    [Fact]
    public void AndTheDefaultReflectionRungStillStops()
    {
      // The control for the rung above: without the knob, the reflected table is self-bounding.
      Assert.Equal(new[] { new Entry("Alpha", 100m) }, Table<Entry>().Map(Gapped()));
    }

    // --- D. Laziness — the run-to-edge walk stays row-at-a-time -------------------------------------
    //
    // The cross-door L3 equality for a Skip run-to-edge table is pinned in
    // Streaming/CrossDoorDenotationTests ("a bounded skip table over interior blanks"). Here is the
    // other half of D: that run-to-edge does not measure the sheet up front but peeks each row as
    // the walk advances — the CountingSpace pattern LazyForcingTests uses, which is the in-memory
    // proxy for the streaming door's deferral (both ride the same IIncrementalAreaStrategy).

    /// <summary>
    /// A header over ten body rows with one interior blank, wrapped in a counting space. The list
    /// returned records how many rows of the sheet had been touched at the moment each non-blank
    /// body row was projected.
    /// </summary>
    private static (IReadOnlyList<int> RowsTouchedAsEachProjects, int TotalTouched) SkipWalk()
    {
      var values = new object?[11, 2];

      values[0, 0] = "Name";
      values[0, 1] = "Amount";

      for (var row = 1; row <= 10; row++)
      {
        if (row == 5)
          continue;                       // the interior blank the walk must peek and skip

        values[row, 0] = $"row {row}";
        values[row, 1] = row * 10m;
      }

      var counter = new CountingSpace(Mixed(values));
      var observations = new List<int>();

      Table(r => { observations.Add(counter.RowsTouched); return r.Index; }, onBlank: BlankRowStrategy.Skip)
        .Apply(counter);

      return (observations, counter.RowsTouched);
    }

    [Fact]
    public void SkipRunToEdgePeeksOneRowAtATimeRatherThanMeasuringUpFront()
    {
      var (observations, total) = SkipWalk();

      // Nine records — ten body rows less the one blank — each projected in step with the walk.
      Assert.Equal(9, observations.Count);

      // The first record projects having read only its own row and the header: two, not the eleven
      // an up-front measurement would have forced. This is the row-at-a-time claim.
      Assert.Equal(2, observations[0]);

      // The walk advances monotonically, and by the last record the whole sheet has been peeked.
      Assert.Equal(observations.OrderBy(count => count).ToList(), observations);
      Assert.Equal(11, observations[observations.Count - 1]);

      // The bound is still consumed in full by the time Apply returns — laziness changes what the
      // projection cost, never what the declaration consumed. (Run-to-edge is AllRows, a free
      // dimension query, so the extent itself never scans; what streams is the row WALK, which a
      // materialise-all implementation would have read to eleven before projecting anything.)
      Assert.Equal(11, total);
    }
  }
}
