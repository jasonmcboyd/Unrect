using System;

using Unrect.Core;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// What a point is: an address, not a value. It names a cell of a space in that space's own root
  /// coordinates, forwards every question to the space, and compares by where it points rather than
  /// by what it finds there.
  /// <para>
  /// The last of those is the one place a reader can be misled, because the value model a point
  /// stands in front of compares the other way — two blank cells are one <c>Cell</c>, and two
  /// blank cells are two points. Each half of that is pinned below.
  /// </para>
  /// </summary>
  public class PointTests
  {
    /// <summary>One cell of each kind worth telling apart: a label, a blank, a number, an error.</summary>
    private static ICellSpace Kinds() => SheetGrid.Of(
      new object?[,]
      {
        { "Total", null },
        { 42, Cell.OfError(CellError.DivisionByZero) },
      });

    /// <summary>
    /// The cell at <paramref name="column"/>, <paramref name="row"/>, minted by hand: the locator
    /// that will mint them does not exist yet, and these laws are the point's own either way.
    /// </summary>
    private static Point<ICellSpace> At(ICellSpace space, int column, int row)
      => new Point<ICellSpace>(space, column, row);

    [Fact]
    public void APointNamesACellOfOneSpace()
    {
      var space = Kinds();

      var point = At(space, 1, 1);

      Assert.Same(space, point.Space);
      Assert.Equal(1, point.Column);
      Assert.Equal(1, point.Row);
    }

    [Fact]
    public void APointReadsWhateverItsSpaceReads()
    {
      var space = Kinds();

      Assert.True(At(space, 0, 0).IsText);
      Assert.True(At(space, 0, 0).HasValue);
      Assert.Equal("Total", At(space, 0, 0).AsText());

      Assert.True(At(space, 1, 0).IsBlank);
      Assert.False(At(space, 1, 0).HasValue);
      Assert.False(At(space, 1, 0).IsText);
      Assert.Null(At(space, 1, 0).AsText());

      // A number and an error both say something, and neither says it as text — the distinction
      // every text matcher turns on.
      Assert.False(At(space, 0, 1).IsText);
      Assert.Equal("42", At(space, 0, 1).AsText());

      Assert.False(At(space, 1, 1).IsText);
      Assert.False(At(space, 1, 1).IsBlank);
      Assert.Equal("#DIV/0!", At(space, 1, 1).AsText());
    }

    [Fact]
    public void APointOffTheEdgeIsMadeWithoutComplaintAndRefusesWhenItIsRead()
    {
      // The constructor trusts its caller. The only thing it could check against is the space's
      // extent, and asking a space how tall it is can settle a boundary a declaration was still
      // discovering — so the refusal comes from the space, at the read, and it is still the bounds
      // condition a declaration may recover from rather than a bug in the reading code.
      var outside = At(Kinds(), 5, 5);

      Assert.Equal(5, outside.Column);
      Assert.Throws<OutOfBoundsException>(() => { _ = outside.IsBlank; });
      Assert.Throws<OutOfBoundsException>(() => { _ = outside.IsText; });
      Assert.Throws<OutOfBoundsException>(() => { _ = outside.AsText(); });
    }

    [Fact]
    public void APointWithNoSpaceIsStillAnAddress()
    {
      // default(Point<T>) is unavoidable for a struct, so it is stated rather than guarded against:
      // it compares, hashes and prints like any other point, and has nothing to read.
      Point<ICellSpace> nowhere = default;

      Assert.Equal("(0,0)", nowhere.ToString());
      Assert.Equal(nowhere, default(Point<ICellSpace>));
      Assert.Equal(nowhere.GetHashCode(), default(Point<ICellSpace>).GetHashCode());
      Assert.Throws<NullReferenceException>(() => { _ = nowhere.IsBlank; });
    }

    // --- Address equality ---------------------------------------------------------------------------

    [Fact]
    public void TwoPointsAreEqualWhenTheyNameTheSameCellOfTheSameSpace()
    {
      var space = Kinds();

      Assert.Equal(At(space, 1, 1), At(space, 1, 1));
      Assert.True(At(space, 1, 1) == At(space, 1, 1));
      Assert.False(At(space, 1, 1) != At(space, 1, 1));
      Assert.True(At(space, 1, 1).Equals((object)At(space, 1, 1)));
      Assert.Equal(At(space, 1, 1).GetHashCode(), At(space, 1, 1).GetHashCode());
    }

    [Fact]
    public void APointIsNotEqualToOneNamingAnotherCell()
    {
      var space = Kinds();

      Assert.NotEqual(At(space, 0, 1), At(space, 1, 1));
      Assert.NotEqual(At(space, 1, 0), At(space, 1, 1));
      Assert.True(At(space, 0, 1) != At(space, 1, 1));
    }

    [Fact]
    public void CellsThatSayTheSameThingInDifferentSpacesAreStillDifferentAddresses()
    {
      // Equality is about the place, never about the reading. Two grids built from the same
      // literals hold the same cell at (0, 0) and are still two different places.
      var first = Kinds();
      var second = Kinds();

      Assert.Equal(At(first, 0, 0).AsText(), At(second, 0, 0).AsText());
      Assert.NotEqual(At(first, 0, 0), At(second, 0, 0));
      Assert.True(At(first, 0, 0) != At(second, 0, 0));
    }

    [Fact]
    public void TwoBlankCellsAreTwoAddresses()
    {
      // The other direction of the same rule, and the one a reader coming from the value model is
      // most likely to trip over: nothing distinguishes two blank values, and everything
      // distinguishes two blank cells.
      var space = SheetGrid.Of(new object?[,] { { null, null } });

      Assert.True(At(space, 0, 0).IsBlank);
      Assert.True(At(space, 1, 0).IsBlank);
      Assert.NotEqual(At(space, 0, 0), At(space, 1, 0));
    }

    [Fact]
    public void ASpaceThatCallsItselfEqualToEveryOtherStillMintsItsOwnAddresses()
    {
      // The law above is only worth something if it is the SPACE's identity being compared and not
      // its opinion of itself. This one says every other space is equal to it and hashes to the
      // same number, which is exactly what a point comparing its space with Equals would fall for.
      var first = new AgreeableSpace();
      var second = new AgreeableSpace();

      Assert.Equal<object>(first, second);
      Assert.Equal(first.GetHashCode(), second.GetHashCode());

      Assert.NotEqual(new Point<AgreeableSpace>(first, 0, 0), new Point<AgreeableSpace>(second, 0, 0));

      // The hash INequality is an assertion and not a hope, which is worth saying because unequal
      // hashes are normally something a test may not demand. Here it is decidable: the two spaces
      // have different RuntimeHelpers.GetHashCode values (that is what a reference hash is), the
      // coordinates are identical, and the mix is `first * K + second` with K odd — which is a
      // bijection on int for any fixed second. Distinct first terms therefore give distinct
      // results, with no collision to allow for.
      Assert.NotEqual(
        new Point<AgreeableSpace>(first, 0, 0).GetHashCode(),
        new Point<AgreeableSpace>(second, 0, 0).GetHashCode());
    }

    /// <summary>A space with no opinions about its cells and a very strong one about itself.</summary>
    private sealed class AgreeableSpace : ISpace
    {
      public Area Area => new Area(1, 1);

      public override bool Equals(object? obj) => obj is AgreeableSpace;

      public override int GetHashCode() => 1;

      public bool IsBlank(int column, int row) => true;

      public bool TryGetTextAt(int column, int row, out string value, out CellProblem? problem)
      {
        value = null!;
        problem = null;
        return false;
      }

      public string? AsText(int column, int row) => null;
    }

    [Fact]
    public void APointRendersItsAddressInItsSpacesOwnCoordinates()
    {
      // Diagnostics only: a point knows where it sits in the space it names and not where that
      // space sits in a workbook, so this is never an A1 address.
      var space = Kinds();

      Assert.Equal("(1,1)", At(space, 1, 1).ToString());
      Assert.Equal("(0,0)", At(space, 0, 0).ToString());
    }
  }
}
