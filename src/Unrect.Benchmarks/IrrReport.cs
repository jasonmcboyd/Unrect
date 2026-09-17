using System;
using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;

namespace Unrect.Benchmarks
{
  public sealed record SummaryRow(
    string Investor,
    decimal ContributionItd,
    decimal DistributionItd,
    decimal ManagementFeeItd,
    decimal EndBalance,
    double Irr);

  public sealed record CashFlow(string InvestorName, DateTime Date, string Transaction, double Irr);

  public sealed record IrrReportHeader(string Title, string Fund, DateTime ReportDate, string ReportId);

  public sealed record Report(
    IrrReportHeader ReportHeader,
    IReadOnlyList<SummaryRow> Summary,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByTransferDate,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByInception);

  /// <summary>
  /// The reference document projection, lifted verbatim from <c>linqpad/investor-irr.linq</c>: a
  /// typed header, a bound summary table, and one repeating block projection declared once and
  /// placed twice under two captions, the first bounded by the caption that begins the second.
  ///
  /// <para>It is shared by the EndToEnd and Diagnostics families on purpose. EndToEnd measures what
  /// it costs to parse; Diagnostics measures what the same parse costs with the diagnostic channel
  /// on, and what failing inside it costs. Two families asking about one declaration means the
  /// declaration must be one object, not two that drift.</para>
  /// </summary>
  internal static class IrrReport
  {
    private static readonly IProjectionDefinition<ISheetCells, IrrReportHeader> Header = VerticalFlow(v =>
    {
      var title = v.Next(Text());
      var fund = v.Next(Text());
      var reportDate = v.Next(Date());
      var reportId = v.Next(Text());

      return v.Build(read => new IrrReportHeader(
        Title: read.Of(title),
        Fund: read.Of(fund),
        ReportDate: read.Of(reportDate),
        ReportId: read.Of(reportId)));
    });

    // Five of six captions bind with nothing said; only Investor needs one, because the sheet's
    // heading is plural where the member is singular.
    private static readonly IProjectionDefinition<ISheetCells, IReadOnlyList<SummaryRow>> Summary =
      Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

    private static readonly IProjectionDefinition<ISheetCells, IReadOnlyList<CashFlow>> InvestorBlock = Table<CashFlow>();

    private static readonly IProjectionDefinition<ISheetCells, IReadOnlyList<IReadOnlyList<CashFlow>>> Series =
      VerticalRepeat(InvestorBlock, separatedBy: BlankRows());

    private static readonly IProjectionDefinition<ISheetCells, IReadOnlyList<IReadOnlyList<CashFlow>>> ByTransferDate =
      Until(RowContaining(CanonicalSpaces.InceptionCaption))
        .Heading(CanonicalSpaces.DetailsCaption)
        .Heading(CanonicalSpaces.TransferDateCaption)
        .Of(Series);

    private static readonly IProjectionDefinition<ISheetCells, IReadOnlyList<IReadOnlyList<CashFlow>>> ByInception =
      Heading(CanonicalSpaces.InceptionCaption).Of(Series);

    public static readonly IProjectionDefinition<ISheetCells, Report> Projection = VerticalFlow(v =>
    {
      var header = v.Next(Header);
      var summary = v.Next(Summary);
      var byTransferDate = v.Next(ByTransferDate);
      var byInception = v.Next(ByInception);

      return v.Build(read => new Report(
        ReportHeader: read.Of(header),
        Summary: read.Of(summary),
        ByTransferDate: read.Of(byTransferDate),
        ByInception: read.Of(byInception)));
    });

    /// <summary>
    /// The same report with one caption that is not in the document. Used by the failure rows: it
    /// fails deep -- inside a section, inside the flow -- so the measured cost is a real path, not
    /// a root-level throw.
    /// </summary>
    public static readonly IProjectionDefinition<ISheetCells, Report> WithMissingSection = VerticalFlow(v =>
    {
      var header = v.Next(Header);
      var summary = v.Next(Summary);
      var byTransferDate = v.Next(ByTransferDate);
      var byInception = v.Next(Heading("No Such Caption Exists Here").Of(Series));

      return v.Build(read => new Report(
        ReportHeader: read.Of(header),
        Summary: read.Of(summary),
        ByTransferDate: read.Of(byTransferDate),
        ByInception: read.Of(byInception)));
    });
  }
}
