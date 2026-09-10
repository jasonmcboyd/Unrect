using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Tests.Observations;

// THE SECOND SPACE, AND THEREFORE THE SECOND FILE. The declaration below demands a capability, so
// its file closes the vocabulary over ISpreadsheetSpace rather than over ISpace — and that is why it
// cannot live beside the plain twins in ProjectionModelAcceptanceTests.EntryC.cs: two closings of
// ProjectionBuilders<> in one file would make every shared name ambiguous (CS0121 on invocation,
// every signature being identical), so a declaration file names ONE space. The split is the
// dichotomy, demonstrated: the two files are halves of one partial class and each names its own
// space at the top.
//
// The BACKEND rule, in the same header: a backend ships its own closed class with DISJOINT member
// names, which is exactly what lets these two `using static` directives coexist — `Formula` and
// `RowWithFormula` are nowhere in ProjectionBuilders<>, so nothing collides and the declaration
// below is prefix-free across both vocabularies at once.
using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The fourth acceptance declaration's Entry C twin — the audited ledger, whose whole point is
  /// that it demands something of its space. See <c>ProjectionModelAcceptanceTests.EntryC.cs</c> for
  /// what the phase is and what "same declaration" is asserted to mean; this file adds only what a
  /// demanding declaration adds.
  /// <para>
  /// <b>What the respelling does to the demand: nothing, and visibly nothing.</b> The original
  /// answers the demand once, in prose position, by opening a scope
  /// (<c>var p = Projection.Over&lt;ISpreadsheetSpace&gt;()</c>) and writing <c>p.</c> at the three
  /// sites that need it. Entry C answers it once too, in the import — which is the same act moved
  /// from a local to the top of the file, where it is a header a reader sees before the declaration
  /// rather than a receiver they have to notice mid-expression. The declaration below carries no
  /// type argument, no witness and no prefix, and it demands exactly what the original demands.
  /// </para>
  /// </summary>
  public partial class ProjectionModelAcceptanceTests
  {
    /// <summary>
    /// The audited ledger through the closed vocabularies and the pipeline. Three column offsets
    /// written position-first, and the total line's anchor-then-movement read as one phrase —
    /// <c>On(RowContaining("Total")).Right(3).Of(Formula())</c>, which is where the section is,
    /// then what it reads.
    /// <para>
    /// <c>.Of</c> carries the backend's leaf here, and that is the boundary working as designed
    /// rather than an escape from it: <c>Unrect</c> cannot name <c>Formula</c>, so no stage will ever
    /// have a <c>Formula()</c> terminal, and the door for everything the terminals do not spell is
    /// the one door — declare it, then place it.
    /// </para>
    /// </summary>
    private static IProjection<ISpreadsheetSpace, AuditedLedger> AuditedLedgerThroughTheBuilders()
    {
      // A cell has a value and a formula, so reading both is an overlay's job, as ever.
      var line = Overlay(o => new AuditedLine(
        Item: o.Next(Text()),
        Qty: o.Next(Right(1).Integer()),
        Total: o.Next(Right(3).Double()),
        Formula: o.Next(Right(3).Of(Formula()))));

      var lines = Table(headerRows: 1, eachRow: line);
      var total = On(RowContaining("Total")).Right(3).Of(Formula());

      return VerticalFlow(v => new AuditedLedger(
        Lines: v.Next(lines),
        TotalFormula: v.Next(total)));
    }

    [Fact]
    public void TheAuditedLedgerThroughTheBuildersIsTheSameDeclaration()
    {
      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      // Both sides are read as plain projections, which is the cast the typed layer makes internally
      // and the only way a differential can compare a demanding declaration at all: the demand lives
      // in the static type, and the harness reads what a reading produces. The describer is what
      // makes the four lines and their four formulas visible to the value facet rather than only the
      // record's shape — see the plain half's class remarks.
      AssertL3(
        Read((IProjection<AuditedLedger>)AuditedLedgerDeclaration(), sheet),
        Read((IProjection<AuditedLedger>)AuditedLedgerThroughTheBuilders(), sheet));
    }

    [Fact]
    public void AndTheTwinReadsValuesAndTheFormulasBehindThem()
    {
      // Non-vacuity, and the two readings that make this file worth a whole declaration: a value and
      // the formula behind it in one record, and a shared FOLLOWER whose text the reader
      // reconstructs for its own row rather than reporting the master's.
      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      var ledger = ((IProjection<AuditedLedger>)AuditedLedgerThroughTheBuilders()).Map(sheet);

      Assert.Equal(4, ledger.Lines.Count);

      Assert.Equal("Widget", ledger.Lines[0].Item);
      Assert.Equal(2, ledger.Lines[0].Qty);
      Assert.Equal(4.5, ledger.Lines[0].Total);
      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", ledger.Lines[0].Formula);

      Assert.Equal("Doodad", ledger.Lines[3].Item);
      Assert.Equal(@"IF(B5>0,ROUND(B5*$C$2,2)+SUM($B$2:B5),""B2"")", ledger.Lines[3].Formula);

      Assert.Equal("SUM(D2:D5)", ledger.TotalFormula);
    }
  }
}
