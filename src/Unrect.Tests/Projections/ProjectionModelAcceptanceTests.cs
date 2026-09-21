using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjections;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The projection-model campaign's acceptance: the typed-spaces gauntlet's evidence, promoted out
  /// of the spike and run by the suite. What is here is what no phase suite says on its own — whole
  /// declarations, run end to end, in the vocabulary the campaign finished with.
  /// <para>
  /// The phase suites already pin the mechanisms one at a time, and this file deliberately does not
  /// repeat them: <see cref="OrBlankTests"/> has the per-field tolerance,
  /// <see cref="RowProjectionTableTests"/> the <c>eachRow</c> slot over this same sparse export,
  /// <see cref="BoundRowTableTests"/> the bind and the reordered-columns reading,
  /// <see cref="Unrect.Tests.Spreadsheets.FormulaCapabilityTests"/> the capability end to end over a real file,
  /// and <see cref="Unrect.Tests.Streaming.MapWorkbookTests"/> the streaming sugar. What those
  /// cannot say is whether the parts still add up to a parser somebody would write, which is the
  /// only question left at the end of a campaign.
  /// </para>
  /// <para>
  /// Four declarations carry it. Scenario 1 — a plain, pre-campaign declaration, spelled verbatim
  /// and applied to a space that offers more (§7's first criterion, and the one that fails the whole
  /// design if it moves). The work-Claude buying-power export, read whole rather than table-first.
  /// The IRR report in the final vocabulary, one series declared once and placed twice. And the
  /// audited ledger through the scoped entry, over the committed formula fixture.
  /// </para>
  /// <para>
  /// The refusals — the fourteen spellings that must NOT compile — cannot live in a test project at
  /// all, because a test project must compile. They remain re-runnable, with their verbatim compiler
  /// messages, in <c>spike/TypedSpacesGauntlet/MustNotCompile.cs</c>. What IS assertable about them is the
  /// variance that produces them, and that is below.
  /// </para>
  /// </summary>
  public partial class ProjectionModelAcceptanceTests
  {
    private static string TestData(string file) => Path.Combine(AppContext.BaseDirectory, "TestData", file);

    // =============================================================================================
    // 1. Scenario 1 — the plain declaration, unchanged
    // =============================================================================================
    //
    // The experiment's first judgment criterion: "scenario 1 unchanged, or fail". A declaration
    // written before any of this existed must still be spelled with no type argument the vocabulary
    // did not always have, no witness, no scope, and no mention of the word space — and it must run
    // on a space that offers MORE without one character changing, because that is what the whole
    // design rests on.

    /// <summary>
    /// The little allocation sheet, in both flavours. Written once as values so the capable twin
    /// cannot drift from the plain one: the two spaces differ in what they can be asked, and in
    /// nothing else.
    /// <code>
    /// r0  Buying Power Allocation
    /// r1  (blank)
    /// r2  Account | Symbol | Weight
    /// r3  A-1     | SPY    | 0.25     &lt;- D4/$D$8
    /// r4  A-2     | QQQ    | 0.75     &lt;- D5/$D$8
    /// r5  (blank)
    /// r6  Total   |        | 1.00     &lt;- SUM(C4:C5)
    /// </code>
    /// </summary>
    private static Cell[,] AllocationValues()
    {
      var cells = new object?[,]
      {
        { "Buying Power Allocation", null, null },
        { null, null, null },
        { "Account", "Symbol", "Weight" },
        { "A-1", "SPY", 0.25m },
        { "A-2", "QQQ", 0.75m },
        { null, null, null },
        { "Total", null, 1.00m },
      };

      var values = new Cell[cells.GetLength(0), cells.GetLength(1)];

      for (var row = 0; row < cells.GetLength(0); row++)
        for (var column = 0; column < cells.GetLength(1); column++)
          values[row, column] = Adapt(cells[row, column]);

      return values;
    }

    private static ICellSpace PlainAllocations() => SheetGrid.Of(AllocationValues());

    private static ISpreadsheetSpace CapableAllocations()
    {
      var formulas = new string?[7, 3];

      formulas[3, 2] = "D4/$D$8";
      formulas[4, 2] = "D5/$D$8";
      formulas[6, 2] = "SUM(C4:C5)";

      return new FormulaGridSpace(AllocationValues(), formulas);
    }

    /// <summary>
    /// The declaration under test, hoisted so both tests read the same one. Every character of it
    /// predates the campaign, and its type is what it always was.
    /// </summary>
    private static IProjectionDefinition<ICellSpace, Report> AllocationReport()
    {
      var title = Text();
      var rows = Table<Allocation>();

      return VerticalFlow(v => new Report(
        Title: v.Next(title),
        Rows: v.Next(rows)));
    }

    [Fact]
    public void APlainDeclarationIsStillSpelledWithNothingAboutSpacesInIt()
    {
      // The local's annotation is the assertion: a declaration that reads nothing exotic is an
      // IProjectionDefinition<ICellSpace, T>, which is what every hoisted helper in the corpus and every test in this
      // suite says. If the campaign had cost this, it would have cost everything.
      IProjectionDefinition<ICellSpace, Report> report = AllocationReport();

      var read = report.Map(PlainAllocations());

      Assert.Equal("Buying Power Allocation", read.Title);
      Assert.Equal(
        new[] { new Allocation("A-1", "SPY", 0.25m), new Allocation("A-2", "QQQ", 0.75m) },
        read.Rows);
    }

    [Fact]
    public void AndTheSameDeclarationReadsASpaceThatOffersMore()
    {
      // Nothing is written differently — not a cast, not an overload, not a witness. A declaration
      // names the space it is written over, and a space that offers more IS one of those: the
      // conversion happens at the argument, where Map takes the declaration's own space type.
      var read = AllocationReport().Map(CapableAllocations());

      Assert.Equal("Buying Power Allocation", read.Title);
      Assert.Equal(
        new[] { new Allocation("A-1", "SPY", 0.25m), new Allocation("A-2", "QQQ", 0.75m) },
        read.Rows);

      // ...and it lands in the same place, which is the half a value comparison would not catch.
      Assert.Equal(
        AllocationReport().Apply(PlainAllocations()).Consumed,
        AllocationReport().Apply(CapableAllocations()).Consumed);
    }

    // =============================================================================================
    // 2. A declaration names its space, and a shared helper is generic in it
    // =============================================================================================
    //
    // Scenario 8 (one modifier definition, every space), scenario 3 (a lift raising what it
    // touches), and the static half of scenario 5 — the conversions that do and do not exist.
    //
    // WHAT CHANGED, AND WHY IT IS HERE RATHER THAN DELETED. This section used to be about VARIANCE:
    // IProjectionDefinition was contravariant in its space, so a plain declaration was usable wherever a
    // demanding one was wanted and the refusals fell out of the direction the conversion did not
    // run in. A projection is handed a region of its space now — a Plane<TSpace>, an invariant
    // struct — so `in TSpace` and a real Project are mutually exclusive, and the interface is
    // invariant. The refusals survive; what produces them moved. A file names one space, a
    // declaration is typed by it, and the two things that used to need variance are now (a) the
    // argument conversion at Map, which is enough for "reads a space that offers more", and (b) a
    // helper generic in TSpace, which is enough for one definition shared across spaces.

    [Fact]
    public void OneModifierChainIsTypedByWhateverSpaceItIsWrittenOver()
    {
      // One chain of modifiers, written once in the library and typed by the file that spells it.
      // The annotated local is the assertion; that it reads its sheet is the proof the type is not
      // the only thing that survived. (The demanding twin of this very chain is in
      // ProjectionModelAcceptanceTests.EntryC.Spreadsheets.cs, because its space is not this file's.)
      IProjectionDefinition<ICellSpace, decimal> plain = On(RowContaining("Total")).Right(2).Of(Decimal()
        .Named("total"));

      Assert.Equal(1.00m, plain.Map(PlainAllocations()));

      // The name survives the chain too — a placement stage hands back the projection's own type, so
      // it cannot quietly rewrap it into something with a different identity. (The transparent
      // wrappers — Padded, Select, Until — are a different rule, pinned where each of them lives.)
      Assert.Equal("total", plain.Name);
    }

    [Fact]
    public void ALiftRaisesTheProjectionItTouches()
    {
      // Scenario 3: the matcher's demand and the receiver's are unified by the file's space. The
      // matcher is contravariant in what it demands — it is a landmark and no plane appears in its
      // signature, so `in TSpace` survives there — which is what lets a matcher published for
      // IFormulaSpace stand in a pipeline closed over the bundle.
      IProjectionDefinition<ISpreadsheetSpace, string> firstFormulaRow =
        ProjectionBuilders<ISpreadsheetSpace>.On(SpreadsheetProjections.RowWithFormula())
          .Of(ProjectionBuilders<ISpreadsheetSpace>.Row(cells => cells[0].Text()));

      Assert.Equal("A-1", firstFormulaRow.Map(CapableAllocations()));
    }

    [Fact]
    public void ADeclarationNamesExactlyOneSpace()
    {
      // The reason every refusal in the ledger is still a refusal, said the way it is now true: a
      // declaration is INVARIANT in its space, so neither conversion exists and "applied to the
      // wrong backend" is a compile error in both directions.
      var plain = typeof(IProjectionDefinition<ICellSpace, Report>);
      var demanding = typeof(IProjectionDefinition<ISpreadsheetSpace, Report>);

      Assert.False(demanding.IsAssignableFrom(plain), "a declaration converts to no other space");
      Assert.False(plain.IsAssignableFrom(demanding), "a demanding declaration must not run on any space");

      // ...and what makes the invariance liveable, which is the half worth pinning beside it: the
      // conversion that a reader actually needs is at the ARGUMENT, where Map takes the declaration's
      // own space type and a space offering more is one of those. Scenario 1's second half is this
      // sentence, run.
      Assert.Equal("Buying Power Allocation", AllocationReport().Map(CapableAllocations()).Title);
    }

    /// <summary>A hoisted plain helper: exactly what it was, which is the common case and the point.</summary>
    private static IProjectionDefinition<ICellSpace, Allocation> AllocationRow()
      => HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

    /// <summary>A hoisted demanding helper: its space says what it requires, and it says so once.</summary>
    private static IProjectionDefinition<ISpreadsheetSpace, SourcedAllocation> SourcedRow()
      => ProjectionBuilders<ISpreadsheetSpace>.Overlay(o => new SourcedAllocation(
        Account: o.Next(ProjectionBuilders<ISpreadsheetSpace>.Text()),
        Formula: o.Next(ProjectionBuilders<ISpreadsheetSpace>.Right(2)
          .Of(SpreadsheetProjections.Formula<ISpreadsheetSpace>()))));

    /// <summary>A helper generic in whatever space its caller names — the shape that replaced variance.</summary>
    private static IProjectionDefinition<TSpace, IReadOnlyList<T>> Sections<TSpace, T>(IProjectionDefinition<TSpace, T> item)
      where TSpace : class, ICellSpace
      => ProjectionBuilders<TSpace>.VerticalRepeat(item, separatedBy: BlankRows());

    [Fact]
    public void AHoistedHelperSaysWhatItRequiresInItsReturnType()
    {
      // Scenario 7: what tooltips show over a library of hoisted declarations. The plain one names
      // the plain space; the demanding one names a space that carries formulas, and that IS the
      // requirement; and a generic helper composes either, keeping whatever it was handed.
      IProjectionDefinition<ICellSpace, IReadOnlyList<Allocation>> plainSections = Sections(AllocationRow());
      IProjectionDefinition<ISpreadsheetSpace, IReadOnlyList<SourcedAllocation>> demandingSections = Sections(SourcedRow());

      // Both read the same capable sheet — the plain one because Map's argument converts, the
      // demanding one because the sheet answers what it asks.
      var rows = On(RowContaining("A-1")).Of(AllocationRow()).Map(CapableAllocations());

      Assert.Equal(new Allocation("A-1", "SPY", 0.25m), rows);
      Assert.Equal("VerticalRepeat", plainSections.Description);
      Assert.Equal("VerticalRepeat", demandingSections.Description);

      var sourced = ProjectionBuilders<ISpreadsheetSpace>.On(ProjectionBuilders<ISpreadsheetSpace>.RowContaining("A-1"))
        .Of(SourcedRow())
        .Map(CapableAllocations());

      Assert.Equal(new SourcedAllocation("A-1", "D4/$D$8"), sourced);
    }

    // =============================================================================================
    // 3. The work-Claude parser, read whole
    // =============================================================================================
    //
    // The export the row-projection slot was designed for, and the parser that started the
    // conversation: no header a record can be bound by, values in columns 1, 6 and 9, a record that
    // carries a fund code and two absences, and a total line underneath. RowProjectionTableTests
    // pins the TABLE over this shape; what this pins is the document — title, table and total in one
    // declaration, which is what the original parser was and what it could not say before.

    /// <summary>
    /// The PCTCAL2-shaped export.
    /// <code>
    ///      c0        c1      c6         c9
    /// r0   PCTCAL2 BUYING POWER
    /// r1   (blank)
    /// r2   ACCOUNT   FUND    PRIMARY    FEP
    /// r3             ABC     1250.75    300.00
    /// r4             DEF      980.50
    /// r5             GHI                          &lt;- a fund code and nothing else
    /// r6   (blank)
    /// r7   TOTAL             2231.25
    /// </code>
    /// </summary>
    private static ICellSpace BuyingPower()
    {
      var cells = new object?[8, 11];

      cells[0, 0] = "PCTCAL2 BUYING POWER";

      cells[2, 0] = "ACCOUNT";
      cells[2, 1] = "FUND";
      cells[2, 6] = "PRIMARY";
      cells[2, 9] = "FEP";

      cells[3, 1] = "ABC";
      cells[3, 6] = 1250.75m;
      cells[3, 9] = 300.00m;

      cells[4, 1] = "DEF";
      cells[4, 6] = 980.50m;

      cells[5, 1] = "GHI";

      cells[7, 0] = "TOTAL";
      cells[7, 6] = 2231.25m;

      return Mixed(cells);
    }

    /// <summary>
    /// The parser in its final form. Everything the campaign added is in these eight lines and
    /// nothing else is: a record projected by a <em>projection</em> rather than by a lambda, an
    /// overlay saying which column each field is, <c>OrBlank</c> saying which of them a record may
    /// omit, and the table's own extent discovered from the content below the caption row.
    /// </summary>
    private static IProjectionDefinition<ICellSpace, BuyingPowerAllocation> BuyingPowerParser()
    {
      var allocation = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Right(1).Of(Text())),
        Primary: o.Next(Right(6).Of(Decimal().OrBlank())),
        Fep: o.Next(Right(9).Of(Decimal().OrBlank()))));

      var allocations = Below(RowContaining("ACCOUNT")).Sized(RowsWhileAnyValue()).Of(Table(headerRows: 0, eachRow: allocation));

      return VerticalFlow(v => new BuyingPowerAllocation(
        Title: v.Next(Text()),
        Allocations: v.Next(allocations),
        Total: v.Next(On(RowContaining("TOTAL")).Right(6).Of(Decimal()))));
    }

    [Fact]
    public void TheBuyingPowerParserReadsTheExportItWasWrittenFor()
    {
      var read = BuyingPowerParser().Map(BuyingPower());

      Assert.Equal("PCTCAL2 BUYING POWER", read.Title);
      Assert.Equal(2231.25m, read.Total);

      // The last record is the one the declaration exists to be allowed to describe: a fund code and
      // two absences, which is a record and not a failure, because the declaration said so per field.
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
    public void AndDescribesEveryRowOfTheSheetWhileDoingIt()
    {
      // The whole-document claim, which the table alone cannot make: read table-first, the title and
      // the total line arrive as the unconsumed-space Info. Read as a document, there is nothing
      // left over to report — the empty diagnostic list IS the assertion.
      var read = BuyingPowerParser().MapWithDiagnostics(BuyingPower());
      var applied = BuyingPowerParser().Apply(BuyingPower());

      Assert.Empty(read.Diagnostics);
      Assert.Equal(8, applied.Consumed.Height);
      Assert.Equal(11, applied.Consumed.Width);
    }

    [Fact]
    public void AndTheTotalIsFoundByItsLandmarkRatherThanByCounting()
    {
      // Nothing in the declaration knows how many records there are, so a file with one more of them
      // reads without a character changing — the difference between a declaration and a script.
      var cells = new object?[9, 11];

      cells[0, 0] = "PCTCAL2 BUYING POWER";
      cells[2, 0] = "ACCOUNT";
      cells[2, 1] = "FUND";
      cells[3, 1] = "ABC";
      cells[3, 6] = 1250.75m;
      cells[4, 1] = "DEF";
      cells[4, 6] = 980.50m;
      cells[5, 1] = "GHI";
      cells[5, 6] = 10.00m;
      cells[6, 1] = "JKL";
      cells[6, 6] = 5.00m;
      cells[8, 0] = "TOTAL";
      cells[8, 6] = 2246.25m;

      var read = BuyingPowerParser().Map(Mixed(cells));

      Assert.Equal(new[] { "ABC", "DEF", "GHI", "JKL" }, read.Allocations.Select(row => row.FundCode));
      Assert.Equal(2246.25m, read.Total);
    }

    [Fact]
    public void AndReadsTheSheetForwardOnly()
    {
      // The cost claim behind the whole row-projection design, measured rather than asserted from
      // the shape of the code: the table's bound is discovered a row at a time and each record is
      // projected as its row is reached, so nothing reads behind the furthest row already read. A
      // declaration with this property is one a windowed reader can serve from one chunk.
      var watched = new WatermarkSpace(BuyingPower());

      var read = BuyingPowerParser().Map(watched);

      Assert.Equal(3, read.Allocations.Count);
      Assert.Equal(7, watched.HighWaterMark);            // the total line, and no further
      Assert.Equal(0, watched.BackwardReach);            // and never a row behind the furthest read
    }

    // =============================================================================================
    // 4. The IRR report in the final vocabulary — one series declared once, placed twice
    // =============================================================================================
    //
    // ProjectionExampleTests reads this same workbook through the lambda table rung, which is what
    // the shipped script says and what the corpus has pinned since wave 2. This is the same document
    // at the TOP of the ladder: every column bound by the member it fills, nothing written about any
    // of them, and the same Heading/Until placement carrying a typed table instead of a lambda.
    // Extended rather than duplicated: what is asserted here is the record content the lambda
    // spelling never had, plus the two facts that must agree between the spellings (the block
    // counts, and full consumption of the sheet).

    private static ICellSpace Irr() => SpreadsheetSpace.Create(TestData("investor-irr.xlsx"), "IRR");

    private static IProjectionDefinition<ICellSpace, IrrReport> IrrReportDeclaration()
    {
      // Not one caption is written down: "Investor Name" binds to InvestorName and "IRR" to Irr,
      // through the comparer, and each member's own type chooses the kind and the accessor.
      var investorBlock = Table<CashFlow>();

      // Declared once, placed twice.
      var series = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      const string Inception = "Cash Flows using inception date";

      return VerticalFlow(v => new IrrReport(
        Header: v.Next(VerticalFlow(h => new IrrHeader(
          Title: h.Next(Text()),
          Fund: h.Next(Text()),
          AsOf: h.Next(Date()),
          Id: h.Next(Text()))).Named("report header")),
        Summary: v.Next(Table<InvestorSummary>().Named("summary")),
        ByTransferDate: v.Next(Until(RowContaining(Inception)).Heading("IRR Details").Heading("Cash Flows Using Transfer Date").Of(series)),
        ByInception: v.Next(Heading(Inception).Of(series))));
    }

    [Fact]
    public void TheIrrReportBindsEveryColumnWithNothingDeclared()
    {
      var report = IrrReportDeclaration().Map(Irr());

      Assert.Equal("Investor IRR Report", report.Header.Title);
      Assert.Equal("Growth Fund II, LP", report.Header.Fund);
      Assert.Equal(new DateTime(2026, 6, 30), report.Header.AsOf);
      Assert.Equal("RPT-00214", report.Header.Id);

      Assert.Equal(
        new[]
        {
          new InvestorSummary("Alpha Capital LLC", 500000m, 125000m, 15000m, 402500m, 0.081m),
          new InvestorSummary("Beacon Trust", 250000m, 40000m, 7500m, 214300m, 0.064m),
          new InvestorSummary("Cedar Holdings", 750000m, 310000m, 22500m, 486200m, 0.102m),
        },
        report.Summary);
    }

    [Fact]
    public void AndOneSeriesReadsBothHalvesOfTheSheet()
    {
      var report = IrrReportDeclaration().Map(Irr());

      // The counts the lambda spelling pins, restated here because they are the two spellings'
      // agreement and not this one's own finding.
      Assert.Equal(new[] { 3, 2, 4 }, report.ByTransferDate.Select(block => block.Count));
      Assert.Equal(new[] { 3, 2, 4 }, report.ByInception.Select(block => block.Count));

      // What the typed rung adds: whole records, from a declaration that named no column.
      Assert.Equal(
        new CashFlow("Alpha Capital LLC", new DateTime(2024, 1, 15), "Contribution", 0m),
        report.ByTransferDate[0][0]);

      Assert.Equal(
        new CashFlow("Cedar Holdings", new DateTime(2025, 8, 1), "Distribution", 0.104m),
        report.ByInception[2][3]);

      // The two series differ in their dates, which is what the file is about: the same investor's
      // flows dated from the transfer and from inception.
      Assert.Equal(new DateTime(2024, 1, 15), report.ByTransferDate[0][0].Date);
      Assert.Equal(new DateTime(2024, 1, 1), report.ByInception[0][0].Date);
    }

    [Fact]
    public void AndTheTypedSpellingDescribesTheWholeSheetTheLambdaOneDid()
    {
      // The regression bar for the top rung: moving from a lambda to a bound type changed what the
      // code says a column means, and nothing else. Same 6x45 sheet, same nothing left over.
      var space = Irr();

      var result = IrrReportDeclaration().MapWithDiagnostics(space);

      Assert.Equal(6, space.Area.Size.Width);
      Assert.Equal(45, space.Area.Size.Height);
      Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void TheFriendDemoIsAFlowOverATypedTable()
    {
      // The shape the demo is built from, over its own small grid: a name, a gap, and a table whose
      // captions are the record's own member names. Nothing about the table is written down.
      var sheet = Mixed(new object?[,]
      {
        { "Acme LP", null },
        { null, null },
        { "Date", "Amount" },
        { new DateTime(2024, 1, 15), 1000.00m },
        { new DateTime(2024, 2, 15), -250.00m },
      });

      var cashFlows = Table<InvestorCashFlow>();

      var investor = VerticalFlow(v => new InvestorBlock(
        Name: v.Next(Text()),
        CashFlows: v.Next(cashFlows)));

      var read = investor.Map(sheet);

      Assert.Equal("Acme LP", read.Name);
      Assert.Equal(
        new[]
        {
          new InvestorCashFlow(new DateTime(2024, 1, 15), 1000.00m),
          new InvestorCashFlow(new DateTime(2024, 2, 15), -250.00m),
        },
        read.CashFlows);
    }

    // =============================================================================================
    // 5. The audited ledger — in the file whose space it names
    // =============================================================================================
    //
    // The fourth declaration lives in ProjectionModelAcceptanceTests.EntryC.Spreadsheets.cs, which
    // is the whole teaching: a file closes the vocabulary over ONE space, and this declaration's
    // space is not this file's.
    //
    // Two declarations stood here and are gone with what they compared. The first answered its
    // demand in PROSE POSITION — `var p = Projection.Over<ISpreadsheetSpace>()`, then `p.` at the
    // three sites that needed it — and the second answered the same demand with a WITNESS argument
    // (`Overlay(Spreadsheets, o => ...)`), with a pin that the two read the file identically. There
    // is one way to name a space now, and it is the import at the top of a file; the scope entry and
    // the witness are both deleted, and neither has a spelling left to be at parity with.

    // --- The results ---------------------------------------------------------------------------------

    private sealed record Allocation(string Account, string Symbol, decimal Weight);

    private sealed record Report(string Title, IReadOnlyList<Allocation> Rows);

    private sealed record SourcedAllocation(string Account, string? Formula);

    private sealed record BuyingPowerRow(string FundCode, decimal? Primary, decimal? Fep);

    private sealed record BuyingPowerAllocation(
      string Title,
      IReadOnlyList<BuyingPowerRow> Allocations,
      decimal Total);

    private sealed record CashFlow(string InvestorName, DateTime Date, string Transaction, decimal Irr);

    private sealed record InvestorSummary(
      string Investors,
      decimal ContributionItd,
      decimal DistributionItd,
      decimal ManagementFeeItd,
      decimal EndBalance,
      decimal Irr);

    private sealed record IrrHeader(string Title, string Fund, DateTime AsOf, string Id);

    private sealed record IrrReport(
      IrrHeader Header,
      IReadOnlyList<InvestorSummary> Summary,
      IReadOnlyList<IReadOnlyList<CashFlow>> ByTransferDate,
      IReadOnlyList<IReadOnlyList<CashFlow>> ByInception);

    private sealed record InvestorCashFlow(DateTime Date, decimal Amount);

    private sealed record InvestorBlock(string Name, IReadOnlyList<InvestorCashFlow> CashFlows);

    private sealed record AuditedLine(string Item, int Qty, double Total, string? Formula);

    private sealed record AuditedLedger(IReadOnlyList<AuditedLine> Lines, string? TotalFormula);
  }
}
