using System;
using System.Collections.Generic;

using Unrect.Shapes;

using static Unrect.Shapes.Shape;

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
  /// The reference document shape, lifted verbatim from <c>linqpad/investor-irr.linq</c>: a typed
  /// header, a bound summary table, and one repeating block shape declared once and placed twice
  /// under two captions, the first bounded by the caption that begins the second.
  ///
  /// <para>It is shared by the EndToEnd and Diagnostics families on purpose. EndToEnd measures what
  /// it costs to parse; Diagnostics measures what the same parse costs with the diagnostic channel
  /// on, and what failing inside it costs. Two families asking about one declaration means the
  /// declaration must be one object, not two that drift.</para>
  /// </summary>
  internal static class IrrReport
  {
    private static readonly IShape<IrrReportHeader> Header = VerticalFlow(v => new IrrReportHeader(
      Title: v.Next(Text()),
      Fund: v.Next(Text()),
      ReportDate: v.Next(Date()),
      ReportId: v.Next(Text())));

    // Five of six captions bind with nothing said; only Investor needs one, because the sheet's
    // heading is plural where the member is singular.
    private static readonly IShape<IReadOnlyList<SummaryRow>> Summary =
      TableRows<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

    private static readonly IShape<IReadOnlyList<CashFlow>> InvestorBlock = TableRows<CashFlow>();

    private static readonly IShape<IReadOnlyList<IReadOnlyList<CashFlow>>> Series =
      Repeat(InvestorBlock, separatedBy: BlankRows());

    private static readonly IShape<IReadOnlyList<IReadOnlyList<CashFlow>>> ByTransferDate = Series
      .Under(Caption(CanonicalSpaces.DetailsCaption), Caption(CanonicalSpaces.TransferDateCaption))
      .Until(RowContaining(CanonicalSpaces.InceptionCaption));

    private static readonly IShape<IReadOnlyList<IReadOnlyList<CashFlow>>> ByInception =
      Series.Under(Caption(CanonicalSpaces.InceptionCaption));

    public static readonly IShape<Report> Shape = VerticalFlow(v => new Report(
      ReportHeader: v.Next(Header),
      Summary: v.Next(Summary),
      ByTransferDate: v.Next(ByTransferDate),
      ByInception: v.Next(ByInception)));

    /// <summary>
    /// The same report with one caption that is not in the document. Used by the failure rows: it
    /// fails deep -- inside a section, inside the flow -- so the measured cost is a real path, not
    /// a root-level throw.
    /// </summary>
    public static readonly IShape<Report> WithMissingSection = VerticalFlow(v => new Report(
      ReportHeader: v.Next(Header),
      Summary: v.Next(Summary),
      ByTransferDate: v.Next(ByTransferDate),
      ByInception: v.Next(Series.Under(Caption("No Such Caption Exists Here")))));
  }
}
