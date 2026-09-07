using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.FormulaProjections;

namespace TypedSpacesGauntlet
{
  /// <summary>
  /// SPIKE, scenario 5. Every member here is supposed to FAIL to compile. Build with
  /// <c>-p:DefineConstants=MUST_NOT_COMPILE</c> to collect the messages; the recorded output lives
  /// in Gauntlet.cs beside the scenario.
  /// </summary>
  public static class MustNotCompile
  {
#if MUST_NOT_COMPILE
    private static IProjection<IFormulaSpace, AuditedReport> Report()
    {
      var title = Text();
      var rows = TableRows<Allocation>();
      var totalFormula = Formula().On(RowContaining("Total")).Right(2);

      return VerticalFlow(Formulas, v => new AuditedReport(
        v.Next(title), v.Next(rows), v.Next(totalFormula)));
    }

    // (a) demanding projection applied to a concrete plain space
    public static AuditedReport A() => Report().Map(Sheets.Plain());

    // (b) the same, with the type arguments stated
    public static AuditedReport B() => Report().Map<IFormulaSpace, AuditedReport>(Sheets.Plain());

    // (c) the same, through an ISpace-typed variable
    public static AuditedReport C()
    {
      ISpace plain = Sheets.Plain();

      return Report().Map(plain);
    }

    // (d) laundering the demand away by annotation
    public static IProjection<AuditedReport> D() => Report();

    // (e) a demanding child inside a PLAIN flow
    public static IProjection<AuditedReport> E()
    {
      var title = Text();
      var rows = TableRows<Allocation>();
      var totalFormula = Formula().On(RowContaining("Total")).Right(2);

      return VerticalFlow(v => new AuditedReport(v.Next(title), v.Next(rows), v.Next(totalFormula)));
    }

    // (f) a demanding alternative in a Choice with a plain one
    public static IProjection<string?> F() => Choice(Text().Select(t => (string?)t), Formula());

    // (g) a demanding item in a plain VerticalRepeat, assigned to a plain type
    public static IProjection<IReadOnlyList<string?>> G() => VerticalRepeat(Formula());

    // (h) a demanding matcher lifted onto a projection that is then used as plain
    public static IProjection<string> H() => Row(cells => cells[0].GetString()).On(RowWithFormula());

    // (i) a plain Map on a demanding projection, receiver-side
    public static string? I() => Formula().Map(Sheets.Plain());

    // (j) the WITNESSED flow spelling is exactly as safe: applied to a plain sheet it still refuses
    public static AuditedReport J()
    {
      var title = Text();
      var rows = TableRows<Allocation>();
      var totalFormula = Formula().On(RowContaining("Total")).Right(2);

      var report = VerticalFlow(Formulas, v => new AuditedReport(
        v.Next(title), v.Next(rows), v.Next(totalFormula)));

      return report.Map(Sheets.Plain());
    }
#endif
  }
}
