using System.Threading.Tasks;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// UNR002 — the fix on the compiler's CS1503. The error is already at the right place and already
  /// names both types; what it cannot say is that a demand enters a declaration through a
  /// <em>factory</em>, so the edit belongs on the factory rather than on the child it points at.
  /// </summary>
  public class DemandDoorTests
  {
    /// <summary>
    /// The case the boundary note recorded: a scope too weak for the child declared through it.
    /// The fix moves the factory to a scope already in hand that can carry the demand.
    /// </summary>
    [Fact]
    public Task A_child_refused_by_a_layout_moves_the_factory_to_a_scope_that_carries_it()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string?> Header()
          {
            var plain = Projection.Over<ICellValues>();
            var spreadsheet = Projection.Over<ISpreadsheetSpace>();

            return plain.VerticalFlow(v => v.Next({|CS1503:SpreadsheetProjections.Formula()|}));
          }
        }
        """,
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string?> Header()
          {
            var plain = Projection.Over<ICellValues>();
            var spreadsheet = Projection.Over<ISpreadsheetSpace>();

            return spreadsheet.VerticalFlow(v => v.Next(SpreadsheetProjections.Formula()));
          }
        }
        """);

    /// <summary>
    /// A child handed straight to a factory rather than through a cursor is the same mistake one
    /// level shallower, and the same edit fixes it — named arguments and all.
    /// </summary>
    [Fact]
    public Task A_row_refused_by_a_table_moves_the_table_to_the_scope()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, IReadOnlyList<string?>> Rows()
          {
            var plain = Projection.Over<ICellValues>();
            var spreadsheet = Projection.Over<ISpreadsheetSpace>();
            IProjection<IFormulaSpace, string?> row = SpreadsheetProjections.Formula();

            return plain.Table(headerRows: 1, eachRow: {|CS1503:row|});
          }
        }
        """,
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, IReadOnlyList<string?>> Rows()
          {
            var plain = Projection.Over<ICellValues>();
            var spreadsheet = Projection.Over<ISpreadsheetSpace>();
            IProjection<IFormulaSpace, string?> row = SpreadsheetProjections.Formula();

            return spreadsheet.Table(headerRows: 1, eachRow: row);
          }
        }
        """);

    /// <summary>A scope handed in is as much in hand as one opened here.</summary>
    [Fact]
    public Task A_scope_taken_as_a_parameter_is_offered_too()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string?> Header(ProjectionScope<ISpreadsheetSpace> sheet)
          {
            var plain = Projection.Over<ICellValues>();

            return plain.VerticalFlow(v => v.Next({|CS1503:SpreadsheetProjections.Formula()|}));
          }
        }
        """,
        """
        class Report
        {
          IProjection<ISpreadsheetSpace, string?> Header(ProjectionScope<ISpreadsheetSpace> sheet)
          {
            var plain = Projection.Over<ICellValues>();

            return sheet.VerticalFlow(v => v.Next(SpreadsheetProjections.Formula()));
          }
        }
        """);

    /// <summary>
    /// Nothing is invented. Where no scope in hand can carry the demand, the fix stays silent rather
    /// than opening a door the author has not chosen — which space to widen to is a decision, and
    /// the narrowest capability that works is rarely the one a fix would guess.
    /// </summary>
    [Fact]
    public Task No_scope_in_hand_means_no_offer()
      => Verify.OffersNoFixForCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjection<ICellValues, string?> Header()
          {
            var plain = Projection.Over<ICellValues>();

            return plain.VerticalFlow(v => v.Next({|CS1503:SpreadsheetProjections.Formula()|}));
          }
        }
        """);

    /// <summary>CS1503 is a common error; only the projection-shaped one is answered.</summary>
    [Fact]
    public Task An_ordinary_argument_conversion_is_left_to_the_compiler()
      => Verify.OffersNoFixForCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          static void Take(int value)
          {
          }

          static void Give() => Take({|CS1503:"text"|});
        }
        """);
  }
}
