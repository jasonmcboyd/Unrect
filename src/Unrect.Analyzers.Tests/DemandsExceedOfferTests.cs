using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// UNR003 — the sentence the compiler refuses to say at <c>Map</c>. Every positive case here is
  /// code that does not compile; the point of the rule is that what the compiler says about it names
  /// no capability, so the two spaces that disagree are named beside it.
  /// </summary>
  public class DemandsExceedOfferTests
  {
    private static DiagnosticResult Exceeds(string demanded, string offered)
      => new DiagnosticResult(UnrectDiagnostics.DemandsExceedOffer).WithLocation(0).WithArguments(demanded, offered);

    /// <summary>
    /// The boundary finding, verbatim from the note: the over-demand punishes as CS0411 naming
    /// nothing. The compiler's own error is declared here beside ours, on the method name it points
    /// at, so the pair is on record.
    /// </summary>
    [Fact]
    public Task A_formula_reading_declaration_over_a_plain_grid_names_both_spaces()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static string? Read(ISpace grid)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula();

            return {|#0:formula.{|CS0411:Map|}(grid)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISpace"));

    [Fact]
    public Task Apply_is_read_the_same_way()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static void Read(ISpace grid)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula();

            var applied = {|#0:formula.{|CS0411:Apply|}(grid)|};
          }
        }
        """,
        Exceeds("IFormulaSpace", "ISpace"));

    [Fact]
    public Task MapWithDiagnostics_is_read_the_same_way()
      => Verify.Reports<DemandsExceedOfferAnalyzer>(
        """
        class Report
        {
          static void Read(ISpace grid)
          {
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula();

            var mapped = {|#0:formula.{|CS0411:MapWithDiagnostics|}(grid)|};
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
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula();

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
          static string Read(ISpace grid) => Projection.Text().Map(grid);
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
            IProjection<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula();

            var mapped = formula.{|CS0411:Map|}("not a space");
          }
        }
        """);
  }
}
