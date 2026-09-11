using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE, scenario C — <b>the zero-prefix file</b>, acceptance read 1: the audited
  /// <c>investor-irr</c>. Every summary row carries the formula behind its amount as well as the
  /// amount, so this declaration demands <c>ISpreadsheetSpace</c> — and the demand is answered ONCE,
  /// in the using block above, where C# already puts file-level bindings.
  /// <para>
  /// <b>The using block is the read.</b> Four lines, and only the third is new:
  /// <list type="bullet">
  /// <item><c>using Unrect.Projections;</c> — the POSTFIX half of the vocabulary. Every operator that
  /// spells after its subject (<c>.Until</c> here; <c>.Named</c>, <c>.Optional</c>, <c>.OrBlank</c>,
  /// <c>.Select</c> elsewhere) is an extension method, and CS1106 forbids extension methods in a
  /// generic static class, so Entry C cannot carry them. Under the geography law that is a clean
  /// seam rather than a leak: what is above the subject is imported, what is below is extended.</item>
  /// <item><c>using Unrect.Spreadsheets;</c> — the space type named in the third line, and the door
  /// (<c>SpreadsheetSpace.CreateWithFormulas</c>) the differential opens.</item>
  /// <item><c>using static …ProjectionBuilders&lt;ISpreadsheetSpace&gt;;</c> — the whole prefix
  /// vocabulary with the space answered.</item>
  /// <item><c>using static …SpreadsheetProjections;</c> — the backend's own vocabulary. It composes
  /// by a second import for a reason worth stating: its member names (<c>Formula</c>,
  /// <c>RowWithFormula</c>) are DISJOINT from the closed class's, so nothing is ambiguous. Two
  /// vocabularies coexist exactly when they do not overlap — which is why the one import Entry C
  /// cannot have is <c>Projection</c> itself.</item>
  /// </list>
  /// </para>
  /// <para>
  /// Below: not one <c>Projection.</c>, not one <c>Place.Over&lt;…&gt;()</c>, not one type argument
  /// naming the space. The differential in <c>ScenarioC_Differential.cs</c> compares it against
  /// today's Entry B spelling of the same document at L2 and L3.
  /// </para>
  /// </summary>
  public static class ScenarioCAudited
  {
    public const string Inception = "Cash Flows using inception date";

    /// <summary>The audited report, declared once and reusable — the acceptance read, entire.</summary>
    public static IProjection<ISpreadsheetSpace, AuditedIrrReport> Report { get; } = Declare();

    private static IProjection<ISpreadsheetSpace, AuditedIrrReport> Declare()
    {
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title:      v.Next(Text()),
        Fund:       v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId:   v.Next(Text())));

      // The audited row: the amount, and the formula behind the same cell. Placement-first, in
      // execution order — that many columns along, a decimal — with the leaf as the terminal.
      Func<LabelMap, IProjection<ISpreadsheetSpace, AuditedSummaryRow>> auditedRow = captions => Overlay(o => new AuditedSummaryRow(
        Investor:      o.Next(Right(captions["Investors"]).Text()),
        EndBalance:    o.Next(Right(captions["End Balance"]).Decimal()),
        AmountFormula: o.Next(Right(captions["End Balance"]).Of(Formula()))));

      var summary = Table(headerRows: 1, eachRow: auditedRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails    = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      // Phase 5 retired postfix `.Until`, so the bound is a leading stage over the captioned section.
      var byTransferDate = Until(RowContaining(Inception))
        .Of(Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")).Of(irrDetails));

      var byInception = Under(Caption(Inception)).Of(irrDetails);

      return VerticalFlow(v => new AuditedIrrReport(
        Header:         v.Next(reportHeader),
        Summary:        v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception:    v.Next(byInception)));
    }

    // --- Boundary (c), half one: the helper trap, in the file where it happens ---------------------
    //
    // The body below reads nothing but text and decimals. Written HERE, it comes back demanding
    // ISpreadsheetSpace anyway, because every composing member it calls came from a scope that
    // answers the space. It compiles; nothing warns; the inferred type is the only evidence, and
    // ScenarioC prints it beside the plain file's character-for-character twin.

    /// <summary>
    /// The helper with NOTHING annotated, so what comes back is what the compiler inferred rather
    /// than what an author declared.
    /// </summary>
    public static string InferredHelperType()
    {
      var section = Under(Caption("Region A")).Of(Table<Line>());

      return Reveal(section);
    }

    private static string Reveal<T>(T value) => Judge.TypeName(typeof(T));
  }
}
