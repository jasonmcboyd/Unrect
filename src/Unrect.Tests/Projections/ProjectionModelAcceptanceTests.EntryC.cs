using System;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Tests.Observations;

// THE SHIPPING SHAPE, WRITTEN AS IT SHIPS. This is a declaration file in the Entry C canon: one
// space named once, at the top, and not one prefix below it. `Text()`, `Table<T>()`, `Below(...)`
// and `Heading(...)` all come from here, closed over ISpace, and the file is the scope.
//
// It is therefore the exact opposite of ProjectionBuildersParityTests, whose header explains why
// THAT file must hold the vocabulary at arm's length behind a `using B = ...` alias: a parity suite
// has to name both spellings at once, and `Projection.Text()` and `ProjectionBuilders<ISpace>.Text()`
// have identical signatures, so a file importing both statically could not invoke either. Here there
// is nothing to compare against in-file — the pre-campaign spellings live in
// ProjectionModelAcceptanceTests.cs, which carries its own `using static Unrect.Projections.Projection`
// and is untouched — so this half can be written the way a user writes one. Usings are per-file, and
// the two halves of the partial class prove it.
//
// The type argument is spelled in FULL, and must be: a using alias or a `using static` is resolved
// as if the other usings were not there, so `ProjectionBuilders<ISpace>` would not bind even with
// `using Unrect.Core;` four lines above.
using static Unrect.Projections.ProjectionBuilders<Unrect.Core.ISpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// Phase 4 of the placement renovation, on the campaign's own acceptance corpus: each of the four
  /// whole declarations gains an Entry C twin, spelled in the new canon — the closed vocabulary with
  /// no prefix, the placement pipeline's entries instead of the postfix modifiers, <c>Heading</c>
  /// instead of <c>.Under(Caption(...))</c>, and <c>.Of</c> where the subject was hoisted — and the
  /// twin is asserted to be the SAME DECLARATION as the original, not merely to read the same value.
  /// <para>
  /// <b>Both live.</b> Nothing in <see cref="ProjectionModelAcceptanceTests"/> moved: the four
  /// declarations there are byte-identical, still applied, still asserted. This file is the additive
  /// half of the phase, and the two halves are one partial class so that a twin compares against the
  /// REAL original rather than against a copy of it — a copy would drift, and a drifted differential
  /// passes forever.
  /// </para>
  /// <para>
  /// <b>What "same declaration" means here.</b> L3 through the <see cref="Observations"/> harness:
  /// the value, the offset the placement resolved to, the extent consumed, the advance a sibling
  /// steps past, the diagnostics in order, and — where a reading fails — the failure's sentence with
  /// its path and its subject. That last part is why every twin below keeps the originals'
  /// identifiers (<c>allocation</c>, <c>allocations</c>, <c>investorBlock</c>) and inlines what the
  /// original inlined: a child's path segment is the identifier it was written as, so hoisting a
  /// pipeline into a well-named local would change the path and the twin would no longer be the same
  /// declaration at the level a user reads.
  /// </para>
  /// <para>
  /// <b>Why the results are compared as they are read.</b> These declarations all produce a record
  /// whose fields include a list, which used to be invisible to the value facet: a record's
  /// compiler-written <c>ToString</c> prints each member through that member's own, so the rows
  /// printed as <c>System.Collections.Generic.List`1[…]</c> and a twin whose columns had moved
  /// compared EQUAL. That was found by perturbation rather than by reading — a twin built with
  /// <c>Right(5)</c> in place of <c>Right(6)</c> passed L3 — and it was fixed where it belonged, in
  /// <see cref="Observations"/>, which now spells a record out from its own properties. The
  /// per-type describers this file carried as a workaround are gone with it; the perturbation was
  /// re-run against the central fix to confirm the pins are still the ones that catch it.
  /// </para>
  /// <para>
  /// <b>The one place the pipeline earns nothing</b> is scenario 1, and that is worth a pin rather
  /// than a footnote: a declaration with no placement in it has nothing to respell, so its twin
  /// differs only in where the words came from. The pipeline is for declarations that say where
  /// something sits; it is not a tax on the ones that do not.
  /// </para>
  /// <para>
  /// The scoped half — the audited ledger, whose declaration demands a capability — is in
  /// <c>ProjectionModelAcceptanceTests.EntryC.Spreadsheets.cs</c>, because a file closes the
  /// vocabulary over ONE space and that declaration's space is not this one's. That the split is
  /// forced is the file-is-the-scope teaching, demonstrated rather than described.
  /// </para>
  /// </summary>
  public partial class ProjectionModelAcceptanceTests
  {
    // =============================================================================================
    // 1. Scenario 1 — nothing to place, so nothing to respell
    // =============================================================================================

    /// <summary>
    /// The allocation report in the closed vocabulary. Every word of the body is the same word;
    /// what changed is the import at the top of the file and, at the hoisted-helper site, the return
    /// type — <c>IProjection&lt;ISpace, Report&gt;</c> rather than <c>IProjection&lt;Report&gt;</c>,
    /// because the closed class's layouts are closed over the space the file named. The two are one
    /// type up to the derivation that makes the plain form the base, which is the annotation tax the
    /// campaign already recorded and priced.
    /// </summary>
    private static IProjection<ISpace, Report> AllocationReportInTheClosedVocabulary()
    {
      var title = Text();
      var rows = Table<Allocation>();

      return VerticalFlow(v => new Report(
        Title: v.Next(title),
        Rows: v.Next(rows)));
    }

    [Fact]
    public void TheClosedVocabularysAllocationReportIsTheSameDeclaration()
    {
      // No entry, no stage, no Heading — because the declaration places nothing. The finding this
      // pin exists to state: Entry C costs a declaration with no placement exactly one line, at the
      // top of the file, and changes nothing else about it.
      AssertL3(
        Read(AllocationReport(), PlainAllocations()),
        Read(AllocationReportInTheClosedVocabulary(), PlainAllocations()));
    }

    [Fact]
    public void AndTheClosedVocabularysTwinAlsoReadsASpaceThatOffersMore()
    {
      // Scenario 1's second half survives the respelling too: the twin demands ISpace and the sheet
      // offers formulas, and variance does the work with nothing written about it.
      AssertL3(
        Read(AllocationReport(), CapableAllocations()),
        Read(AllocationReportInTheClosedVocabulary(), CapableAllocations()));

      var read = ((IProjection<Report>)AllocationReportInTheClosedVocabulary()).Map(CapableAllocations());

      Assert.Equal("Buying Power Allocation", read.Title);
      Assert.Equal(
        new[] { new Allocation("A-1", "SPY", 0.25m), new Allocation("A-2", "QQQ", 0.75m) },
        read.Rows);
    }

    // =============================================================================================
    // 2. The work-Claude parser, with its placement said before its subject
    // =============================================================================================

    /// <summary>
    /// The buying-power parser through the pipeline. Two anchored sections and three column offsets,
    /// each now written position-first — <c>Below(mark).Sized(...).Table(...)</c> reads as the sheet
    /// reads, top to bottom, where the postfix spelling puts the subject first and the geography
    /// last.
    /// <para>
    /// <c>.Of</c> appears twice, and both times for the reason it exists: the tolerant leaf
    /// <c>Decimal().OrBlank()</c> is not a terminal and never will be — <c>OrBlank</c> is written
    /// against the plain projection form, so the leaf comes back plain and the postfix half reaches
    /// it, exactly as the parity suite's boundary pin says. Declare it, then place it.
    /// </para>
    /// </summary>
    private static IProjection<ISpace, BuyingPowerAllocation> BuyingPowerParserThroughThePipeline()
    {
      var allocation = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Right(1).Text()),
        Primary: o.Next(Right(6).Of(Decimal().OrBlank())),
        Fep: o.Next(Right(9).Of(Decimal().OrBlank()))));

      var allocations = Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue())
        .Table(headerRows: 0, eachRow: allocation);

      return VerticalFlow(v => new BuyingPowerAllocation(
        Title: v.Next(Text()),
        Allocations: v.Next(allocations),
        Total: v.Next(On(RowContaining("TOTAL")).Right(6).Decimal())));
    }

    [Fact]
    public void TheBuyingPowerParserThroughThePipelineIsTheSameDeclaration()
    {
      var space = BuyingPower();

      AssertL3(
        Read(BuyingPowerParser(), space),
        Read(BuyingPowerParserThroughThePipeline(), space));
    }

    [Fact]
    public void AndTheTwinStillReadsTheExportItWasWrittenFor()
    {
      // Non-vacuity, and the record the whole declaration exists to be allowed to describe: a fund
      // code with two absences is still a record and not a failure, per field, through the pipeline.
      var read = ((IProjection<BuyingPowerAllocation>)BuyingPowerParserThroughThePipeline()).Map(BuyingPower());

      Assert.Equal("PCTCAL2 BUYING POWER", read.Title);
      Assert.Equal(2231.25m, read.Total);
      Assert.Equal(
        new[]
        {
          new BuyingPowerRow("ABC", 1250.75m, 300.00m),
          new BuyingPowerRow("DEF", 980.50m, null),
          new BuyingPowerRow("GHI", null, null),
        },
        read.Allocations);
    }

    [Fact]
    public void AndTheTwinReadsTheSheetForwardOnlyToo()
    {
      // The cost claim, re-measured rather than inherited. L3 compares what each spelling consumed
      // and where, but not the ORDER the rows were touched in — and the order is the only thing a
      // windowed reader cares about. A stage records a modifier and replays it, so a pipeline cannot
      // introduce a backwards reach; this is that argument made into an instrument reading.
      var watched = new WatermarkSpace(BuyingPower());

      var read = ((IProjection<BuyingPowerAllocation>)BuyingPowerParserThroughThePipeline()).Map(watched);

      Assert.Equal(3, read.Allocations.Count);
      Assert.Equal(7, watched.HighWaterMark);
      Assert.Equal(0, watched.BackwardReach);
    }

    // =============================================================================================
    // 3. The IRR report — the fossil pair dissolved
    // =============================================================================================

    /// <summary>
    /// The IRR report with <c>Heading</c> where <c>.Under(Caption(...))</c> was. This is the phase's
    /// sharpest respelling and the renovation's whole argument in four lines: the original mints two
    /// <c>Caption</c> leaves whose values nobody reads, purely so that the rows above the section are
    /// consumed — a value declared to be discarded. <c>Heading</c> says what those rows are (the
    /// thing the section announces itself by), asserts the text, consumes the row, and contributes no
    /// node. The captions are still minted; they are minted inside the library now, which is the
    /// whole of what the word moves.
    /// <para>
    /// The bound leads, because a bound is geometry and geometry comes before the headings in the
    /// canonical stage order — and it is the same declaration as the postfix bound the original
    /// writes, which <see cref="PlacementPipelineLawTests"/> pins as a law in its own right.
    /// </para>
    /// </summary>
    private static IProjection<ISpace, IrrReport> IrrReportThroughHeadings()
    {
      var investorBlock = Table<CashFlow>();

      // Declared once, placed twice — and the placements are what changed, not the series.
      var series = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      const string Inception = "Cash Flows using inception date";

      return VerticalFlow(v => new IrrReport(
        Header: v.Next(VerticalFlow(h => new IrrHeader(
          Title: h.Next(Text()),
          Fund: h.Next(Text()),
          AsOf: h.Next(Date()),
          Id: h.Next(Text()))).Named("report header")),
        Summary: v.Next(Table<InvestorSummary>().Named("summary")),
        ByTransferDate: v.Next(Until(RowContaining(Inception))
          .Heading("IRR Details")
          .Heading("Cash Flows Using Transfer Date")
          .Of(series)),
        ByInception: v.Next(Heading(Inception).Of(series))));
    }

    [Fact]
    public void TheIrrReportThroughHeadingsIsTheSameDeclaration()
    {
      // Same sheet, same 45 rows, same nothing left over — and, because this is L3, the same two
      // caption paths under the same two 'Under' segments, which is what says the headings really
      // are the leaves the original wrote by hand.
      var space = Irr();

      AssertL3(
        Read(IrrReportDeclaration(), space),
        Read(IrrReportThroughHeadings(), space));
    }

    [Fact]
    public void AndTheTwinBindsEveryColumnWithNothingDeclared()
    {
      // Non-vacuity for the pin above: the whole document really is read, both series really are the
      // one hoisted series placed twice, and the sheet really is described end to end.
      var result = ((IProjection<IrrReport>)IrrReportThroughHeadings()).MapWithDiagnostics(Irr());
      var report = result.Value;

      Assert.Equal("Investor IRR Report", report.Header.Title);
      Assert.Equal(new DateTime(2026, 6, 30), report.Header.AsOf);

      Assert.Equal(new[] { 3, 2, 4 }, report.ByTransferDate.Select(block => block.Count));
      Assert.Equal(new[] { 3, 2, 4 }, report.ByInception.Select(block => block.Count));

      Assert.Equal(
        new CashFlow("Alpha Capital LLC", new DateTime(2024, 1, 15), "Contribution", 0m),
        report.ByTransferDate[0][0]);

      Assert.Equal(
        new CashFlow("Cedar Holdings", new DateTime(2025, 8, 1), "Distribution", 0.104m),
        report.ByInception[2][3]);

      Assert.Empty(result.Diagnostics);
    }

    // --- The machinery -------------------------------------------------------------------------------

    /// <summary>
    /// One side of a comparison: the declaration read over the space. The harness spells the result
    /// out to its leaves, so nothing has to be described by hand here — see the class remarks for
    /// what that replaced.
    /// </summary>
    private static Observation Read<T>(IProjection<T> declaration, ISpace space)
      => Observe(declaration, space);

    /// <summary>
    /// The same, for a declaration written through the closed vocabulary. The cast is the one the
    /// typed layer itself makes: the demand lives only in the static type and every projection this
    /// library builds implements <c>IProjection&lt;T&gt;</c>, so forgetting a demand in order to READ
    /// a declaration is exactly what <c>ProjectionExtensions.Plain</c> does inside the library.
    /// Written once here so no twin above carries a cast in the middle of an assertion.
    /// </summary>
    private static Observation Read<T>(IProjection<ISpace, T> declaration, ISpace space)
      => Read((IProjection<T>)declaration, space);
  }
}
