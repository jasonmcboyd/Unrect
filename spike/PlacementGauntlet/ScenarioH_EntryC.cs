using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ISpace>;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario H — <b>Heading in a zero-prefix file.</b> The Entry C integration: the whole
  /// investor-irr script with both series announced by headings, and not one prefix anywhere.
  /// <para>
  /// Note what the using block LOST against <c>ScenarioC_Plain.cs</c>: nothing was added for
  /// <c>Heading</c> — it is a re-export on the closed class like every other entry. And note what a
  /// declaration lost: <c>Caption</c> appears nowhere in this file, so the leaf that existed only to
  /// be discarded is not imported, not written, and not read.
  /// </para>
  /// <para>
  /// The bound is spelled POSTFIX here rather than as the leading stage of ruling 1's canonical
  /// order, and the differential pins that the two are the same declaration — which is evidence for
  /// the open owner call on where a bound sits, not a position taken.
  /// </para>
  /// </summary>
  public static class ScenarioHEntryC
  {
    private const string Inception = "Cash Flows using inception date";

    /// <summary>The whole script, headings and all.</summary>
    public static IProjection<ISpace, PlainIrrReport> Report { get; } = Declare();

    /// <summary>The bounded series with the bound LEADING, per ruling 1's canonical order.</summary>
    public static IProjection<ISpace, IReadOnlyList<IReadOnlyList<CashFlow>>> BoundLeading { get; }
      = Until(RowContaining(Inception))
        .Heading("IRR Details")
        .Heading("Cash Flows Using Transfer Date")
        .Of(VerticalRepeat(Table<CashFlow>(), separatedBy: BlankRows()));

    private static IProjection<ISpace, PlainIrrReport> Declare()
    {
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title:      v.Next(Text()),
        Fund:       v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId:   v.Next(Text())));

      Func<LabelMap, IProjection<ISpace, PlainSummaryRow>> plainRow = captions => Overlay(o => new PlainSummaryRow(
        Investor:   o.Next(Right(captions["Investors"]).Text()),
        EndBalance: o.Next(Right(captions["End Balance"]).Decimal())));

      var summary = Table(headerRows: 1, eachRow: plainRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails    = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      // Phase 5 retired postfix `.Until`, so the bound leads, per ruling 1's canonical order.
      var byTransferDate = Until(RowContaining(Inception))
        .Heading("IRR Details")
        .Heading("Cash Flows Using Transfer Date")
        .Of(irrDetails);

      var byInception = Heading(Inception).Of(irrDetails);

      return VerticalFlow(v => new PlainIrrReport(
        Header:         v.Next(reportHeader),
        Summary:        v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception:    v.Next(byInception)));
    }
  }
}
