using System;
using System.Collections.Generic;

namespace PlacementGauntlet
{
  // The record types the corpus's scripts declare, verbatim where they exist, plus the ones the
  // synthetic scenarios need.

  public record Transaction(string Client, DateTime Date, string Type, decimal Amount);

  public record DealTransaction(
    string AccountKey,
    string FundCode,
    string Name,
    string TransactionType,
    decimal Amount,
    DateTime TransferDate);

  public record SummaryRow(
    string Investor,
    decimal ContributionItd,
    decimal DistributionItd,
    decimal ManagementFeeItd,
    decimal EndBalance,
    double Irr);

  public record CashFlow(string InvestorName, DateTime Date, string Transaction, double Irr);

  public record Position(string Symbol, decimal Quantity, decimal MarketValue, decimal BuyingPower);

  public record Line(string Fund, decimal Amount);

  public record Region(string Name, IReadOnlyList<Line> Lines);

  public record KLine(string Code, decimal Amount);

  public record KSection(string Caption, IReadOnlyList<KLine> Lines);

  public record K1Report(string Title, KSection Lines, KSection Portfolio);

  public record Allocation(string Account, decimal Weight);

  public record SourcedAllocation(string Account, string? Formula);

  public record AuditedLine(string Item, int Qty, double Total, string? Formula);

  public record AuditedLedger(IReadOnlyList<AuditedLine> Lines, string? TotalFormula);

  // --- Scenario C: the audited investor-irr, and its plain twin ---------------------------------
  //
  // "Audited" is what raises the declaration's demand: every summary row carries the formula behind
  // its amount as well as the amount, so the report reads a capability and the file's scope is
  // load-bearing rather than decorative. The plain twin reads the same document with that one leaf
  // removed, which is the whole difference between ProjectionBuilders<ISpreadsheetSpace> and
  // ProjectionBuilders<ISpace>.

  public record IrrHeader(string Title, string Fund, DateTime ReportDate, string ReportId);

  public record AuditedSummaryRow(string Investor, decimal EndBalance, string? AmountFormula);

  public record AuditedIrrReport(
    IrrHeader Header,
    IReadOnlyList<AuditedSummaryRow> Summary,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByTransferDate,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByInception);

  public record PlainSummaryRow(string Investor, decimal EndBalance);

  public record PlainIrrReport(
    IrrHeader Header,
    IReadOnlyList<PlainSummaryRow> Summary,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByTransferDate,
    IReadOnlyList<IReadOnlyList<CashFlow>> ByInception);
}
