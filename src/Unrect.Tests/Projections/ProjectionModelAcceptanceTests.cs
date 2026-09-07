using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;
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
  /// <see cref="ProjectionScopeTests"/> every member of the scope against its witness twin,
  /// <see cref="Unrect.Tests.FormulaCapabilityTests"/> the capability end to end over a real file,
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
  /// all, because a test project must compile. They are documented, with their verbatim compiler
  /// messages, in <c>docs/design/projection-model-refusals.md</c>, and remain re-runnable in
  /// <c>spike/TypedSpacesGauntlet/MustNotCompile.cs</c>. What IS assertable about them is the
  /// variance that produces them, and that is below.
  /// </para>
  /// </summary>
  public class ProjectionModelAcceptanceTests
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
    private static CellValue[,] AllocationValues()
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

      var values = new CellValue[cells.GetLength(0), cells.GetLength(1)];

      for (var row = 0; row < cells.GetLength(0); row++)
        for (var column = 0; column < cells.GetLength(1); column++)
          values[row, column] = Adapt(cells[row, column]);

      return values;
    }

    private static ISpace PlainAllocations() => new GridSpace(AllocationValues());

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
    private static IProjection<Report> AllocationReport()
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
      // IProjection<T>, which is what every hoisted helper in the corpus and every test in this
      // suite says. If the campaign had cost this, it would have cost everything.
      IProjection<Report> report = AllocationReport();

      var read = report.Map(PlainAllocations());

      Assert.Equal("Buying Power Allocation", read.Title);
      Assert.Equal(
        new[] { new Allocation("A-1", "SPY", 0.25m), new Allocation("A-2", "QQQ", 0.75m) },
        read.Rows);
    }

    [Fact]
    public void AndTheSameDeclarationReadsASpaceThatOffersMore()
    {
      // Variance does the work and nothing is written differently — not a cast, not an overload,
      // not a witness. A projection demanding less runs wherever more is offered.
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
    // 2. Demands climb by themselves, and only ever upward
    // =============================================================================================
    //
    // Scenario 8 (phase 1's question: one modifier definition, both demands), scenario 3 (a lift
    // raising what it touches), and the static half of scenario 5 (the direction the conversion
    // does not exist in — the refusals' whole cause, stated as a fact rather than as a comment).

    [Fact]
    public void OneModifierChainIsTypedTwiceByWhatItWasGiven()
    {
      // The phase-1 result, read as a user reads it: the SAME chain of modifiers, each written once
      // in the library and typed twice at the use site. The two annotated locals are the assertion;
      // that each of them reads its sheet is the proof the type is not the only thing that survived.
      IProjection<decimal> plain = Decimal()
        .Named("total").On(RowContaining("Total")).Right(2);

      IProjection<IFormulaSpace, string?> demanding = Formula()
        .Named("total formula").On(RowContaining("Total")).Right(2);

      Assert.Equal(1.00m, plain.Map(PlainAllocations()));
      Assert.Equal("SUM(C4:C5)", demanding.Map(CapableAllocations()));

      // The names survive the chain too — a placement modifier hands back the projection's own type,
      // so it cannot quietly rewrap it into something with a different identity. (The transparent
      // wrappers — Padded, Select, Until — are a different rule, pinned where each of them lives.)
      Assert.Equal("total", plain.Name);
      Assert.Equal("total formula", demanding.Name);
    }

    [Fact]
    public void ALiftRaisesTheProjectionItTouches()
    {
      // Scenario 3, and the one modifier a self-typed receiver cannot do alone: the matcher's demand
      // and the receiver's are unified by inference. Written plain, read demanding, annotated
      // nowhere — the annotation on the local is what the compiler already inferred.
      IProjection<IFormulaSpace, string> firstFormulaRow =
        Row(cells => cells[0].GetString()).On(RowWithFormula());

      Assert.Equal("A-1", firstFormulaRow.Map(CapableAllocations()));
    }

    [Fact]
    public void ADeclarationsDemandOnlyEverPointsOneWay()
    {
      // The reason every refusal in the ledger is a refusal. A plain declaration is usable where a
      // demanding one is wanted; the reverse conversion does not exist, which is what turns
      // "applied to the wrong backend" from a fault into a compile error.
      var plain = typeof(IProjection<Report>);
      var demanding = typeof(IProjection<IFormulaSpace, Report>);

      Assert.True(demanding.IsAssignableFrom(plain), "a plain declaration runs on a capable space");
      Assert.False(plain.IsAssignableFrom(demanding), "a demanding declaration must not run on any space");
    }

    /// <summary>A hoisted plain helper: exactly what it was, which is the common case and the point.</summary>
    private static IProjection<Allocation> AllocationRow()
      => HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

    /// <summary>A hoisted demanding helper: one type argument more, and it is a statement of requirement.</summary>
    private static IProjection<IFormulaSpace, SourcedAllocation> SourcedRow()
      => Overlay(Formulas, o => new SourcedAllocation(
        Account: o.Next(Text()),
        Formula: o.Next(Formula().Right(2))));

    /// <summary>A helper generic in whatever its caller demands — the one shape that needs the parameter.</summary>
    private static IProjection<TSpace, IReadOnlyList<T>> Sections<TSpace, T>(IProjection<TSpace, T> item)
      where TSpace : class, ISpace
      => VerticalRepeat(item, separatedBy: BlankRows());

    [Fact]
    public void AHoistedHelperSaysWhatItRequiresInItsReturnType()
    {
      // Scenario 7: what tooltips show over a library of hoisted declarations. The plain one did not
      // move; the demanding one grew a type argument that reads as a requirement; and a generic
      // helper composes either, keeping whatever it was handed.
      //
      // The generic helper's plain instantiation is IProjection<ISpace, T> and NOT IProjection<T>:
      // the latter DERIVES from the former, so the conversion runs one way and a generic helper
      // hands back the base form. It is the annotation tax's whole remaining balance, it costs one
      // word at one kind of site, and it is recorded here rather than filed away.
      IProjection<ISpace, IReadOnlyList<Allocation>> plainSections = Sections(AllocationRow());
      IProjection<IFormulaSpace, IReadOnlyList<SourcedAllocation>> demandingSections = Sections(SourcedRow());

      // Both read the same capable sheet — the plain one because variance lets it, the demanding one
      // because the sheet answers what it asks.
      var rows = AllocationRow().On(RowContaining("A-1")).Map(CapableAllocations());

      Assert.Equal(new Allocation("A-1", "SPY", 0.25m), rows);
      Assert.Equal("VerticalRepeat", plainSections.Description);
      Assert.Equal("VerticalRepeat", demandingSections.Description);

      var sourced = SourcedRow().On(RowContaining("A-1")).Map(CapableAllocations());

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
    private static ISpace BuyingPower()
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
    private static IProjection<BuyingPowerAllocation> BuyingPowerParser()
    {
      var allocation = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Text().Right(1)),
        Primary: o.Next(Decimal().OrBlank().Right(6)),
        Fep: o.Next(Decimal().OrBlank().Right(9))));

      var allocations = Table(headerRows: 0, eachRow: allocation)
        .Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue());

      return VerticalFlow(v => new BuyingPowerAllocation(
        Title: v.Next(Text()),
        Allocations: v.Next(allocations),
        Total: v.Next(Decimal().On(RowContaining("TOTAL")).Right(6))));
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
    // of them, and the same .Under/.Until placement carrying a typed table instead of a lambda.
    // Extended rather than duplicated: what is asserted here is the record content the lambda
    // spelling never had, plus the two facts that must agree between the spellings (the block
    // counts, and full consumption of the sheet).

    private static ISpace Irr() => SpreadsheetSpace.Create(TestData("investor-irr.xlsx"), "IRR");

    private static IProjection<IrrReport> IrrReportDeclaration()
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
        ByTransferDate: v.Next(series
          .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
          .Until(RowContaining(Inception))),
        ByInception: v.Next(series
          .Under(Caption(Inception)))));
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
    // 5. The audited ledger, through the scoped entry
    // =============================================================================================
    //
    // Entry B over the committed formula fixture: the demand answered once, in prose position, for a
    // whole declaration. ProjectionScopeTests pins each scope member against its witness twin and
    // reads one cell of this file through the eager door; this is the declaration those members were
    // added for — a table of records that read a value AND the formula behind it, and a total line
    // read as a formula alone.

    private static IProjection<ISpreadsheetSpace, AuditedLedger> AuditedLedgerDeclaration()
    {
      var p = Projection.Over<ISpreadsheetSpace>();

      // A cell has a value and a formula, so reading both is an overlay's job, as ever.
      var line = p.Overlay(o => new AuditedLine(
        Item: o.Next(Text()),
        Qty: o.Next(Integer().Right(1)),
        Total: o.Next(Double().Right(3)),
        Formula: o.Next(Formula().Right(3))));

      var lines = p.Table(headerRows: 1, eachRow: line);
      var total = Formula().On(RowContaining("Total")).Right(3);

      return p.VerticalFlow(v => new AuditedLedger(
        Lines: v.Next(lines),
        TotalFormula: v.Next(total)));
    }

    [Fact]
    public void TheScopedLedgerReadsValuesAndTheFormulasBehindThem()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      var ledger = AuditedLedgerDeclaration().Map(sheet);

      Assert.Equal(4, ledger.Lines.Count);

      Assert.Equal("Widget", ledger.Lines[0].Item);
      Assert.Equal(2, ledger.Lines[0].Qty);
      Assert.Equal(4.5, ledger.Lines[0].Total);
      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", ledger.Lines[0].Formula);

      // The fourth line is a shared FOLLOWER: the file carries only the group's index for it, and
      // the reader reconstructs the text for this row rather than reporting the master's.
      Assert.Equal("Doodad", ledger.Lines[3].Item);
      Assert.Equal(21.5, ledger.Lines[3].Total);
      Assert.Equal(@"IF(B5>0,ROUND(B5*$C$2,2)+SUM($B$2:B5),""B2"")", ledger.Lines[3].Formula);

      // And the total line, below the gap, read as a formula and nothing else.
      Assert.Equal("SUM(D2:D5)", ledger.TotalFormula);
    }

    [Fact]
    public void AndTheSameDeclarationWithNoScopeIsTheSameDeclaration()
    {
      // The scope is sugar over the witness form, so a declaration written the other way must read
      // the file identically. This is the acceptance-level statement of what ProjectionScopeTests
      // says member by member.
      var line = Overlay(Spreadsheets, o => new AuditedLine(
        Item: o.Next(Text()),
        Qty: o.Next(Integer().Right(1)),
        Total: o.Next(Double().Right(3)),
        Formula: o.Next(Formula().Right(3))));

      var witnessed = VerticalFlow(Spreadsheets, v => new AuditedLedger(
        Lines: v.Next(Table(headerRows: 1, eachRow: line)),
        TotalFormula: v.Next(Formula().On(RowContaining("Total")).Right(3))));

      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      var byScope = AuditedLedgerDeclaration().Map(sheet);
      var byWitness = witnessed.Map(sheet);

      Assert.Equal(byWitness.TotalFormula, byScope.TotalFormula);
      Assert.Equal(byWitness.Lines, byScope.Lines);
    }

    /// <summary>The bundle as a witness, so the witnessed twin above names no type argument either.</summary>
    private static Demand<ISpreadsheetSpace> Spreadsheets => Demand<ISpreadsheetSpace>.Instance;

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
