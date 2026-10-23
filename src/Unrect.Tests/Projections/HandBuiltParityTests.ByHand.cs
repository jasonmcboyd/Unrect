using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Fluent;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

// No `using static …ProjectionBuilders<…>` in this file, on purpose: everything below is built from
// the model's public constructors, the Placement value, the Strategies factories and the binder's
// public plan alone — nothing the vocabulary alone can spell.
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
      /// Fluent: <c>Below(Rows.Saying("Items")).Sized(Rows.WhileAnyIsNotBlank()).Table&lt;Line&gt;()</c>.
      /// The reflective table is the bind node over a one-row header, its row a plan projected over
      /// the header's labels, wrapped as one unit named for the record — then placed.
      /// </summary>
      public static IProjectionDefinition<ICellSpace, IReadOnlyList<Line>> PlacedTable()
      {
        var plan = RowBinding<Line>.Create<ICellSpace>(null, typeof(ICellSpace));

        var composed = Scaffolding(new LabelledDefinition<ICellSpace, IReadOnlyList<Line>>(
          LabelAxis.Column,
          Scaffolding(new ColumnLabelsDefinition<ICellSpace>(1, Placement<ICellSpace>.Default)),
          labels => Scaffolding(new BandsDefinition<ICellSpace, Line>(
            plan.Project<ICellSpace>(labels), Orientation.Vertical, 1, UseSite.From(null, null), onBlank: null, Placement<ICellSpace>.Default)),
          "the row projection is built from the header's captions; it is known only once a header is read",
          Placement<ICellSpace>.Default));

        IProjectionDefinition<ICellSpace, IReadOnlyList<Line>> table = new UnitDefinition<ICellSpace, IReadOnlyList<Line>>(
          composed,
          Array.Empty<Child>(),
          "Table",
          Placement<ICellSpace>.Of(SizeStrategies.RowsThenColumns(
            RowStrategies.TakeRowsWhileAnyIsNotBlank<ICellSpace>(), ColumnStrategies.TakeTableColumns<ICellSpace>())));

        return table.With(table.Annotations
          .WithUnitName("Table<Line>")
          .WithPlacement(table.Placement
            .WithExtent(SizeStrategies.RowsWhileAnyIsNotBlank<ICellSpace>())
            .WithOffset(OffsetStrategies.Past(RowLandmarks.RowSaying<ICellSpace>("Items")))));
      }

      /// <summary>What the vocabulary marks as its own plumbing, so it contributes no path segment.</summary>
      private static IProjectionDefinition<ICellSpace, T> Scaffolding<T>(IProjectionDefinition<ICellSpace, T> definition)
        => definition.With(definition.Annotations.AsScaffolding());

      /// <summary>Fluent: <c>VerticalRepeat(item, separatedBy: Rows.SkipBlank())</c>.</summary>
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
      /// Fluent: <c>number.ElseDefault()</c>. Over a value type the fluent form's <c>T?</c> is <c>T</c>
      /// itself (an unconstrained type parameter's <c>?</c> is an annotation, not a Nullable), so an
      /// absent number is <c>default</c>, 0 — the hand-built form says so plainly.
      /// </summary>
      public static IProjectionDefinition<ICellSpace, int> DefaultedNumber(IProjectionDefinition<ICellSpace, int> number)
        => new FallbackDefinition<ICellSpace, int>(
          new SelectDefinition<ICellSpace, int, int>(number, value => value, Placement<ICellSpace>.Default),
          fallback: null,
          fallbackValue: default(int),
          Placement<ICellSpace>.Default,
          "ElseDefault");
    }
  }
}
