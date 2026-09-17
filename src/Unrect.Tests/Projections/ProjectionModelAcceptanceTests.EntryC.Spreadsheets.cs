using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;


// THE SECOND SPACE, AND THEREFORE THE SECOND FILE. The declaration below demands a capability, so
// its file closes the vocabulary over ISpreadsheetSpace rather than over ISheetCells — and that is why it
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
    /// The audited ledger, and the fourth acceptance declaration. Three column offsets written
    /// position-first, and the total line's anchor-then-movement read as one phrase —
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
      var line = Overlay(o =>
      {
        var textSlot = o.Next(Text());
        var right = o.Next(Right(1).Integer());
        var right2 = o.Next(Right(3).Double());
        var right3 = o.Next(Right(3).Of(Formula()));

        return o.Build(read => new AuditedLine(
          Item: read.Of(textSlot),
          Qty: read.Of(right),
          Total: read.Of(right2),
          Formula: read.Of(right3)));
      });

      var lines = Table(headerRows: 1, eachRow: line);
      var total = On(RowContaining("Total")).Right(3).Of(Formula());

      return VerticalFlow(v =>
      {
        var lines2 = v.Next(lines);
        var total2 = v.Next(total);

        return v.Build(read => new AuditedLedger(
          Lines: read.Of(lines2),
          TotalFormula: read.Of(total2)));
      });
    }

    [Fact]
    public void AndAModifierChainOverThisSpaceIsTypedByThisSpace()
    {
      // The demanding half of ProjectionModelAcceptanceTests.OneModifierChainIsTypedByWhateverSpace-
      // ItIsWrittenOver, and it is HERE for the reason this file exists: the chain is the same chain,
      // written once in the library, and what types it is the space the file named. Its plain twin
      // reads a decimal off the same coordinates over a sheet with no formulas in it.
      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      IProjection<ISpreadsheetSpace, string?> demanding =
        On(RowContaining("Widget")).Right(3).Of(Formula().Named("line formula"));

      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", demanding.Map(sheet));
      Assert.Equal("line formula", demanding.Name);
    }

    // (A differential stood here, comparing this declaration against a second spelling of it that
    // answered its demand in prose position — `var p = Projection.Over<ISpreadsheetSpace>()`. That
    // entry is deleted and so is the witness form beside it: a file names its space in the import,
    // and there is no second spelling left to be the same declaration AS.)

    [Fact]
    public void TheAuditedLedgerReadsValuesAndTheFormulasBehindThem()
    {
      // The two readings that make this file worth a whole declaration: a value and the formula
      // behind it in one record, and a shared FOLLOWER whose text the reader reconstructs for its
      // own row rather than reporting the master's.
      var sheet = SpreadsheetSpace.CreateWithFormulas(TestData("formulas.xlsx"), "Formulas");

      var ledger = AuditedLedgerThroughTheBuilders().Map(sheet);

      Assert.Equal(4, ledger.Lines.Count);

      Assert.Equal("Widget", ledger.Lines[0].Item);
      Assert.Equal(2, ledger.Lines[0].Qty);
      Assert.Equal(4.5, ledger.Lines[0].Total);
      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", ledger.Lines[0].Formula);

      Assert.Equal("Doodad", ledger.Lines[3].Item);
      Assert.Equal(21.5, ledger.Lines[3].Total);
      Assert.Equal(@"IF(B5>0,ROUND(B5*$C$2,2)+SUM($B$2:B5),""B2"")", ledger.Lines[3].Formula);

      // And the total line, below the gap, read as a formula and nothing else.
      Assert.Equal("SUM(D2:D5)", ledger.TotalFormula);
    }
  }
}
