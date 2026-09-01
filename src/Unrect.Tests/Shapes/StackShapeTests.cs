using System;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// Vertical and Horizontal: a cursor walk in declaration order that consumes space along its own
  /// axis only, sizes itself from what its children consumed, and treats a child that does not fit
  /// as a hard error rather than a stopping condition.
  /// </summary>
  public class StackShapeTests
  {
    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    private static ISpace Ladder(int height)
    {
      var values = new int[height, 1];

      for (var row = 0; row < height; row++)
        values[row, 0] = row + 1;

      return Grid(values);
    }

    // --- Order --------------------------------------------------------------------------------------

    [Fact]
    public void Vertical_ProjectsChildrenTopToBottomInDeclarationOrder()
    {
      Assert.Equal((1, 2, 3), Vertical(IntCell(), IntCell(), IntCell()).Map(Ladder(3)));
    }

    [Fact]
    public void Horizontal_ProjectsChildrenLeftToRightInDeclarationOrder()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal((1, 2, 3), Horizontal(IntCell(), IntCell(), IntCell()).Map(space));
    }

    // --- Axis-only consumption ------------------------------------------------------------------------

    [Fact]
    public void Vertical_ConsumesAChildsOffsetOnTheVerticalAxisOnly()
    {
      // The first child is inset one column. The second child starts back at column 0, one row down.
      var space = Grid(new[,] { { 1, 2 }, { 3, 4 } });

      Assert.Equal((2, 3), Vertical(IntCell().Right(1), IntCell()).Map(space));
    }

    [Fact]
    public void Horizontal_ConsumesAChildsOffsetOnTheHorizontalAxisOnly()
    {
      // The first child is inset one row. The second child starts back at row 0, one column across.
      var space = Grid(new[,] { { 1, 2 }, { 3, 4 } });

      Assert.Equal((3, 2), Horizontal(IntCell().Down(1), IntCell()).Map(space));
    }

    // --- Derived extent -------------------------------------------------------------------------------

    [Fact]
    public void Vertical_SizesItselfFromItsChildren()
    {
      // Along the axis the children accumulate; across it, the widest child wins.
      var space = Grid(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

      var applied = Vertical(Row(2, s => s.Count), Row(3, s => s.Count)).Apply(space);

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void Horizontal_SizesItselfFromItsChildren()
    {
      var space = Grid(new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } });

      var applied = Horizontal(Column(2, s => s.Count), Column(3, s => s.Count)).Apply(space);

      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void Vertical_IncludesAChildsOffsetInWhatItConsumes()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 } });

      var applied = Vertical(IntCell().Down(1), IntCell()).Apply(space);

      Assert.Equal((1, 2), applied.Value);
      Assert.Equal(3, applied.Consumed.Height);
    }

    // --- Declared areas are consumed in full ------------------------------------------------------------

    [Fact]
    public void AChildWithADeclaredArea_ConsumesItInFullEvenWhenItsContentUsedLess()
    {
      // The inner stack only needs two rows, but it was declared three tall — so the next sibling
      // starts after the third row, not the second. A declared extent is a claim on the space.
      var shape = Vertical(
        Vertical(IntCell(), IntCell()).Sized(AreaStrategies.ExplicitArea(1, 3)),
        IntCell());

      Assert.Equal(((1, 2), 4), shape.Map(Ladder(4)));
    }

    [Fact]
    public void AChildWithoutADeclaredArea_ConsumesOnlyWhatItsContentUsed()
    {
      var shape = Vertical(
        Vertical(IntCell(), IntCell()),
        IntCell());

      Assert.Equal(((1, 2), 3), shape.Map(Ladder(4)));
    }

    // --- Misfit is an error ------------------------------------------------------------------------------

    [Fact]
    public void Vertical_WhenAChildDoesNotFit_Throws()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Vertical(Cells(1, 2, b => b.Height), Cells(1, 2, b => b.Height)).Map(space));

      Assert.Contains("an extent of 1x2 does not fit here", failure.Message);
    }

    [Fact]
    public void Vertical_WhenAChildRunsOutOfSpaceEntirely_Throws()
    {
      Assert.Throws<ShapeException>(() => Vertical(IntCell(), IntCell(), IntCell()).Map(Ladder(2)));
    }

    // --- Select --------------------------------------------------------------------------------------------

    [Fact]
    public void Select_ReceivesTheChildrenInDeclarationOrder()
    {
      var result = Vertical(IntCell(), IntCell(), IntCell())
        .Select((first, second, third) => new[] { first, second, third })
        .Map(Ladder(3));

      Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void Select_OverASingleValuedShape_TransformsIt()
    {
      Assert.Equal("1", IntCell().Select(v => v.ToString()).Map(Ladder(1)));
    }

    // --- Arity ---------------------------------------------------------------------------------------------

    [Fact]
    public void Vertical_SupportsEightChildren()
    {
      var shape = Vertical(
        IntCell(), IntCell(), IntCell(), IntCell(),
        IntCell(), IntCell(), IntCell(), IntCell());

      Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), shape.Map(Ladder(8)));
    }

    [Fact]
    public void Horizontal_SupportsEightChildren()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4, 5, 6, 7, 8 } });

      var shape = Horizontal(
        IntCell(), IntCell(), IntCell(), IntCell(),
        IntCell(), IntCell(), IntCell(), IntCell());

      Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), shape.Map(space));
    }

    [Fact]
    public void BeyondEightChildren_NestAStackInsideAStack()
    {
      // Eight is where ValueTuple stops; a stack is a shape, so nesting is the answer rather than a
      // ninth overload.
      var shape = Vertical(
        Vertical(
          IntCell(), IntCell(), IntCell(), IntCell(),
          IntCell(), IntCell(), IntCell(), IntCell()),
        IntCell());

      var (first8, ninth) = shape.Map(Ladder(9));

      Assert.Equal((1, 2, 3, 4, 5, 6, 7, 8), first8);
      Assert.Equal(9, ninth);
    }

    // --- Construction guards -----------------------------------------------------------------------------------

    [Fact]
    public void AStackWithANullChild_IsRejectedAtConstruction()
    {
      // The factory blames the parameter the caller actually wrote, so the message names the
      // position in the declaration rather than an internal children array.
      var second = Assert.Throws<ArgumentNullException>(() => Vertical(IntCell(), (IShape<int>)null!));
      var first = Assert.Throws<ArgumentNullException>(() => Horizontal((IShape<int>)null!, IntCell()));

      Assert.Equal("second", second.ParamName);
      Assert.Equal("first", first.ParamName);
    }

    [Fact]
    public void AStackIsAShapeAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 } });

      var shape = Vertical(IntCell(), IntCell()).AfterBlankRows().Named("block");

      Assert.Equal((1, 2), shape.Map(space));
      Assert.Equal("block", shape.Name);
    }
  }
}
