using System.Threading.Tasks;

using Microsoft.CodeAnalysis.Testing;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// UNR001 — the rule and, at least as important, the legitimate twins it must stay quiet about.
  /// The question it asks is always the same: does anything under this declaration genuinely demand
  /// the space the factory is closed over?
  /// </summary>
  public class UnnecessaryScopeTests
  {
    private static DiagnosticResult Unnecessary(string space)
      => new DiagnosticResult(UnrectDiagnostics.UnnecessaryScope).WithLocation(0).WithArguments(space);

    [Fact]
    public Task A_scope_whose_children_are_all_plain_is_reported()
      => Verify.Reports<UnnecessaryScopeAnalyzer>(
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
        Unnecessary("ISpreadsheetSpace"));

    [Fact]
    public Task A_scope_with_a_demanding_child_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string?> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return p.VerticalFlow(v => v.Next(SpreadsheetProjections.Formula()));
          }
        }
        """);

    [Fact]
    public Task A_witness_nothing_under_it_needs_is_reported()
      => Verify.Reports<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          IProjection<IFormulaSpace, string> Header()
            => {|#0:Projection.VerticalFlow|}(SpreadsheetProjections.Formulas, v => v.Next(Projection.Text()));
        }
        """,
        Unnecessary("IFormulaSpace"));

    /// <summary>
    /// The helper trap, which is the finding this rule was built for: in a file scoped to a space,
    /// a hoisted helper that reads nothing capability-specific still demands the file's space, and
    /// every caller inherits a requirement the helper does not have. Nothing about the code says so
    /// — it compiles, it runs, and it refuses spaces it never needed.
    /// </summary>
    [Fact]
    public Task An_Entry_C_helper_that_reads_nothing_capability_specific_is_reported()
      => Verify.Reports<UnnecessaryScopeAnalyzer>(
        """
        using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

        class Report
        {
          static IProjection<ISpreadsheetSpace, string> Title()
            => {|#0:VerticalFlow|}(v => v.Next(Text()));
        }
        """,
        Unnecessary("ISpreadsheetSpace"));

    [Fact]
    public Task An_Entry_C_declaration_that_reads_a_formula_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
        using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

        class Report
        {
          static IProjection<ISpreadsheetSpace, string?> Title()
            => VerticalFlow(v => v.Next(Formula()));
        }
        """);

    /// <summary>
    /// A branch of a scoped declaration that happens to read nothing special is not a mistake: the
    /// scope belongs to the declaration, said once, and qualifying one branch of it would be the
    /// opposite of what a scoped file is for. Only the outermost scoped construction is judged.
    /// </summary>
    [Fact]
    public Task A_plain_branch_inside_a_demanding_declaration_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        record Block(string Title, string? Formula);

        class Report
        {
          IProjection<ISpreadsheetSpace, Block> Section()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return p.VerticalFlow(v => new Block(
              v.Next(p.VerticalFlow(inner => inner.Next(Projection.Text()))),
              v.Next(SpreadsheetProjections.Formula())));
          }
        }
        """);

    [Fact]
    public Task A_declaration_with_no_scope_at_all_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          IProjection<string> Header()
            => Projection.VerticalFlow(v => v.Next(Projection.Text()));
        }
        """);

    /// <summary>
    /// A helper generic in its own space is parameterized, not scoped: it demands whatever its
    /// caller demands, which is the narrow-demand advice already taken.
    /// </summary>
    [Fact]
    public Task A_helper_generic_in_its_space_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          static IProjection<TSpace, string> Header<TSpace>()
            where TSpace : class, ISpace
            => Projection.Over<TSpace>().VerticalFlow(v => v.Next(Projection.Text()));
        }
        """);

    /// <summary>A placement is not a scope. <c>Below(m).Of(table)</c> raises no demand and is not judged.</summary>
    [Fact]
    public Task A_postfix_placement_on_a_plain_projection_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        record Line(string Name);

        class Report
        {
          IProjection<IReadOnlyList<Line>> Lines()
            => Projection.Below(Projection.RowContaining("Total")).Of(Projection.Table<Line>());
        }
        """);

    [Fact]
    public Task A_scoped_pipeline_entered_on_a_demanding_matcher_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return p.Below(SpreadsheetProjections.RowWithFormula()).VerticalFlow(v => v.Next(Projection.Text()));
          }
        }
        """);

    [Fact]
    public Task A_scoped_pipeline_entered_on_a_plain_matcher_is_reported_at_its_entry()
      => Verify.Reports<UnnecessaryScopeAnalyzer>(
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
        Unnecessary("ISpreadsheetSpace"));

    /// <summary>
    /// An ascription is a promise the type system cannot check, and it is the one way a declaration
    /// says "this reads a capability" without any type showing it. It counts as a demand, so a scope
    /// carrying one is doing its job.
    /// </summary>
    [Fact]
    public Task A_scope_over_an_ascribed_child_is_silent()
      => Verify.Silent<UnnecessaryScopeAnalyzer>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string> Header()
          {
            var p = Projection.Over<ISpreadsheetSpace>();

            return p.VerticalFlow(v => v.Next(Projection.Text().Demanding(SpreadsheetProjections.Formulas)));
          }
        }
        """);
  }
}
