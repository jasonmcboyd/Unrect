using System.Linq;

using PlacementGauntlet.Staged;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.Place;
using static Unrect.Projections.Projection;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario 1 — the five example declarations, rewritten under the inverted pipeline and
  /// run against the workbooks the scripts parse. Each one is written BOTH ways and the two readings
  /// are compared.
  /// <para>
  /// <b>The investor-irr rewrite here is the FIRST TRIAL's</b>, when <c>.Until</c> was a pipeline
  /// stage. The geography law superseded it; <c>ScenarioG</c> carries the current spelling of the
  /// same declaration, and both are kept so the trial's record stays readable.
  /// </para>
  /// <para>
  /// The first thing the rewrite discovers: <b>three of the five scripts contain no placement
  /// operator at all</b>. Silence is adjacency, tables absorb their own gaps, and repeats carry
  /// their separators, so <c>simple-report</c>, <c>investors-by-deal</c> and <c>array</c> are
  /// already written in the pipeline's "both stages default" spelling — <c>Vertical(…)</c> bare.
  /// The inversion is invisible in them, which is the zero-change migration claim, measured.
  /// </para>
  /// </summary>
  public static class Scenario1
  {
    public static void Run()
    {
      Judge.Section("Scenario 1 — the five example declarations");

      SimpleReport();
      InvestorsByDeal();
      InvestorSummary();
      InvestorIrr();
      Array();
      MovementsComposeOntoAShapesOwnDefault();
    }

    // --- The trap under the movement entries --------------------------------------------------------
    //
    // A movement COMPOSES onto the shape's own default offset (a Table's "past the blank rows in
    // front of me"), where an anchor REPLACES it. A façade that turned Down(1) into an offset
    // strategy would silently change every table in the corpus, so the stages replay the modifier the
    // author wrote instead. This is the one differential that would fail loudly if they did not.

    private static void MovementsComposeOntoAShapesOwnDefault()
    {
      var sheet = Sheets.Grid(new object?[,]
      {
        { null, null },
        { null, null },
        { "Fund", "Amount" },
        { "Alpha", 100m },
        { "Beta", 200m },
      });

      var eachRow = Row(cells => cells[0].GetString());

      var oldMoved = Table(headerRows: 0, eachRow: eachRow).Down(1);
      var newMoved = Place.Down(1).Table(headerRows: 0, eachRow: eachRow);

      Judge.Same("Down(1) as an entry still composes onto the table's own blank-row skip",
        oldMoved.Map(sheet), newMoved.Map(sheet));
      Judge.Note($"Both read {Judge.Render(newMoved.Map(sheet))}, and the unmoved table reads "
        + $"{Judge.Render(Table(headerRows: 0, eachRow: eachRow).Map(sheet))}.");
      Judge.Note("Two blank rows skipped by the table, then one more row by the movement."
        + " Had the entry been lowered to OffsetBy, it would have replaced the skip and read the blanks.");
    }

    // --- simple-report ----------------------------------------------------------------------------
    //
    // No placement anywhere. Under the grammar, the bare terminal IS the declaration; the explicit
    // spelling below states both stages at their defaults and must read identically.

    private static void SimpleReport()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("simple-report.xlsx"), "Report");

      var transactions = Table<Transaction>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type"));

      // OLD — today's vocabulary.
      var oldHeader = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        SubTitle = v.Next(Text()),
        ReportDate = v.Next(Date()),
        ReportId = v.Next(Text()),
      });

      var oldReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(oldHeader),
        Transactions = v.Next(transactions),
      });

      // NEW, bare — character for character the same declaration.
      var newHeader = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        SubTitle = v.Next(Text()),
        ReportDate = v.Next(Date()),
        ReportId = v.Next(Text()),
      });

      var newReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(newHeader),
        Transactions = v.Next(transactions),
      });

      // NEW, both stages stated at their defaults — the grammar's fourth spelling, and the reading
      // that shows SizedToChildren is explicitness rather than ceremony.
      var statedHeader = Offset().SizedToChildren().VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        SubTitle = v.Next(Text()),
        ReportDate = v.Next(Date()),
        ReportId = v.Next(Text()),
      });

      var statedReport = Offset().SizedToChildren().VerticalFlow(v => new
      {
        ReportHeader = v.Next(statedHeader),
        Transactions = v.Next(transactions),
      });

      Judge.Same("simple-report: bare new spelling reads as the old one", oldReport.Map(sheet), newReport.Map(sheet));
      Judge.Same("simple-report: stated-defaults spelling reads the same again", oldReport.Map(sheet), statedReport.Map(sheet));
      Judge.Note("simple-report declares no placement: the rewrite is the identity, and the explicit stages are inert.");
    }

    // --- investors-by-deal ------------------------------------------------------------------------

    private static void InvestorsByDeal()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investors-by-deal.xlsx"), "Investors");

      var dealCode = Text();
      var transactions = Table<DealTransaction>();

      var deal = VerticalFlow(v => new
      {
        DealCode = v.Next(dealCode),
        Transactions = v.Next(transactions),
      });

      var oldDeals = VerticalRepeat(deal, separatedBy: BlankRows());
      var newDeals = Offset().VerticalRepeat(deal, separatedBy: BlankRows());

      Judge.Same("investors-by-deal: neutral entry is inert", oldDeals.Map(sheet), newDeals.Map(sheet));
      Judge.Note("The separator is an ARGUMENT of the repeat, not a placement stage — the pipeline has nothing to say about it.");
    }

    // --- investor-summary -------------------------------------------------------------------------
    //
    // The corpus's one movement written on a composite: the details section steps over the blank
    // rows in front of it. Inverted, the movement leads.

    private static void InvestorSummary()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investor-summary.xlsx"), "Summary");

      var investorName = Text();

      var detailTransactions = Table(r => new
      {
        Date = r["Date"].GetDateTime(),
        Type = r["Transaction Type"].GetString(),
        Amount = r["Amount"].GetDecimal(),
      });

      var investorDetail = VerticalFlow(v => new
      {
        Investor = v.Next(investorName),
        Transactions = v.Next(detailTransactions),
      });

      var reportHeader = Column(c => new
      {
        Title = c[0].GetString(),
        ReportDate = c[1].GetDateTime(),
        ReportId = c[2].GetString(),
      });

      var summary = Table(r => new
      {
        Investor = r["Investor"].GetString(),
        Contributions = r["Contributions"].GetDecimal(),
        Distributions = r["Distributions"].GetDecimal(),
        Net = r["Net"].GetDecimal(),
      });

      // OLD: subject first, placement last.
      var oldDetails = VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1).AfterBlankRows();

      // NEW: placement first, projection last.
      var newDetails = Place.AfterBlankRows().VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1);

      var oldReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        Details = v.Next(oldDetails),
      });

      var newReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        Details = v.Next(newDetails),
      });

      Judge.Same("investor-summary: AfterBlankRows() as an entry reads as the postfix modifier", oldReport.Map(sheet), newReport.Map(sheet));
      Judge.Note("A movement entry REPLAYS the modifier rather than composing a strategy, which is what keeps"
        + " Down(2).Table<T>() meaning 'past the blank rows, then two more' as Table<T>().Down(2) does.");
    }

    // --- investor-irr -----------------------------------------------------------------------------
    //
    // The .Until site, spelled BOTH ways (spec §2), and the one declaration in the corpus where the
    // pipeline cannot take over the whole placement: .Under is a caption-consuming wrapper, not a
    // stage, so the new spelling is half pipeline and half postfix.

    private const string Inception = "Cash Flows using inception date";

    private static void InvestorIrr()
    {
      var sheet = SpreadsheetSpace.Create(Sheets.Example("investor-irr.xlsx"), "IRR");

      var reportHeader = VerticalFlow(v => new
      {
        Title = v.Next(Text()),
        Fund = v.Next(Text()),
        ReportDate = v.Next(Date()),
        ReportId = v.Next(Text()),
      });

      var summary = Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));
      var investorBlock = Table<CashFlow>();
      var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      // OLD — .Until as a post-terminal wrapper.
      var oldByTransferDate = irrDetails
        .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
        .Until(RowContaining(Inception));

      // NEW — .Until as a pipeline STAGE, over a projection declared elsewhere.
      var newByTransferDate = Place.Until(RowContaining(Inception))
        .Of(irrDetails.Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")));

      var byInception = irrDetails.Under(Caption(Inception));

      var oldReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        ByTransferDate = v.Next(oldByTransferDate),
        ByInception = v.Next(byInception),
      });

      var newReport = VerticalFlow(v => new
      {
        ReportHeader = v.Next(reportHeader),
        Summary = v.Next(summary),
        ByTransferDate = v.Next(newByTransferDate),
        ByInception = v.Next(byInception),
      });

      var oldMapped = oldReport.MapWithDiagnostics(sheet);
      var newMapped = newReport.MapWithDiagnostics(sheet);

      Judge.Same("investor-irr: Until as a stage reads as Until as a wrapper", oldMapped.Value, newMapped.Value);
      Judge.Same("investor-irr: and says the same about what it did not describe",
        Render(oldMapped), Render(newMapped));
      Judge.Note("Both spellings are the SAME CALL — the stage records .Until(landmark) and the terminal replays it."
        + " What differs is word order: 'until the next caption, this repeat' against 'this repeat, until the next caption'.");
      Judge.Note("SUPERSEDED by the geography law (see ScenarioG): a bound's landmark is BELOW the section it ends, so"
        + " it belongs after the subject — postfix .Until, unchanged — and `.Under` DOES stage, as an entry holding the"
        + " captions. The current spelling of this declaration is"
        + " Under(Caption(a), Caption(b)).Of(irrDetails).Until(RowContaining(Inception)).");
    }

    // --- array ------------------------------------------------------------------------------------

    private static void Array()
    {
      var space = Sheets.Numbers();

      var firstRow = Row(r => r.Select(v => v.GetInt()).ToArray());

      var rest = Range(b => b.Rows.Select(r => r.Select(v => v.GetInt()).ToArray()).ToArray());

      var block = VerticalFlow(v => new
      {
        FirstRow = v.Next(firstRow),
        Rest = v.Next(rest),
      });

      var oldBlocks = VerticalRepeat(block, separatedBy: BlankRows());
      var newBlocks = SizedToChildren().VerticalRepeat(block, separatedBy: BlankRows());

      Judge.Same("array: the size stage stated at its default is inert", oldBlocks.Map(space), newBlocks.Map(space));
    }

    private static string Render<T>(MapResult<T> mapped)
      => string.Join(" | ", mapped.Diagnostics.Select(d => d.ToString()));
  }
}
