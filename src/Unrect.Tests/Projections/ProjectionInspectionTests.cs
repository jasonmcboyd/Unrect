using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A projection is an inspectable value, not a closure over a file: its name, description and
  /// placement can all be read without ever handing it a space, and so can its children — a
  /// layout's included, since its lambda ran once when it was declared. The one node whose child
  /// is built from data, the bind rung, says so rather than passing for a leaf, through
  /// <c>Opacity</c>.
  /// <para>
  /// What is left readable is what makes the wave-3 diagnostics (dry runs, traces, capability
  /// checks) possible, and it is what makes one projection safe to apply to many spaces at once.
  /// </para>
  /// </summary>
  public class ProjectionInspectionTests
  {
    // --- Descriptions ------------------------------------------------------------------------------------

    [Fact]
    public void LeavesDescribeThemselvesStructurally()
    {
      // The typed leaves and the labelled block, described by the factory the user typed.
      Assert.Equal("Text", Text().Description);
      Assert.Equal("Decimal", Decimal().Description);
      Assert.Equal("Integer", Integer().Description);
      Assert.Equal("Double", Double().Description);
      Assert.Equal("Date", Date().Description);
      Assert.Equal("Boolean", Boolean().Description);
      Assert.Equal("Fields", Fields(Field("EIN")).Description);
      Assert.Equal("Caption(\"Total\")", Caption("Total").Description);

      Assert.Equal("Point", Point().Description);
      Assert.Equal("AsText", AsText().Description);
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
      Assert.Equal("VerticalRepeat", VerticalRepeat(IntCell()).Description);
      Assert.Equal("HorizontalRepeat", HorizontalRepeat(IntCell()).Description);
      Assert.Equal("Select", IntCell().Select(v => v + 1).Description);
      Assert.Equal("Table", Table(t => t.RowCount).Description);
      Assert.Equal("Table", Table(r => r[0]).Description);
    }

    [Fact]
    public void ANameDoesNotReplaceTheDescription()
    {
      var projection = IntCell().Named("report id");

      Assert.Equal("report id", projection.Name);
      Assert.Equal("Integer", projection.Description);
    }

    [Fact]
    public void AnUnnamedProjectionHasNoName()
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
    public void ALayoutCompositeExposesItsChildrenComplete()
    {
      // Declaring children by calling Next costs nothing here: the lambda ran once, at declaration,
      // so what a layout declares is complete before any space exists and it hides nothing.
      var flow = VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}");
      var overlay = Overlay(o => $"{o.Next(IntCell())}{o.Next(IntCell())}");

      Assert.Equal(2, flow.Children.Count);
      Assert.Equal(2, overlay.Children.Count);
      Assert.Null(Reason(flow));
      Assert.Null(Reason(overlay));
    }

    [Fact]
    public void ARepeatExposesItsItem()
    {
      var item = IntCell().Named("item");

      Assert.Same(item, Assert.Single(VerticalRepeat(item).Children).Definition);
    }

    [Fact]
    public void AnEdgeCarriesTheIdentifierTheDeclarationWrote()
    {
      // The use site is declaration data: what a failure path would call the child is readable off
      // the edge, without a space and without running anything.
      var block = IntCell();

      var hoisted = Assert.Single(VerticalRepeat(block).Children);
      var inline = Assert.Single(VerticalRepeat(IntCell()).Children);

      Assert.Same(block, hoisted.Definition);
      Assert.Equal("block", hoisted.Site.Name);
      Assert.Null(hoisted.Site.Ordinal);

      Assert.Null(inline.Site.Name);
      Assert.Null(inline.Site.Ordinal);
    }

    [Fact]
    public void ATableExposesItsRowProjection()
    {
      // The slot form is a composite in the same sense a repeat is — it applies one declaration to
      // each band — so it shows the declaration, and a renderer can walk into a record without
      // running anything.
      var row = IntCell().Named("row");

      Assert.Same(row, Assert.Single(Table(0, row).Children).Definition);
      Assert.Same(row, Assert.Single(Table(1, row).Children).Definition);
      Assert.Equal("row", Assert.Single(Table(1, row).Children).Site.Name);
    }

    [Theory]
    [InlineData("Table(view lambda)")]
    [InlineData("Table(row lambda)")]
    [InlineData("Table()")]
    public void ButTheLambdaRungsHaveNoneToShow(string rung)
    {
      // What a lambda rung reads is knowable only by running it, so it is a leaf to tooling — as it
      // has always been. This is the contrast that gives the fact above its meaning: the two forms
      // of table differ in exactly this, and nothing else.
      IProjectionDefinition projection = rung switch
      {
        "Table(view lambda)" => Table(table => table.RowCount),
        "Table(row lambda)" => Table(row => row.Index),
        "Table()" => Table(),

        _ => throw new System.ArgumentOutOfRangeException(nameof(rung), rung, "No such rung."),
      };

      Assert.Empty(projection.Children);

      // ...and a lambda rung does not claim to be hiding anything: it has no opacity marker,
      // because with a lambda in the slot there was never a child to declare.
      Assert.Null(Reason(projection));
    }

    [Fact]
    public void TheBindRungSaysWhyItsChildIsMissing()
    {
      // The one shipped node whose child is manufactured from data: the row projection exists only
      // once a header has been read, so an empty Children would be a lie and the rung says why. The
      // reflective Table<T> is the bind rung underneath, so it says the same.
      var bound = Table(1, (LabelMap labels) => IntCell());
      var reflected = Table<Entry>();

      Assert.Empty(bound.Children);
      Assert.Equal("the row projection is built from the header's captions; it is known only once a header is read", Reason(bound));
      Assert.Equal(Reason(bound), Reason(reflected));
    }

    /// <summary>What the typed rung binds a row to — a shape for the theory above and nothing more.</summary>
    public record Entry(string Client, int Amount);

    [Fact]
    public void ATableWithARowProjectionCanBeWalkedIntoWithoutASpace()
    {
      // The dry-run traversal, all the way down: the walk enters the record and its layout, and
      // reaches the leaves, with no space anywhere.
      var allocation = Overlay(o => $"{o.Next(IntCell())}{o.Next(IntCell())}").Named("allocation");

      Assert.Equal(
        new[]
        {
          "Table",
          "  'allocation' (Overlay)",
          "    Integer",
          "    Integer",
        },
        Describe(Table(0, allocation)).ToArray());
    }

    [Fact]
    public void ASelectExposesTheProjectionItWraps()
    {
      var inner = IntCell().Named("inner");

      Assert.Same(inner, Assert.Single(inner.Select(v => v + 1).Children).Definition);
    }

    // --- Wrappers ---------------------------------------------------------------------------------------

    [Fact]
    public void AWrapperSaysSoWhetherOrNotItIsNamed()
    {
      // A structural fact about the projection, not a rendering decision: naming a wrapper changes
      // whether a path shows it (the renderer's rule), never what it is.
      Assert.True(IntCell().Select(v => v + 1).IsWrapper);
      Assert.True(IntCell().Padded(1).IsWrapper);
      Assert.True(Until(RowContaining("Total")).Of(IntCell()).IsWrapper);

      Assert.True(IntCell().Select(v => v + 1).Named("named").IsWrapper);
      Assert.True(IntCell().Padded(1).Named("named").IsWrapper);
      Assert.True(Until(RowContaining("Total")).Of(IntCell()).Named("named").IsWrapper);

      // Projections that are levels of the tree in their own right never are.
      Assert.False(IntCell().IsWrapper);
      Assert.False(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").IsWrapper);
      Assert.False(VerticalRepeat(IntCell()).IsWrapper);
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
      Assert.Null(VerticalRepeat(IntCell()).Placement.Area);
      Assert.Null(IntCell().Select(v => v).Placement.Area);
    }

    // --- Walking a whole declaration without a space ---------------------------------------------------------------

    [Fact]
    public void AProjectionTreeCanBeWalkedWithoutASpaceUntilItMeetsALayout()
    {
      // The dry-run traversal in miniature: no ICellSpace anywhere. It walks the wrappers and the
      // repeat happily, and stops where a layout composite is — reporting why rather than
      // pretending the layout is a leaf.
      var projection = VerticalRepeat(
        Until(RowContaining("Total")).Of(Table(r => r[0])
          .Named("rows"))
          .Select(rows => rows.Count)
          .Named("block"))
        .Named("blocks");

      Assert.Equal(
        new[]
        {
          "'blocks' (VerticalRepeat)",
          "  'block' (Select)",
          "    Until",
          "      'rows' (Table)",
        },
        Describe(projection).ToArray());
    }

    [Fact]
    public void TheWalkGoesThroughALayout()
    {
      var projection = VerticalRepeat(VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}").Named("block"));

      Assert.Equal(
        new[]
        {
          "VerticalRepeat",
          "  'block' (VerticalFlow)",
          "    Integer",
          "    Integer",
        },
        Describe(projection).ToArray());
    }

    private static IEnumerable<string> Describe(IProjectionDefinition projection, int depth = 0)
    {
      var label = projection.Name is null ? projection.Description : $"'{projection.Name}' ({projection.Description})";
      var reason = Reason(projection);

      yield return new string(' ', depth * 2) + label + (reason is null ? string.Empty : $" [opaque: {reason}]");

      foreach (var child in projection.Children)
        foreach (var line in Describe(child.Definition, depth + 1))
          yield return line;
    }

    /// <summary>
    /// Why a composite's children are missing, or null when it has none to hide — read off the
    /// public face, exactly as a renderer shipped in another assembly would read it.
    /// </summary>
    private static string? Reason(IProjectionDefinition projection) => projection.Opacity;

    // --- Reuse ---------------------------------------------------------------------------------------------------

    [Fact]
    public void OneProjectionCanBeAppliedToManyDifferentSpaces()
    {
      var projection = VerticalFlow(v => (v.Next(IntCell()), v.Next(VerticalRepeat(IntCell()))));

      Assert.Equal("1:2,3", Read(projection, Grid(new[,] { { 1 }, { 2 }, { 3 } })));
      Assert.Equal("9:8", Read(projection, Grid(new[,] { { 9 }, { 8 } })));
      Assert.Equal("4:5,6,7", Read(projection, Grid(new[,] { { 4 }, { 5 }, { 6 }, { 7 } })));
    }

    [Fact]
    public void OneProjectionCanBeAppliedToManySpacesConcurrently()
    {
      // The context tree is built per Map call, so nothing is shared between concurrent runs.
      var projection = VerticalFlow(v => (v.Next(IntCell()), v.Next(VerticalRepeat(IntCell()))));

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1 }, { seed + 2 }, { seed + 3 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = Read(projection, spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}:{index + 2},{index + 3}", results[index]);
    }

    [Fact]
    public void MappingDoesNotMutateTheProjection()
    {
      var projection = Down(1).Of(IntCell().Named("value"));

      projection.Map(Grid(new[,] { { 1 }, { 2 } }));

      Assert.Equal("value", projection.Name);
      Assert.NotNull(projection.Placement.Area);
      Assert.Equal(2, projection.Map(Grid(new[,] { { 1 }, { 2 } })));
    }

    /// <summary>Renders a result as text so array identity never enters the comparison.</summary>
    private static string Read(IProjectionDefinition<ICellSpace, (int, IReadOnlyList<int>)> projection, ICellSpace space)
    {
      var (first, rest) = projection.Map(space);
      return $"{first}:{string.Join(",", rest)}";
    }
  }
}
