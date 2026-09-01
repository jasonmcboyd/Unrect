using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// A shape is an inspectable value, not a closure over a file: its name, description, placement
  /// and children can all be read without ever handing it a space. That is what makes the wave-3
  /// diagnostics (dry runs, traces, capability checks) possible, and it is what makes one shape
  /// safe to apply to many spaces at once.
  /// </summary>
  public class ShapeInspectionTests
  {
    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    // --- Descriptions ------------------------------------------------------------------------------------

    [Fact]
    public void LeavesDescribeThemselvesStructurally()
    {
      Assert.Equal("Cell", Cell(v => v.GetInt()).Description);
      Assert.Equal("Row", Row(s => s.Count).Description);
      Assert.Equal("Row(3)", Row(3, s => s.Count).Description);
      Assert.Equal("Column", Column(s => s.Count).Description);
      Assert.Equal("Column(4)", Column(4, s => s.Count).Description);
      Assert.Equal("Cells", Cells(b => b.Width).Description);
      Assert.Equal("Cells(2, 3)", Cells(2, 3, b => b.Width).Description);
    }

    [Fact]
    public void CompositesDescribeThemselvesStructurally()
    {
      Assert.Equal("Vertical", Vertical(IntCell(), IntCell()).Description);
      Assert.Equal("Horizontal", Horizontal(IntCell(), IntCell()).Description);
      Assert.Equal("Repeat", Repeat(IntCell()).Description);
      Assert.Equal("RepeatHorizontal", RepeatHorizontal(IntCell()).Description);
      Assert.Equal("Select", IntCell().Select(v => v + 1).Description);
      Assert.Equal("Table", Table(t => t.RowCount).Description);
      Assert.Equal("TableRows", TableRows(r => r[0]).Description);
    }

    [Fact]
    public void ANameDoesNotReplaceTheDescription()
    {
      var shape = IntCell().Named("report id");

      Assert.Equal("report id", shape.Name);
      Assert.Equal("Cell", shape.Description);
    }

    [Fact]
    public void AnUnnamedShapeHasNoName()
    {
      Assert.Null(IntCell().Name);
    }

    // --- Children ----------------------------------------------------------------------------------------

    [Fact]
    public void LeavesHaveNoChildren()
    {
      Assert.Empty(IntCell().Children);
      Assert.Empty(Row(s => s.Count).Children);
      Assert.Empty(Table(t => t.RowCount).Children);
    }

    [Fact]
    public void AStackExposesItsChildrenInDeclarationOrder()
    {
      var first = IntCell().Named("first");
      var second = IntCell().Named("second");
      var third = IntCell().Named("third");

      var children = Vertical(first, second, third).Children;

      Assert.Equal(3, children.Count);
      Assert.Same(first, children[0]);
      Assert.Same(second, children[1]);
      Assert.Same(third, children[2]);
    }

    [Fact]
    public void ARepeatExposesItsItem()
    {
      var item = IntCell().Named("item");

      Assert.Same(item, Assert.Single(Repeat(item).Children));
    }

    [Fact]
    public void ASelectExposesTheShapeItWraps()
    {
      var inner = IntCell().Named("inner");

      Assert.Same(inner, Assert.Single(inner.Select(v => v + 1).Children));
    }

    // --- Transparency ---------------------------------------------------------------------------------------

    [Fact]
    public void OnlyAnUnnamedSelectIsTransparent()
    {
      Assert.True(IntCell().Select(v => v + 1).IsTransparent);
      Assert.False(IntCell().Select(v => v + 1).Named("named").IsTransparent);
      Assert.False(IntCell().IsTransparent);
      Assert.False(Vertical(IntCell(), IntCell()).IsTransparent);
      Assert.False(Repeat(IntCell()).IsTransparent);
    }

    // --- Placement ---------------------------------------------------------------------------------------------

    [Fact]
    public void LeavesDeclareTheirArea()
    {
      Assert.NotNull(IntCell().Placement.Area);
      Assert.NotNull(Row(s => s.Count).Placement.Area);
      Assert.NotNull(Table(t => t.RowCount).Placement.Area);
    }

    [Fact]
    public void CompositesDeriveTheirArea()
    {
      Assert.Null(Vertical(IntCell(), IntCell()).Placement.Area);
      Assert.Null(Repeat(IntCell()).Placement.Area);
      Assert.Null(IntCell().Select(v => v).Placement.Area);
    }

    // --- Walking a whole declaration without a space ---------------------------------------------------------------

    [Fact]
    public void AWholeShapeTreeCanBeWalkedWithoutASpace()
    {
      // This is the dry-run traversal in miniature: no ISpace anywhere.
      var shape = Vertical(
        Column(3, c => c.Count).Named("header"),
        Repeat(
          Vertical(
            Cell(v => v.GetString()).Named("code"),
            TableRows(r => r[0]).Named("rows"))
          .Named("block"))
        .Named("blocks"));

      var described = Describe(shape).ToArray();

      Assert.Equal(
        new[]
        {
          "Vertical",
          "  'header' (Column(3))",
          "  'blocks' (Repeat)",
          "    'block' (Vertical)",
          "      'code' (Cell)",
          "      'rows' (TableRows)",
        },
        described);
    }

    private static IEnumerable<string> Describe(IShape shape, int depth = 0)
    {
      var label = shape.Name is null ? shape.Description : $"'{shape.Name}' ({shape.Description})";

      yield return new string(' ', depth * 2) + label;

      foreach (var child in shape.Children)
        foreach (var line in Describe(child, depth + 1))
          yield return line;
    }

    // --- Reuse ---------------------------------------------------------------------------------------------------

    [Fact]
    public void OneShapeCanBeAppliedToManyDifferentSpaces()
    {
      var shape = Vertical(IntCell(), Repeat(IntCell()));

      Assert.Equal("1:2,3", Read(shape, Grid(new[,] { { 1 }, { 2 }, { 3 } })));
      Assert.Equal("9:8", Read(shape, Grid(new[,] { { 9 }, { 8 } })));
      Assert.Equal("4:5,6,7", Read(shape, Grid(new[,] { { 4 }, { 5 }, { 6 }, { 7 } })));
    }

    [Fact]
    public void OneShapeCanBeAppliedToManySpacesConcurrently()
    {
      // The context tree is built per Map call, so nothing is shared between concurrent runs.
      var shape = Vertical(IntCell(), Repeat(IntCell()));

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1 }, { seed + 2 }, { seed + 3 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = Read(shape, spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}:{index + 2},{index + 3}", results[index]);
    }

    [Fact]
    public void MappingDoesNotMutateTheShape()
    {
      var shape = IntCell().Named("value").Down(1);

      shape.Map(Grid(new[,] { { 1 }, { 2 } }));

      Assert.Equal("value", shape.Name);
      Assert.NotNull(shape.Placement.Area);
      Assert.Equal(2, shape.Map(Grid(new[,] { { 1 }, { 2 } })));
    }

    /// <summary>Renders a result as text so array identity never enters the comparison.</summary>
    private static string Read(IShape<(int, IReadOnlyList<int>)> shape, ISpace space)
    {
      var (first, rest) = shape.Map(space);
      return $"{first}:{string.Join(",", rest)}";
    }
  }
}
