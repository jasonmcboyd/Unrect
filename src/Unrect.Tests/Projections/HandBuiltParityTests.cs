using System;
using System.Linq;
using System.Reflection;

using Unrect.Fluent;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The fluent vocabulary is sugar, and nothing more: every shape it spells can be built by hand
  /// from the model's public constructors, a <see cref="Placement{TSpace}"/> and the Strategies
  /// factories, and the two spellings agree — in the value they yield and in the path and sentence
  /// of a failure. The hand-built side lives in <c>HandBuiltParityTests.ByHand.cs</c>, a file that
  /// imports no vocabulary at all.
  /// </summary>
  public sealed partial class HandBuiltParityTests
  {
    public sealed record Line(string Name, int Qty);

    [Fact]
    public void EveryNodeIsPublicWithAPublicDoor()
    {
      var nodes = typeof(DefinitionNode).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && !type.IsNested && typeof(IProjectionDefinition).IsAssignableFrom(type))
        .ToList();

      Assert.NotEmpty(nodes);

      foreach (var node in nodes)
      {
        Assert.True(node.IsPublic, $"{node.Name} is not public");

        // A node is built by a public constructor; the one singleton (Nothing) is handed out by a
        // public static Instance instead, which is the same door for a type with one value.
        var constructed = node.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 0
          || node.GetField("Instance", BindingFlags.Public | BindingFlags.Static) is not null;

        Assert.True(constructed, $"{node.Name} has no public constructor and no public Instance");
      }
    }

    [Fact]
    public void ALeafAgrees()
    {
      AssertAgree(Point().Decimal(), ByHand.DecimalCell(), Mixed(new object?[,] { { 1.5 } }), 1.5m, Mixed(new object?[,] { { "x" } }));
    }

    [Fact]
    public void ANamedFlowAgrees()
    {
      var first = IntCell();
      var second = IntCell();

      var fluent = VerticalFlow(v => (First: v.Next(first), Second: v.Next(second)));
      var byHand = ByHand.Pair(ByHand.IntegerCell(), ByHand.IntegerCell());

      AssertAgree(fluent, byHand, Grid(new[,] { { 1 }, { 2 } }), (1, 2), Mixed(new object?[,] { { 1 }, { "x" } }));
    }

    [Fact]
    public void APlacedTableAgrees()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Items", null },
        { "Name", "Qty" },
        { "bolt", 3 },
        { "nut", 5 },
        { null, null },
        { "trailer", "ignored" },
      });

      var broken = Mixed(new object?[,]
      {
        { "Items", null },
        { "Name", "Qty" },
        { "bolt", "three" },
      });

      var fluent = Below(RowSaying("Items")).Sized(RowsWhileAnyIsNotBlank()).Of(Table<Line>());
      var expected = new[] { new Line("bolt", 3), new Line("nut", 5) };

      AssertAgree(fluent, ByHand.PlacedTable(), sheet, expected, broken, (a, b) => a.SequenceEqual(b));
    }

    [Fact]
    public void ARepeatWithASeparatorAgrees()
    {
      var item = IntCell();
      var fluent = VerticalRepeat(item, separatedBy: BlankRows());

      AssertAgree(
        fluent, ByHand.Repeated(ByHand.IntegerCell()),
        Grid(new[,] { { 1 }, { 0 }, { 2 }, { 0 }, { 3 } }), new[] { 1, 2, 3 },
        Mixed(new object?[,] { { 1 }, { null }, { "x" } }),
        (a, b) => a.SequenceEqual(b));
    }

    [Fact]
    public void AChoiceAgrees()
    {
      var fluent = Choice(Point().Integer(), Point().Text().Select(text => text.Length));

      AssertAgree(fluent, ByHand.NumberOrLength(), Mixed(new object?[,] { { "abcd" } }), 4, Mixed(new object?[,] { { true } }));
    }

    [Fact]
    public void AnOptionalAgrees()
    {
      var fluent = IntCell().Optional();
      var byHand = ByHand.OptionalNumber(ByHand.IntegerCell());

      Assert.Equal(7, fluent.Map(Grid(new[,] { { 7 } })));
      Assert.Equal(7, byHand.Map(Grid(new[,] { { 7 } })));
      // Over a value type Optional yields default, not null: T? on an unconstrained T is T.
      Assert.Equal(0, fluent.Map(Mixed(new object?[,] { { "x" } })));
      Assert.Equal(0, byHand.Map(Mixed(new object?[,] { { "x" } })));

      // A tolerance boundary absorbs its failures, so the parity that is left is the data face:
      // what the two definitions SAY they are.
      Assert.Equal(fluent.Description, byHand.Description);
      Assert.Equal(fluent.IsWrapper, byHand.IsWrapper);
      Assert.Equal(fluent.Children.Count, byHand.Children.Count);
    }

    /// <summary>
    /// Both spellings yield <paramref name="expected"/> over <paramref name="sheet"/>, and both fail
    /// over <paramref name="broken"/> with the same path and the same sentence.
    /// </summary>
    private static void AssertAgree<T>(
      IProjectionDefinition<ICellSpace, T> fluent,
      IProjectionDefinition<ICellSpace, T> byHand,
      ICellSpace sheet,
      T expected,
      ICellSpace broken,
      Func<T, T, bool>? same = null)
    {
      same ??= (a, b) => Equals(a, b);

      Assert.True(same(expected, fluent.Map(sheet)), "the fluent spelling yielded something else");
      Assert.True(same(expected, byHand.Map(sheet)), "the hand-built spelling yielded something else");

      var fluentFailure = Assert.Throws<ProjectionException>(() => fluent.Map(broken));
      var byHandFailure = Assert.Throws<ProjectionException>(() => byHand.Map(broken));

      Assert.Equal(fluentFailure.Path, byHandFailure.Path);
      Assert.Equal(fluentFailure.Message, byHandFailure.Message);
    }
  }
}
