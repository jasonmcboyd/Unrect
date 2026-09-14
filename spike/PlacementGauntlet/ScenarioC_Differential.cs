using System;
using System.Collections.Generic;
using System.Linq;

using PlacementGauntlet.Staged;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario C — the differential for <b>Entry C, the file-scoped vocabulary</b>.
  /// Today's spellings live HERE, and the Entry C readings
  /// live in their own files, for a reason that is itself the arm's first finding:
  /// <b>the two spellings cannot share a file.</b> This file imports <c>Projection</c> statically;
  /// an Entry C file cannot (CS0121 on every shared name, ledgered as (q)). So a differential is
  /// necessarily cross-file, and the acceptance reads are necessarily whole files — which is what
  /// the file-scoped-vocabulary direction claimed the unit of scoping would become.
  /// </summary>
  public static class ScenarioC
  {
    private const string Inception = ScenarioCAudited.Inception;

    public static void Run()
    {
      Judge.Section("Scenario C — the file-scoped vocabulary (Entry C)");

      TheAuditedReport();
      ThePlainFloor();
      TheK1Pair();
      SplitTypeTrick();
      TheHelperTrap();
      ThePartialClassEdge();
    }

    // --- Acceptance read 1: the audited investor-irr -------------------------------------------------
    //
    // Entry C against Entry B: the same machinery with the receiver moved into the using block, so a
    // difference here would be a difference in the re-export wall and nothing else.

    private static void TheAuditedReport()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(Sheets.Example("investor-irr.xlsx"), "IRR");

      var q = Projection.Over<ISpreadsheetSpace>();

      var reportHeader = q.VerticalFlow(v => new IrrHeader(
        Title: v.Next(Text()),
        Fund: v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId: v.Next(Text())));

      // The eachRow lambda is hoisted into an identically named local in BOTH spellings on purpose:
      // its text is captured by CallerArgumentExpression and becomes the record's path segment, so
      // an inline lambda would make the two declarations differ at L3 for a reason that is about
      // this comparison rather than about Entry C.
      Func<LabelMap, IProjection<ISpreadsheetSpace, AuditedSummaryRow>> auditedRow = captions => q.Overlay(o => new AuditedSummaryRow(
        Investor: o.Next(Projection.Right(captions["Investors"]).Text()),
        EndBalance: o.Next(Projection.Right(captions["End Balance"]).Decimal()),
        AmountFormula: o.Next(Projection.Right(captions["End Balance"]).Of(Formula()))));

      var summary = q.Table(headerRows: 1, eachRow: auditedRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      var byTransferDate = Place.Until(RowContaining(Inception))
        .Of(Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")).Of(irrDetails));

      var byInception = Under(Caption(Inception)).Of(irrDetails);

      var today = q.VerticalFlow(v => new AuditedIrrReport(
        Header: v.Next(reportHeader),
        Summary: v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception: v.Next(byInception)));

      var entryC = ScenarioCAudited.Report;

      Judge.SameL2("read 1 — the audited investor-irr: value and geometry",
        today.Apply(sheet), entryC.Apply(sheet));

      var todayMapped = today.MapWithDiagnostics(sheet);
      var entryCMapped = entryC.MapWithDiagnostics(sheet);

      Judge.Same("read 1 at L3 — diagnostics, verbatim",
        todayMapped.Diagnostics.Select(d => d.ToString()).ToList(),
        entryCMapped.Diagnostics.Select(d => d.ToString()).ToList());

      Judge.SameFailure("read 1 at L3 — a provoked failure keeps its path, subject and location",
        () => today.Map(SpreadsheetSpace.CreateWithFormulas(Sheets.Example("investor-summary.xlsx"), "Summary")),
        () => entryC.Map(SpreadsheetSpace.CreateWithFormulas(Sheets.Example("investor-summary.xlsx"), "Summary")));

      Judge.Note("The audited row reads a formula that is not there — investor-irr.xlsx carries values only — so"
        + " every AmountFormula is null. That is the honest per-cell answer for a capable space with nothing to"
        + " report, and it is what makes the demand real without inventing a fixture.");
      Judge.Note("The import blocks are the same size — four lines either way. What Entry C deletes is the"
        + " `var q = Projection.Over<ISpreadsheetSpace>()` line and the four `q.` prefixes above"
        + " (q.VerticalFlow, q.Overlay, q.Table, q.VerticalFlow). It is not an extra import; it is a different one.");
    }

    // --- Acceptance read 2: the plain-twin floor ----------------------------------------------------
    //
    // The dichotomy theorem's corollary, measured: the plain vocabulary is Entry C at its floor. If
    // ProjectionBuilders<ISpace> is not exactly today's plain vocabulary, the claim is false.

    private static void ThePlainFloor()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investor-irr.xlsx"), "IRR");

      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title: v.Next(Text()),
        Fund: v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId: v.Next(Text())));

      Func<LabelMap, IProjection<PlainSummaryRow>> plainRow = captions => Projection.Overlay(o => new PlainSummaryRow(
        Investor: o.Next(Projection.Right(captions["Investors"]).Text()),
        EndBalance: o.Next(Projection.Right(captions["End Balance"]).Decimal())));

      var summary = Projection.Table(headerRows: 1, eachRow: plainRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      var byTransferDate = Place.Until(RowContaining(Inception))
        .Of(Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")).Of(irrDetails));

      var byInception = Under(Caption(Inception)).Of(irrDetails);

      var today = VerticalFlow(v => new PlainIrrReport(
        Header: v.Next(reportHeader),
        Summary: v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception: v.Next(byInception)));

      var entryC = ScenarioCPlain.Report;

      Judge.SameL2("read 2 — the plain twin: value and geometry", today.Apply(sheet), entryC.Apply(sheet));

      Judge.Same("read 2 at L3 — diagnostics, verbatim",
        today.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList(),
        entryC.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList());

      Judge.Note("Same document, same reading, one leaf less. ProjectionBuilders<ISpace> is today's plain"
        + " vocabulary spelled through a scope that answers ISpace — the theorem's corollary, compiled.");

      // And the audited declaration applied to a space that cannot answer: refused, which is the whole
      // point of the demand. Ledgered as (t); prose here because a compile error cannot be run.
      Judge.Note("The reverse does not exist: ScenarioCAudited.Report.Map(grid) does not compile. What it says is the"
        + " arm's worst message — CS0411, naming neither space, because Map takes both type arguments at once."
        + " Stating them turns it into the sentence a reader needs (CS1503, both types named).");
    }

    // --- Acceptance read 3: the K-1 pair --------------------------------------------------------------

    private static void TheK1Pair()
    {
      var sheet = Sheets.K1();

      Func<LabelMap, IProjection<KLine>> kLine = captions => Projection.Overlay(o => new KLine(
        Code: o.Next(Projection.Right(captions["Line"]).Text()),
        Amount: o.Next(Projection.Right(captions["Amount"]).Decimal())));

      var kLines = Projection.Table(headerRows: 1, eachRow: kLine);

      var lines = Projection.On(RowContaining(ScenarioCPlain.KLines)).Until(RowContaining(ScenarioCPlain.Portfolio))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(ScenarioCPlain.KLines)),
          Lines: v.Next(kLines)));

      var portfolio = Projection.On(RowContaining(ScenarioCPlain.Portfolio)).Until(RowContaining(ScenarioCPlain.Totals))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(ScenarioCPlain.Portfolio)),
          Lines: v.Next(kLines)));

      var title = Text();

      var today = VerticalFlow(v => new K1Report(
        Title: v.Next(title),
        Lines: v.Next(lines),
        Portfolio: v.Next(portfolio)));

      var entryC = ScenarioCPlain.K1;

      Judge.SameL2("read 3 — the K-1 pair: value and geometry", today.Apply(sheet), entryC.Apply(sheet));

      Judge.Same("read 3 at L3 — diagnostics, verbatim",
        today.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList(),
        entryC.MapWithDiagnostics(sheet).Diagnostics.Select(d => d.ToString()).ToList());

      Judge.SameFailure("read 3 at L3 — a missing anchor fails with the same path and sentence",
        () => Projection.On(RowContaining("Nope")).Until(RowContaining(ScenarioCPlain.Portfolio))
          .VerticalFlow(v => new KSection(v.Next(Caption("Nope")), v.Next(kLines))).Map(sheet),
        () => ScenarioCPlain.MissingAnchor.Map(sheet));

      Judge.Note("Read 3 lives in the PLAIN file, deliberately: the K-1 sections read text and decimals, and"
        + " §5.5's guidance is to scope a file to what its declarations READ, not to what it parses.");
    }

    // --- The split-type trick, built and read ---------------------------------------------------------

    private static void SplitTypeTrick()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investors-by-deal.xlsx"), "Investors");

      var throughTheDot = SplitRungs<ISpace>.Table.Of<DealTransaction>();
      var throughTheMethod = Staged.ProjectionBuilders<ISpace>.Table<DealTransaction>();

      var deal = VerticalFlow(v => new
      {
        DealCode = v.Next(Text()),
        Transactions = v.Next(throughTheDot),
      });

      var dealAgain = VerticalFlow(v => new
      {
        DealCode = v.Next(Text()),
        Transactions = v.Next(throughTheMethod),
      });

      Judge.Same("the split-type trick reads what the plain rung reads",
        VerticalRepeat(deal, separatedBy: BlankRows()).Map(sheet),
        VerticalRepeat(dealAgain, separatedBy: BlankRows()).Map(sheet));

      Judge.Note("Both bind, and neither needed the space stated: Table.Of<DealTransaction>() splits the two type"
        + " parameters across the type dot, and Table<DealTransaction>() does not have to, because Entry C's space"
        + " is a CLASS parameter closed by the using directive and the all-or-none wall is about a METHOD's.");
      Judge.Note("So the trick is real and unnecessary here. What adopting it would cost is CS0102 — a class cannot"
        + " hold a nested type and a method of one name — so the whole Table family would be re-spelled to split a"
        + " type argument that did not need splitting. Ledgered as (r).");
    }

    // --- Boundary (c): the helper trap ----------------------------------------------------------------

    private static void TheHelperTrap()
    {
      var audited = ScenarioCAudited.InferredHelperType();
      var plain = ScenarioCPlain.InferredHelperType();

      Judge.Different("the same helper source infers a different demand in each file", audited, plain);
      Judge.Note("Character for character the same body — Under(Caption(c)).Of(Table<Line>()) — reading nothing but"
        + " text and decimals. In the audited file it comes back demanding the spreadsheet bundle, silently, because"
        + " every composing member it called came from a scope that answers the space.");
      Judge.Note("NOT REFUSED, and it should not be: the file said what it was for and the compiler believed it. The"
        + " cost is that a helper hoisted out of a declaration file carries the file's scope with it — §5.5's"
        + " unenforced soft spot, and the second diagnostic the queued scope-hygiene analyzer is for.");
      Judge.Note("The guidance that follows is the weakest-demand rule, restated for files: scoped using static for"
        + " DECLARATION files, narrow explicit demands for shared helpers.");
    }

    // --- Boundary (d): the partial-class edge ----------------------------------------------------------

    private static void ThePartialClassEdge()
    {
      Judge.Different("one class, two files, two scopes — the SAME body infers a different demand in each part",
        ScenarioCPartial.SharedBodyType(), ScenarioCPartial.SharedBodyTypeHere());

      Judge.Note("The audited part also carries a member that reads a formula for real, so its file's scope is what"
        + " the scope is FOR; the shared body above is that same file's over-demand riding along. (A demand is a"
        + " phantom — nothing at run time can be asked about it, which is why every reading here is a STATIC type"
        + " captured through a generic parameter rather than anything reflected off an instance.)");
      Judge.Note("It COMPILES, and the reason is exact: a using static binds a FILE, not a type. So a partial class is"
        + " no counter-example to the dichotomy theorem — it is two files, and the theorem is about files.");
      Judge.Note("What it costs is the theorem's own remedy: a reader who opens the TYPE sees two demands on one class"
        + " and no using block to explain either. A shared type is not a declaration file.");
    }
  }
}
