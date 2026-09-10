using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// The fix for UNR001, and the two spellings it deliberately leaves alone. Every offer here is a
  /// pure deletion — of a receiver, or of a witness — because that is the whole of what "the plain
  /// spelling serves" means.
  /// </summary>
  public class UnnecessaryScopeFixTests
  {
    private static DiagnosticResult Unnecessary(string space)
      => new DiagnosticResult(UnrectDiagnostics.UnnecessaryScope).WithLocation(0).WithArguments(space);

    [Fact]
    public Task A_scope_receiver_becomes_the_plain_vocabulary()
      => Verify.Fixes<UnnecessaryScopeAnalyzer, UnnecessaryScopeCodeFixProvider>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return {|#0:p.VerticalFlow|}(v => v.Next(Projection.Text()));
          }
        }
        """,
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return Projection.VerticalFlow(v => v.Next(Projection.Text()));
          }
        }
        """,
        Unnecessary("ISpreadsheetSpace"));

    [Fact]
    public Task A_witness_is_deleted()
      => Verify.Fixes<UnnecessaryScopeAnalyzer, UnnecessaryScopeCodeFixProvider>(
        """
        class Report
        {
          IProjection<IFormulaSpace, string> Header()
            => {|#0:Projection.VerticalFlow|}(SpreadsheetProjections.Formulas, v => v.Next(Projection.Text()));
        }
        """,
        """
        class Report
        {
          IProjection<IFormulaSpace, string> Header()
            => Projection.VerticalFlow(v => v.Next(Projection.Text()));
        }
        """,
        Unnecessary("IFormulaSpace"));

    /// <summary>
    /// Fixing the entry fixes the pipeline: the plain stages carry the same members as the scoped
    /// ones, so everything after the swap follows by type.
    /// </summary>
    [Fact]
    public Task A_pipeline_is_fixed_at_its_entry()
      => Verify.Fixes<UnnecessaryScopeAnalyzer, UnnecessaryScopeCodeFixProvider>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return {|#0:p.Below|}(Projection.RowContaining("Total")).VerticalFlow(v => v.Next(Projection.Text()));
          }
        }
        """,
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return Projection.Below(Projection.RowContaining("Total")).VerticalFlow(v => v.Next(Projection.Text()));
          }
        }
        """,
        Unnecessary("ISpreadsheetSpace"));

    /// <summary>
    /// The file-scoped vocabulary keeps its warning and gets no fix, because there is no edit to
    /// make: the plain spelling would need <c>using static Projection</c>, which the file cannot
    /// have — every shared name would be ambiguous with the closed class it already imports. The
    /// decision is the file's scope, and a code fix is the wrong size for it.
    /// </summary>
    [Fact]
    public Task An_Entry_C_call_keeps_its_warning_and_gets_no_fix()
      => Verify.OffersNoFix<UnnecessaryScopeAnalyzer, UnnecessaryScopeCodeFixProvider>(
        """
        using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

        class Report
        {
          static IProjection<ISpreadsheetSpace, string> Title()
            => {|#0:VerticalFlow|}(v => v.Next(Text()));
        }
        """,
        Unnecessary("ISpreadsheetSpace"));
  }
}
