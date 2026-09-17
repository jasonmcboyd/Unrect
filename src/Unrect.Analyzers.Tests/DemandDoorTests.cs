using System.Threading.Tasks;

using Xunit;

namespace Unrect.Analyzers.Tests
{
  /// <summary>
  /// UNR002 — the fix on the compiler's CS1503. The error is already at the right place and already
  /// names both types; what it cannot say is that a demand enters a declaration through a
  /// <em>factory</em>, so the edit belongs on the factory rather than on the child it points at.
  /// <para>
  /// Since phase 6 there are no scopes to move a factory <em>to</em>: a file names its space once in
  /// its <c>using static</c>, so the factory a child out-demands is a bare call on the file's
  /// vocabulary and the edit qualifies that one call with the vocabulary closed over the space the
  /// child asks for. The old fixtures opened two scopes with <c>Projection.Over&lt;T&gt;()</c> and
  /// asked which one the fix would reach for; the question no longer exists, and the fix's sentence
  /// is what changed with it.
  /// </para>
  /// </summary>
  public class DemandDoorTests
  {
    /// <summary>
    /// The case the boundary note recorded: a file scoped to a sheet, and a child under it that
    /// reads a formula. The fix names the vocabulary that can carry the demand.
    /// </summary>
    [Fact]
    public Task A_child_refused_by_a_layout_is_offered_the_vocabulary_that_carries_it()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjectionDefinition<IFormulaSpace, string?> Header()
            => VerticalFlow(v =>
            {
              var formula = v.Next({|CS1503:SpreadsheetProjections.Formula<IFormulaSpace>()|});
              return v.Build(read => read.Of(formula));
            });
        }
        """,
        """
        class Report
        {
          IProjectionDefinition<IFormulaSpace, string?> Header()
            => ProjectionBuilders<IFormulaSpace>.VerticalFlow(v =>
            {
              var formula = v.Next(SpreadsheetProjections.Formula<IFormulaSpace>());
              return v.Build(read => read.Of(formula));
            });
        }
        """);

    /// <summary>
    /// A child handed straight to a factory rather than through a cursor is the same mistake one
    /// level shallower, and the same edit fixes it — named arguments and all.
    /// <para>
    /// The title is pinned here because with this fix the sentence IS the product: the edit is one
    /// qualification, and what earns it is the naming of the two spaces the reader has to compare —
    /// the one the child demands and the one the file's <c>using static</c> named.
    /// </para>
    /// </summary>
    [Fact]
    public Task A_row_refused_by_a_table_is_offered_the_vocabulary_that_carries_it()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          IProjectionDefinition<IFormulaSpace, IReadOnlyList<string?>> Rows()
          {
            IProjectionDefinition<IFormulaSpace, string?> row = SpreadsheetProjections.Formula<IFormulaSpace>();

            return Table(headerRows: 1, eachRow: {|CS1503:row|});
          }
        }
        """,
        """
        class Report
        {
          IProjectionDefinition<IFormulaSpace, IReadOnlyList<string?>> Rows()
          {
            IProjectionDefinition<IFormulaSpace, string?> row = SpreadsheetProjections.Formula<IFormulaSpace>();

            return ProjectionBuilders<IFormulaSpace>.Table(headerRows: 1, eachRow: row);
          }
        }
        """,
        titled:
          "Use 'ProjectionBuilders<IFormulaSpace>.Table' here — this child demands 'IFormulaSpace', "
          + "which a 'Table' declared over 'ISheetCells' cannot carry");

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

    /// <summary>
    /// A leaf refused somewhere that is not a composing factory is not this fix's business: there is
    /// no child entering through a factory, so there is nothing to re-declare and the compiler's own
    /// message — which names both types — is the whole of what can be said.
    /// </summary>
    [Fact]
    public Task A_refusal_outside_a_composing_factory_is_left_to_the_compiler()
      => Verify.OffersNoFixForCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          static void Take(IProjectionDefinition<ISheetCells, string?> child)
          {
          }

          static void Give() => Take({|CS1503:SpreadsheetProjections.Formula<IFormulaSpace>()|});
        }
        """);

    /// <summary>
    /// A demand reaches a factory through a strategy as well as through a child declaration: a
    /// separator that reads a formula is refused by a repeat declared over a sheet, and the edit is
    /// the same one — the factory, named over the space the rule asks for.
    /// <para>
    /// The item is left undeclared so that the separator is the only thing the call is refused for:
    /// what is under test is that a demand carried by a <em>rule</em> reaches the fix at all.
    /// </para>
    /// </summary>
    [Fact]
    public Task A_separator_that_demands_more_than_the_repeat_is_offered_the_vocabulary_that_carries_it()
      => Verify.FixesCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          static object Blocks(IOffsetStrategy<ISpreadsheetSpace> gap)
            => VerticalRepeat<string>(default!, {|CS1503:gap|});
        }
        """,
        """
        class Report
        {
          static object Blocks(IOffsetStrategy<ISpreadsheetSpace> gap)
            => ProjectionBuilders<ISpreadsheetSpace>.VerticalRepeat<string>(default!, gap);
        }
        """,
        titled:
          "Use 'ProjectionBuilders<ISpreadsheetSpace>.VerticalRepeat' here — this child demands "
          + "'ISpreadsheetSpace', which a 'VerticalRepeat' declared over 'ISheetCells' cannot carry");

    /// <summary>
    /// The same mis-scoped rule handed to <c>Sized</c>, which composes no child: there is no factory
    /// to re-declare, so nothing is offered and the compiler's own message — which names both
    /// strategy types — is the whole of what can be said.
    /// </summary>
    [Fact]
    public Task A_mis_scoped_rule_outside_a_composing_factory_is_left_to_the_compiler()
      => Verify.OffersNoFixForCompilerError<DemandDoorCodeFixProvider>(
        """
        class Report
        {
          static object Header(IAreaStrategy<ISpreadsheetSpace> rule) => Sized({|CS1503:rule|}).Of(AsText());
        }
        """);
  }
}
