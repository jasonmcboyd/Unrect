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
  /// SPIKE, scenario 3 — the owner's scoped sketch, and how a demand climbs the pipeline.
  /// <para>
  /// <b>The sketch as written does not compile, and cannot.</b>
  /// <c>Projection.Over&lt;IGenericSpace&gt;(Offset().SizedToChildren().VerticalRepeat(Table&lt;T&gt;()))</c>
  /// asks for an ascription that states ONE type argument and infers the other; C# infers a method's
  /// type arguments all or none, which is the same wall <c>ProjectionScope&lt;TSpace&gt;</c> was
  /// built to get around. The exact message is recorded in <c>MustNotCompile.cs</c> as (i).
  /// </para>
  /// <para>
  /// The sketch's OTHER half is the answer to it: the scope vends the ENTRIES
  /// (<c>Place.Over&lt;T&gt;().Offset()</c>), so the space is answered at the head of the pipeline
  /// and every stage after it carries it. That is written below, and it works.
  /// </para>
  /// </summary>
  public static class Scenario3
  {
    public static void Run()
    {
      Judge.Section("Scenario 3 — the scoped sketch, and demands climbing the pipeline");

      TheSketch();
      TheDemandingChild();
      HowFarDemandsClimbWithoutAScope();
    }

    // --- The sketch's shape, on a plain grid ------------------------------------------------------

    private static void TheSketch()
    {
      var sheet = Sheets.BuyingPower();
      var scope = Place.Over<ICellValues>();

      var oldSketch = VerticalRepeat(Table<Position>());
      var newSketch = scope.Offset().SizedToChildren().VerticalRepeat(Table<Position>());

      Judge.Same("the sketch's shape reads the same as the repeat it inverts", oldSketch.Map(sheet), newSketch.Map(sheet));
      Judge.Note("Offset() states nothing about the document — it exists to carry the space into the terminal."
        + " Unscoped it is pure appeasement; scoped it is the only way to open a pipeline with no placement to declare.");
    }

    // --- The demanding child ----------------------------------------------------------------------
    //
    // The committed formula fixture, read as a ledger: every line reads a value AND the formula
    // behind it, and the total line is a formula alone.

    private static void TheDemandingChild()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(Sheets.TestData("formulas.xlsx"), "Formulas");

      // OLD — today's scoped entry.
      var q = Projection.Over<ISpreadsheetSpace>();

      var oldLine = q.Overlay(o => new AuditedLine(
        Item: o.Next(Text()),
        Qty: o.Next(Projection.Right(1).Integer()),
        Total: o.Next(Projection.Right(3).Double()),
        Formula: o.Next(Projection.Right(3).Of(Formula()))));

      var oldLedger = q.VerticalFlow(v => new AuditedLedger(
        Lines: v.Next(q.Table(headerRows: 1, eachRow: oldLine)),
        TotalFormula: v.Next(Projection.On(RowContaining("Total")).Right(3).Of(Formula()))));

      // NEW — the placement scope vends the entries; the demand rides the stages to the terminal.
      var p = Place.Over<ISpreadsheetSpace>();

      var newLine = p.Offset().Overlay(o => new AuditedLine(
        Item: o.Next(Text()),
        Qty: o.Next(Place.Right(1).Integer()),
        Total: o.Next(Place.Right(3).Double()),
        Formula: o.Next(Place.Right(3).Of(Formula()))));

      var newLedger = p.Offset().VerticalFlow(v => new AuditedLedger(
        Lines: v.Next(p.Offset().Table(headerRows: 1, eachRow: newLine)),
        TotalFormula: v.Next(Place.On(RowContaining("Total")).Right(3).Of(Formula()))));

      Judge.Same("the audited ledger reads identically through the placement scope", oldLedger.Map(sheet), newLedger.Map(sheet));
      Judge.Note("A backend leaf has no terminal of its own: Formula() rides in through .Of(...), because the terminal"
        + " spread lives in Unrect and a capability's vocabulary lives in its own package. An extension terminal"
        + " (stage.Formula()) is possible and is written below — but only because Formula() states no type argument.");

      // The backend terminal, as Unrect.Spreadsheets would ship it. Two forms, because the demand
      // may come from the stage or from the leaf.
      var terminalTotal = Place.On(RowContaining("Total")).Right(3).Formula();

      Judge.Same("a backend-supplied terminal reads as .Of(Formula())",
        Projection.On(RowContaining("Total")).Right(3).Of(Formula()).Map(sheet),
        terminalTotal.Map(sheet));
    }

    // --- How far a demand climbs without a scope --------------------------------------------------

    private static void HowFarDemandsClimbWithoutAScope()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(Sheets.TestData("formulas.xlsx"), "Formulas");

      // A demanding ARGUMENT raises a plain stage's terminal, with nothing annotated: the overload
      // pair on the stage is the same shape as Projection/Projection.Typed, so inference behaves as
      // it does today.
      var line = Projection.Over<IFormulaSpace>().Overlay(o => new SourcedAllocation(
        Account: o.Next(Text()),
        Formula: o.Next(Projection.Right(3).Of(Formula()))));

      var climbed = Offset().Table(headerRows: 1, eachRow: line);

      IProjection<IFormulaSpace, System.Collections.Generic.IReadOnlyList<SourcedAllocation>> stated = climbed;

      Judge.Same("a demanding eachRow raises a PLAIN stage's table, no scope and no witness",
        Projection.Table(headerRows: 1, eachRow: line).Map(sheet),
        stated.Map(sheet));

      // A demanding MATCHER does the same at the entry — and lands in the scoped hierarchy, so every
      // terminal on it hands the demand out.
      var firstFormulaRow = Place.On(RowWithFormula()).Row(cells => cells[0].GetString());

      IProjection<IFormulaSpace, string> alsoStated = firstFormulaRow;

      Judge.Same("a demanding matcher as an ENTRY raises the whole pipeline",
        Projection.On(RowWithFormula()).Row(cells => cells[0].GetString()).Map(sheet),
        alsoStated.Map(sheet));
      Judge.Note("Both climbs work, and neither needed a word. What does NOT climb is a demand inside a layout LAMBDA:"
        + " the inversion does not remove that wall, it only moves the message (see MustNotCompile (j)).");

      // The witness is the other answer to that wall, and it survives the inversion unchanged: a
      // plain stage takes it as a first argument exactly as the bare factory does.
      var oldWitnessed = VerticalFlow(Formulas, v => new SourcedAllocation(
        Account: v.Next(Text()),
        Formula: v.Next(Formula())));

      var newWitnessed = Offset().VerticalFlow(Formulas, v => new SourcedAllocation(
        Account: v.Next(Text()),
        Formula: v.Next(Formula())));

      Judge.Same("the witness form reads the same on a stage as on the bare factory",
        oldWitnessed.Map(sheet), newWitnessed.Map(sheet));
    }
  }
}
