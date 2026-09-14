using System;

using Unrect.Core;
using Unrect.Projections;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ICellValues>;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario C — <b>the plain-twin floor</b>. The same file, scoped to <c>ICellValues</c>: the
  /// corollary of the dichotomy theorem, which says the plain vocabulary IS Entry C at its floor —
  /// every file conceptually scopes to what its document offers, <c>ICellValues</c> included.
  /// <para>
  /// Two acceptance reads live here. <b>Read 2</b> is the plain twin of the audited report next
  /// door: the same declaration with the formula leaf removed, and therefore nothing in it demands
  /// anything. <b>Read 3</b> is the K-1 pair — anchor prefix, content, bound postfix — which is
  /// here rather than in the audited file for the reason §5.5 gives as guidance: <b>scope the file
  /// to what the declarations READ, not to what the file parses.</b> The K-1 sections read text and
  /// decimals from a synthetic grid; scoped to <c>ISpreadsheetSpace</c> they would compile happily
  /// and then refuse the grid at <c>Map</c> — ledgered as (t).
  /// </para>
  /// <para>
  /// Note what the using block LOSES against the audited file: one line. The floor costs nothing.
  /// </para>
  /// </summary>
  public static class ScenarioCPlain
  {
    public const string KLines = "K-1 Lines 1-21";
    public const string Portfolio = "Portfolio Income";
    public const string Totals = "Totals";

    /// <summary>Read 2 — the investor-irr report with no capability anywhere in it.</summary>
    public static IProjection<ICellValues, PlainIrrReport> Report { get; } = DeclareReport();

    /// <summary>Read 3 — the K-1 pair, each section anchored above and bounded below.</summary>
    public static IProjection<ICellValues, K1Report> K1 { get; } = DeclareK1();

    private static IProjection<ICellValues, PlainIrrReport> DeclareReport()
    {
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title:      v.Next(Text()),
        Fund:       v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId:   v.Next(Text())));

      Func<LabelMap, IProjection<ICellValues, PlainSummaryRow>> plainRow = captions => Overlay(o => new PlainSummaryRow(
        Investor:   o.Next(Right(captions["Investors"]).Text()),
        EndBalance: o.Next(Right(captions["End Balance"]).Decimal())));

      var summary = Table(headerRows: 1, eachRow: plainRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails    = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      var byTransferDate = Until(RowContaining(ScenarioCAudited.Inception))
        .Of(Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")).Of(irrDetails));

      var byInception = Under(Caption(ScenarioCAudited.Inception)).Of(irrDetails);

      return VerticalFlow(v => new PlainIrrReport(
        Header:         v.Next(reportHeader),
        Summary:        v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception:    v.Next(byInception)));
    }

    /// <summary>
    /// Read 3's provoked failure, declared HERE so that the failure the differential compares is
    /// Entry C's own — an anchor that is not in the document, raised through the pipeline's entry.
    /// </summary>
    public static IProjection<ICellValues, KSection> MissingAnchor { get; } = DeclareMissingAnchor();

    private static IProjection<ICellValues, KLine> KLineRow(LabelMap captions) => Overlay(o => new KLine(
      Code: o.Next(Right(captions["Line"]).Text()),
      Amount: o.Next(Right(captions["Amount"]).Decimal())));

    private static IProjection<ICellValues, KSection> DeclareMissingAnchor()
    {
      var kLines = Table(headerRows: 1, eachRow: KLineRow);

      return On(RowContaining("Nope")).Until(RowContaining(Portfolio))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption("Nope")),
          Lines: v.Next(kLines)));
    }

    private static IProjection<ICellValues, K1Report> DeclareK1()
    {
      Func<LabelMap, IProjection<ICellValues, KLine>> kLine = captions => Overlay(o => new KLine(
        Code:   o.Next(Right(captions["Line"]).Text()),
        Amount: o.Next(Right(captions["Amount"]).Decimal())));

      var kLines = Table(headerRows: 1, eachRow: kLine);

      var lines = On(RowContaining(KLines)).Until(RowContaining(Portfolio))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(KLines)),
          Lines:   v.Next(kLines)));

      var portfolio = On(RowContaining(Portfolio)).Until(RowContaining(Totals))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(Portfolio)),
          Lines:   v.Next(kLines)));

      var title = Text();

      return VerticalFlow(v => new K1Report(
        Title:     v.Next(title),
        Lines:     v.Next(lines),
        Portfolio: v.Next(portfolio)));
    }

    // --- Boundary (c), half two: the same helper source, in a plain file ---------------------------
    //
    // Character for character what the audited file says. Only the file's scope differs, and the
    // type that comes out is the evidence: the trap is invisible at the call site and visible only
    // here, side by side.

    /// <inheritdoc cref="ScenarioCAudited.InferredHelperType"/>
    public static string InferredHelperType()
    {
      var section = Under(Caption("Region A")).Of(Table<Line>());

      return Reveal(section);
    }

    private static string Reveal<T>(T value) => Judge.TypeName(typeof(T));
  }
}
