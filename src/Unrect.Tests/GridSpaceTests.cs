using System;
using System.Globalization;

using Unrect.Core;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// <see cref="GridSpace"/> is the reference adapter: it fixes the index orientation every other
  /// space must honour (backing storage is [row, column]; a locator addresses [column, row]), and
  /// its <c>Create</c> overloads are where blankness is decided, at adaptation time.
  /// <para>
  /// Slicing is not here any more. A region of a grid is a <see cref="Plane{TSpace}"/>, which is
  /// arithmetic over a locator rather than anything the adapter does, and its laws are pinned once
  /// in <see cref="PlaneTests"/> rather than once per backend.
  /// </para>
  /// </summary>
  public class GridSpaceTests
  {
    // A 3-wide, 2-tall grid. Backing storage is row-major, so the outer initializer is rows.
    private static IValueSpace<string?> TextGrid() =>
      GridSpace.Create(new[,]
      {
        { "a", "b", "c" },
        { "d", "e", "f" },
      });

    // A 4-wide, 4-tall grid whose cell value is (row * 10 + column), so a misread coordinate is
    // immediately obvious in the failure message.
    private static IValueSpace<int> CoordinateGrid()
    {
      var values = new int[4, 4];

      for (int row = 0; row < 4; row++)
        for (int column = 0; column < 4; column++)
          values[row, column] = row * 10 + column;

      return GridSpace.Create(values);
    }

    // --- Orientation ----------------------------------------------------------------------------

    [Fact]
    public void Area_TakesWidthFromTheSecondArrayDimensionAndHeightFromTheFirst()
    {
      var space = TextGrid();

      Assert.Equal(3, space.Area.Size.Width);
      Assert.Equal(2, space.Area.Size.Height);
    }

    [Fact]
    public void Reads_AreColumnThenRow()
    {
      // The transposition this type exists to perform, stated over both surfaces it answers on: the
      // values' own and the canonical one. A grid that read them differently would be two spaces.
      var space = TextGrid();

      Assert.Equal("a", space.ValueAt(0, 0));
      Assert.Equal("b", space.ValueAt(1, 0));
      Assert.Equal("c", space.ValueAt(2, 0));
      Assert.Equal("d", space.ValueAt(0, 1));
      Assert.Equal("f", space.ValueAt(2, 1));

      Assert.Equal("b", space.AsText(1, 0));
      Assert.Equal("d", space.AsText(0, 1));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 2)]
    public void Reads_OutsideTheArea_Throw(int column, int row)
    {
      var space = TextGrid();

      Assert.Throws<OutOfBoundsException>(() => { _ = space.ValueAt(column, row); });
      Assert.Throws<OutOfBoundsException>(() => { _ = space.IsBlank(column, row); });
      Assert.Throws<OutOfBoundsException>(() => { _ = space.AsText(column, row); });
    }

    // --- Adaptation and blankness ---------------------------------------------------------------

    [Fact]
    public void Create_WithBlankPredicate_MapsMatchingValuesToBlank()
    {
      var space = GridSpace.Create(new[,] { { 1, 0 }, { 0, 2 } }, isBlank: v => v == 0);

      Assert.Equal(1, space.ValueAt(0, 0));
      Assert.True(space.IsBlank(1, 0));
      Assert.True(space.IsBlank(0, 1));
      Assert.Equal(2, space.ValueAt(1, 1));
    }

    [Fact]
    public void Create_WithBlankPredicate_MakesTheCellSayNothing()
    {
      // Blankness is a rule over the value rather than a replacement of it: the canonical surface
      // reports an empty cell — it says nothing and is not text — while the value the array holds is
      // still the value the array holds. A grid that answered AsText with "0" here would make
      // "is there anything in this cell" a question with two answers.
      var space = GridSpace.Create(new[,] { { 0 } }, isBlank: v => v == 0);

      Assert.True(space.IsBlank(0, 0));
      Assert.Null(space.AsText(0, 0));
      Assert.Equal(0, space.ValueAt(0, 0));
    }

    [Fact]
    public void Create_WithoutBlankPredicate_TreatsEveryValueAsPresent()
    {
      var space = GridSpace.Create(new[,] { { 0, 1 } });

      Assert.False(space.IsBlank(0, 0));
      Assert.Equal(0, space.ValueAt(0, 0));
      Assert.Equal("0", space.AsText(0, 0));
    }

    [Fact]
    public void Create_FromDoubles_MapsBlanksAndNumbers()
    {
      var space = GridSpace.Create(new[,] { { 1.5, double.NaN } }, isBlank: double.IsNaN);

      Assert.Equal(1.5, space.ValueAt(0, 0));
      Assert.True(space.IsBlank(1, 0));
    }

    [Fact]
    public void Create_FromStrings_TreatsNullAndEmptyAsBlank()
    {
      var space = GridSpace.Create(new string?[,] { { "x", "", null } });

      Assert.Equal("x", space.AsText(0, 0));
      Assert.True(space.IsBlank(1, 0));
      Assert.True(space.IsBlank(2, 0));
    }

    [Fact]
    public void Create_WithBothRules_AdaptsArbitraryValues()
    {
      // The general door, and the whole of what a source has to decide: which values are empty
      // cells, and what the rest say. A "-" is an empty cell, and a flag says what a sheet's would.
      var space = GridSpace.Create(
        new[,] { { "yes", "-", "no" } },
        isBlank: v => v == "-",
        asText: v => v == "yes" ? "TRUE" : "FALSE");

      Assert.Equal("TRUE", space.AsText(0, 0));
      Assert.True(space.IsBlank(1, 0));
      Assert.Equal("FALSE", space.AsText(2, 0));
    }

    [Fact]
    public void Create_WithValuesOfAnyType_RendersEachAsItsOwnKindImplies()
    {
      // The heterogeneous door, which is what lets a fixture be written as a literal: null and ""
      // are blank, a string says itself, and everything else renders the way a spreadsheet's
      // does rather than the way its CLR type's ToString happens to.
      var space = GridSpace.Create(new object?[,]
      {
        { "word", 42, 3.5, new DateTime(2026, 1, 15), true, null, "" },
      });

      Assert.Equal("word", space.AsText(0, 0));

      Assert.Equal("42", space.AsText(1, 0));
      Assert.Equal("3.5", space.AsText(2, 0));
      Assert.Equal("2026-01-15", space.AsText(3, 0));
      Assert.Equal("TRUE", space.AsText(4, 0));

      Assert.True(space.IsBlank(5, 0));
      Assert.True(space.IsBlank(6, 0));
    }

    // --- The value surface: what a homogeneous grid knows that a space does not ---------------------

    [Fact]
    public void AGridIsAValueSpaceAndItsPointsHandBackTheValueUnrendered()
    {
      // IValueSpace<T> is one member wide, and this is why it is worth having: the canonical four
      // answer ABOUT a cell, and this answers WITH it — the int, not "42". A declaration that wants
      // the value says so in its own type, and then Point<IValueSpace<T>>.Value() is there.
      IValueSpace<int> grid = GridSpace.Create(new[,] { { 1, 2 }, { 3, 0 } }, isBlank: v => v == 0);

      Assert.Equal(3, Plane<IValueSpace<int>>.Of(grid)[0, 1].Value());
      Assert.Equal(2, grid.ValueAt(1, 0));

      // A blank cell still HAS a value — blankness is the space's question, not this one's — so the
      // zero comes back rather than a null the type could not hold anyway.
      Assert.True(grid.IsBlank(1, 1));
      Assert.Equal(0, Plane<IValueSpace<int>>.Of(grid)[1, 1].Value());
    }

    [Fact]
    public void AndReadingAValueOutsideTheSpaceIsABoundsConditionLikeEveryOtherRead()
    {
      // OutOfBoundsException and not IndexOutOfRangeException: running off the edge of a space is a
      // statement about the data that a declaration may recover from, where an index bug is on the
      // engine's fault list and would make the overrun unrecoverable.
      IValueSpace<int> grid = GridSpace.Create(new[,] { { 1, 2 } }, isBlank: v => v == 0);

      Assert.Throws<OutOfBoundsException>(() => grid.ValueAt(2, 0));
      Assert.Throws<OutOfBoundsException>(() => grid.ValueAt(0, 1));
      Assert.Throws<OutOfBoundsException>(() => grid.ValueAt(-1, 0));
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(3.5, "3.5")]
    [InlineData(true, "TRUE")]
    [InlineData("word", "word")]
    public void AGridRendersEachTypeTheWayASpreadsheetWouldRatherThanTheWayToStringDoes(object value, string rendered)
    {
      // The rendering is per T, chosen once when the array becomes a space, and it is a SPREADSHEET's
      // rendering rather than the CLR's — TRUE, not True; an invariant decimal point, not the
      // current culture's. A declaration comparing a rendered cell against a literal is comparing
      // against this, so it is pinned per type rather than left to the default overload's judgement.
      var space = GridSpace.Create(new object?[,] { { value } });

      Assert.Equal(rendered, space.AsText(0, 0));
    }

    [Fact]
    public void Create_NeedsEveryRuleItWasNotGivenADefaultFor()
    {
      // Not a bounds condition: a grid with no rule for blankness could not answer the one question
      // its source has to answer, so it is the argument bug it looks like.
      var values = new[,] { { 1 } };

      Assert.Throws<ArgumentNullException>(
        () => GridSpace.Create(values, isBlank: null!, asText: v => v.ToString(CultureInfo.InvariantCulture)));
      Assert.Throws<ArgumentNullException>(
        () => GridSpace.Create(values, isBlank: _ => false, asText: null!));
      Assert.Throws<ArgumentNullException>(() => GridSpace.Create((int[,])null!));
    }
  }
}
