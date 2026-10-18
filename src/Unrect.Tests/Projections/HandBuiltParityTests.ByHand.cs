using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

// No `using static …ProjectionBuilders<…>` in this file, on purpose: everything below is built from
// the model's public constructors, the Placement value and the Strategies factories alone. The one
// exception is marked where it stands.
namespace Unrect.Tests.Projections
{
  public sealed partial class HandBuiltParityTests
  {
    /// <summary>The hand-built spellings: what the fluent vocabulary is sugar for.</summary>
    private static class ByHand
    {
      private static Placement<ICellSpace> OneCell => Placement<ICellSpace>.Of(SizeStrategies.ExplicitSize<ICellSpace>(1, 1));

      /// <summary>Fluent: <c>Point().Decimal()</c>.</summary>
      public static IProjectionDefinition<ICellSpace, decimal> DecimalCell()
        => new ReadDefinition<ICellSpace, decimal>(
          "Decimal",
          (Point<ICellSpace> cell, out decimal value, out CellProblem? problem) => cell.TryGetDecimal(out value, out problem),
          OneCell,
          blankIsNull: false);

      /// <summary>Fluent: <c>Point().Integer()</c>.</summary>
      public static IProjectionDefinition<ICellSpace, int> IntegerCell()
        => new ReadDefinition<ICellSpace, int>(
          "Integer",
          (Point<ICellSpace> cell, out int value, out CellProblem? problem) => cell.TryGetInteger(out value, out problem),
          OneCell,
          blankIsNull: false);

      /// <summary>Fluent: <c>Point().Text()</c>.</summary>
      public static IProjectionDefinition<ICellSpace, string> TextCell()
        => new ReadDefinition<ICellSpace, string>(
          "Text",
          (Point<ICellSpace> cell, out string value, out CellProblem? problem) => cell.TryGetText(out value, out problem),
          OneCell,
          blankIsNull: false);

      /// <summary>Fluent: <c>VerticalFlow(v => (v.Next(first), v.Next(second)))</c>, the children named by their use sites.</summary>
      public static IProjectionDefinition<ICellSpace, (int First, int Second)> Pair(IProjectionDefinition<ICellSpace, int> first, IProjectionDefinition<ICellSpace, int> second)
        => new FlowDefinition<ICellSpace, (int First, int Second)>(
          Orientation.Vertical,
          v => (v.Next(first, "first"), v.Next(second, "second")),
          Placement<ICellSpace>.Default);

      /// <summary>
      /// Fluent: <c>Below(RowSaying("Items")).Sized(RowsWhileAnyIsNotBlank()).Of(TableFluent: <c>Below(RowSaying("Items")).Sized(RowsWhileAnyIsNotBlank()).Table&lt;Line&gt;()</c>lt;LineFluent: <c>Below(RowSaying("Items")).Sized(RowsWhileAnyIsNotBlank()).Table&lt;Line&gt;()</c>gt;())</c>.
      /// The reflective table is the one shape whose only public spelling is the fluent one — it
      /// is a composition the binder writes — so it is taken from the vocabulary and PLACED by hand.
      /// </summary>
      public static IProjectionDefinition<ICellSpace, IReadOnlyList<Line>> PlacedTable()
      {
        var table = Unrect.Fluent.ProjectionBuilders<ICellSpace>.Table<Line>();

        return table.With(table.Annotations.WithPlacement(
          table.Placement
            .WithExtent(SizeStrategies.RowsWhileAnyIsNotBlank<ICellSpace>())
            .WithOffset(OffsetStrategies.Past(RowLandmarks.RowSaying<ICellSpace>("Items")))));
      }

      /// <summary>Fluent: <c>VerticalRepeat(item, separatedBy: BlankRows())</c>.</summary>
      public static IProjectionDefinition<ICellSpace, IReadOnlyList<int>> Repeated(IProjectionDefinition<ICellSpace, int> item)
        => new RepeatDefinition<ICellSpace, int>(
          item,
          OffsetStrategies.SkipBlankRows<ICellSpace>(),
          Orientation.Vertical,
          atLeast: 0,
          UseSite.From("item", null),
          Placement<ICellSpace>.Default);

      /// <summary>Fluent: <c>Choice(number, Point().Text().Select(t => t.Length))</c>.</summary>
      public static IProjectionDefinition<ICellSpace, int> NumberOrLength()
        => new ChoiceDefinition<ICellSpace, int>(
          new[]
          {
            IntegerCell(),
            new SelectDefinition<ICellSpace, string, int>(TextCell(), text => text.Length, Placement<ICellSpace>.Default),
          },
          Placement<ICellSpace>.Default);

      /// <summary>
      /// Fluent: <c>number.Optional()</c>. Over a value type the fluent form's <c>T?</c> is <c>T</c>
      /// itself (an unconstrained type parameter's <c>?</c> is an annotation, not a Nullable), so an
      /// absent number is <c>default</c>, 0 — the hand-built form says so plainly.
      /// </summary>
      public static IProjectionDefinition<ICellSpace, int> OptionalNumber(IProjectionDefinition<ICellSpace, int> number)
        => new FallbackDefinition<ICellSpace, int>(
          new SelectDefinition<ICellSpace, int, int>(number, value => value, Placement<ICellSpace>.Default),
          fallback: null,
          fallbackValue: default,
          Placement<ICellSpace>.Default,
          "Optional");
    }
  }
}
