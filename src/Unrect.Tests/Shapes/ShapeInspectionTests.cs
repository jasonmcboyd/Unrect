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
      Assert.Equal("Range", Range(b => b.Width).Description);
      Assert.Equal("Range(2, 3)", Range(2, 3, b => b.Width).Description);
    }

    [Fact]
    public void CompositesDescribeThemselvesStructurally()
    {
      Assert.Equal("VerticalFlow", VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Description);
      Assert.Equal("HorizontalFlow", HorizontalFlow(h => $"{h.Next(IntCell())}{h.Next(IntCell())}").Description);
      Assert.Equal("Overlay", Overlay(o => $"{o.Next(IntCell())}{o.Next(IntCell())}").Description);
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
    public void ALayoutCompositeHasNoChildrenToExpose()
    {
      // The cost of declaring children by calling Next: what a layout declares is knowable only by
      // running it. An empty Children would read as "leaf" to a renderer, so the marker says why.
      var flow = VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}");
      var overlay = Overlay(o => $"{o.Next(IntCell())}{o.Next(IntCell())}");

      Assert.Empty(flow.Children);
      Assert.Empty(overlay.Children);
      Assert.Equal(Reason(flow), Reason(overlay));
      Assert.Equal("declared by a cursor lambda; children are known only while it runs", Reason(flow));
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
    public void OnlyAnUnnamedWrapperIsTransparent()
    {
      // A wrapper the user wrote as part of a shape is not a level of the tree — until it is named,
      // at which point it claims a segment and says what it is.
      Assert.True(IntCell().Select(v => v + 1).IsTransparent);
      Assert.True(IntCell().Padded(1).IsTransparent);
      Assert.True(IntCell().Until(RowContaining("Total")).IsTransparent);

      Assert.False(IntCell().Select(v => v + 1).Named("named").IsTransparent);
      Assert.False(IntCell().Padded(1).Named("named").IsTransparent);
      Assert.False(IntCell().Until(RowContaining("Total")).Named("named").IsTransparent);

      // Shapes that are levels of the tree in their own right never are.
      Assert.False(IntCell().IsTransparent);
      Assert.False(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").IsTransparent);
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
      Assert.Null(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Placement.Area);
      Assert.Null(Repeat(IntCell()).Placement.Area);
      Assert.Null(IntCell().Select(v => v).Placement.Area);
    }

    // --- Walking a whole declaration without a space ---------------------------------------------------------------

    [Fact]
    public void AShapeTreeCanBeWalkedWithoutASpaceUntilItMeetsALayout()
    {
      // The dry-run traversal in miniature: no ISpace anywhere. It walks the wrappers and the
      // repeat happily, and stops where a layout composite is — reporting why rather than
      // pretending the layout is a leaf.
      var shape = Repeat(
        TableRows(r => r[0])
          .Named("rows")
          .Until(RowContaining("Total"))
          .Select(rows => rows.Count)
          .Named("block"))
        .Named("blocks");

      Assert.Equal(
        new[]
        {
          "'blocks' (Repeat)",
          "  'block' (Select)",
          "    Until",
          "      'rows' (TableRows)",
        },
        Describe(shape).ToArray());
    }

    [Fact]
    public void TheWalkStopsAtALayoutAndSaysWhy()
    {
      var shape = Repeat(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Named("block"));

      Assert.Equal(
        new[]
        {
          "Repeat",
          "  'block' (VerticalFlow) [opaque: declared by a cursor lambda; children are known only while it runs]",
        },
        Describe(shape).ToArray());
    }

    private static IEnumerable<string> Describe(IShape shape, int depth = 0)
    {
      var label = shape.Name is null ? shape.Description : $"'{shape.Name}' ({shape.Description})";
      var reason = Reason(shape);

      yield return new string(' ', depth * 2) + label + (reason is null ? string.Empty : $" [opaque: {reason}]");

      foreach (var child in shape.Children)
        foreach (var line in Describe(child, depth + 1))
          yield return line;
    }

    /// <summary>
    /// Why a composite's children are missing, or null when it has none to hide. The marker is
    /// internal to Unrect, which these tests can see; a renderer shipped in another assembly could
    /// not, and is the reason the marker exists at all rather than a member on <c>IShape</c>.
    /// </summary>
    private static string? Reason(IShape shape) => (shape as IOpaqueComposite)?.Reason;

    // --- Reuse ---------------------------------------------------------------------------------------------------

    [Fact]
    public void OneShapeCanBeAppliedToManyDifferentSpaces()
    {
      var shape = VerticalFlow(v => (v.Next(IntCell()), v.Next(Repeat(IntCell()))));

      Assert.Equal("1:2,3", Read(shape, Grid(new[,] { { 1 }, { 2 }, { 3 } })));
      Assert.Equal("9:8", Read(shape, Grid(new[,] { { 9 }, { 8 } })));
      Assert.Equal("4:5,6,7", Read(shape, Grid(new[,] { { 4 }, { 5 }, { 6 }, { 7 } })));
    }

    [Fact]
    public void OneShapeCanBeAppliedToManySpacesConcurrently()
    {
      // The context tree is built per Map call, so nothing is shared between concurrent runs.
      var shape = VerticalFlow(v => (v.Next(IntCell()), v.Next(Repeat(IntCell()))));

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
