using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// UNR003 — the sentence the compiler refuses to say at <c>Map</c>. Every positive case here is
  /// code that does not compile; the point of the rule is that what the compiler says about it names
  /// no capability, so the two spaces that disagree are named beside it.
  /// <para>
  /// The space a fixture is scoped to is <c>ISheetCells</c> (see <see cref="Verify.Usings"/>),
  /// which is what the streaming door vends and what a formula-reading declaration out-demands — the
  /// same pair the phase-6 vocabulary makes a reader meet.
  /// </para>
  /// </summary>
  public class DemandsExceedOfferTests
  {
    private static DiagnosticResult Exceeds(string demanded, string offered)
      => new DiagnosticResult(UnrectDiagnostics.DemandsExceedOffer).WithLocation(0).WithArguments(demanded, offered);

    /// <summary>
    /// The boundary finding, verbatim from the note: the over-demand punishes as CS0411 naming
    /// nothing. The compiler's own error is declared here beside ours, on the method name it points
    /// at, so the pair is on record.
    /// <para>
    /// This is also the rule's live-fire test, and the reason it is named first. UNR003 reports
    /// nothing when <see cref="UnrectSymbols.TryLoad"/> cannot find the types it asks for, so a
    /// renamed Core interface turns the whole analyzer off silently and every <c>Silent</c>
    /// assertion below keeps passing for the wrong reason. Only a positive case can tell the two
    /// apart, and it is checked against the assemblies this solution just built.
    /// </para>
    /// </summary>
    [Fact]
    public Task A_formula_reading_declaration_over_a_sheet_names_both_spaces()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static string? Read(ISheetCells sheet)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            return {|#0:formula.{|CS0411:Map|}(sheet)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISheetCells"));

    [Fact]
    public Task Apply_is_read_the_same_way()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static void Read(ISheetCells sheet)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            var applied = {|#0:formula.{|CS0411:Apply|}(sheet)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISheetCells"));

    [Fact]
    public Task MapWithDiagnostics_is_read_the_same_way()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static void Read(ISheetCells sheet)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            var mapped = {|#0:formula.{|CS0411:MapWithDiagnostics|}(sheet)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISheetCells"));

    /// <summary>
    /// The canonical base is the space that asks for nothing, so it is what an over-demand is
    /// measured against when the file names no capability at all.
    /// </summary>
    [Fact]
    public Task A_bare_canonical_space_offers_the_least_of_all()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static string? Read(ISpace grid)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            return {|#0:formula.{|CS0411:Map|}(grid)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISpace"));

    [Fact]
    public Task A_space_that_answers_the_demand_is_silent()
      => Verify.Silent<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static string? Read(ISpreadsheetSpace sheet)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            return formula.Map(sheet);
          }
        }
        """);

    [Fact]
    public Task A_plain_declaration_over_a_plain_grid_is_silent()
      => Verify.Silent<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static string? Read(ISheetCells sheet) => Text().Map(sheet);
        }
        """);

    /// <summary>
    /// An argument that is not a space at all is a different mistake, and the compiler's message for
    /// it already names the types. Nothing is added on top of a message that works.
    /// </summary>
    [Fact]
    public Task An_argument_that_is_not_a_space_is_left_to_the_compiler()
      => Verify.Silent<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static void Read()
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();

            var mapped = formula.{|CS0411:Map|}("not a space");
          }
        }
        """);
  }
}
