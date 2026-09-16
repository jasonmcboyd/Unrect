using System;

using Unrect.Core;
using Unrect.Interactive;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// Reaching one cell by hand — <c>space.At(2, 3)</c> and <c>space.At("C4")</c>, the exploratory
  /// half of the vocabulary.
  /// <para>
  /// There is almost nothing to <c>At</c>, which is the point: it mints the plane of the whole space
  /// and indexes it, so the coordinate is checked by the same code that checks every coordinate a
  /// declaration produces, and the point it hands back is the one the engine would have handed back.
  /// The tests below are about that identity and about where the two refusals fall — an address that
  /// is not an address is an argument bug, an address off the edge of the sheet is a bounds
  /// condition, and the boundary between them is the whole of the second overload's contract.
  /// </para>
  /// </summary>
  public class AddressingTests
  {
    /// <summary>Four wide, five tall; every cell says its own coordinate as <c>row * 10 + column + 1</c>.</summary>
    private static ISheetCells Sheet() => CoordinateGrid(4, 5);

    // --- At(column, row) ------------------------------------------------------------------------------

    [Fact]
    public void AnAddressedCellIsThePointThePlaneOfTheWholeSpaceMints()
    {
      // The identity that makes At sugar rather than a second way of reaching a cell: it is exactly
      // the root plane's indexer, so anything true of a point a declaration was handed is true of
      // this one. Asymmetric coordinates throughout, because a transposition is invisible on the
      // diagonal.
      var sheet = Sheet();

      var point = sheet.At(2, 3);

      Assert.Equal(Plane<ISheetCells>.Of(sheet)[2, 3], point);
      Assert.Same(sheet, point.Space);
      Assert.Equal(2, point.Column);
      Assert.Equal(3, point.Row);
      Assert.Equal("33", point.AsText());      // row * 10 + column + 1
    }

    [Fact]
    public void AnAddressIsAlwaysInTheSpacesOwnCoordinatesAndNeverInSomeRegionsFrame()
    {
      // The half that would go wrong if At ever grew a frame of its own. A point minted through a
      // slice names an absolute cell, so the point THIS mints must be equal to it — same space, same
      // column, same row — rather than merely reading the same value.
      var sheet = Sheet();

      var throughASlice = Plane<ISheetCells>.Of(sheet).Slice(new Offset(1, 1), new Area(3, 3))[1, 2];

      Assert.Equal(throughASlice, sheet.At(2, 3));
      Assert.Equal(throughASlice.GetHashCode(), sheet.At(2, 3).GetHashCode());

      // Non-vacuous, and specifically against the transposed coordinate: (3, 2) is where a locator
      // that had swapped its components would have landed.
      Assert.NotEqual(sheet.At(3, 2), sheet.At(2, 3));
    }

    [Fact]
    public void ACoordinateOffTheEdgeOfTheSpaceIsAnOverrun()
    {
      // Running off a space is a statement about the DATA, so it arrives as the bounds condition a
      // declaration recovers from — the same refusal at the same edge whether a script asked or a
      // declaration did. Both edges and both negatives, because each is a separate comparison.
      var sheet = Sheet();      // four wide, five tall

      Assert.Throws<OutOfBoundsException>(() => sheet.At(4, 0));
      Assert.Throws<OutOfBoundsException>(() => sheet.At(0, 5));
      Assert.Throws<OutOfBoundsException>(() => sheet.At(-1, 0));
      Assert.Throws<OutOfBoundsException>(() => sheet.At(0, -1));

      // ...and the far corner, one cell inside each edge, is a real cell: the refusals above are
      // off-by-one from something that works.
      Assert.Equal("44", sheet.At(3, 4).AsText());
    }

    [Fact]
    public void AddressingNothingAtAllIsAnArgumentBug()
    {
      // A declaration cannot recover from having no document, so this is never a bounds condition.
      Assert.Throws<ArgumentNullException>(() => ((ISheetCells)null!).At(0, 0));
      Assert.Throws<ArgumentNullException>(() => ((ISheetCells)null!).At("A1"));
    }

    [Fact]
    public void AnySpaceAtAllCanBeAddressed()
    {
      // The constraint is ISpace and not ISheetCells: addressing a cell is not a spreadsheet's
      // privilege, and a script poking at an in-memory grid reaches for the same word. If this ever
      // stops compiling, the sugar has quietly become Excel-only.
      var grid = GridSpace.Create(new[,] { { 1, 2, 3 }, { 4, 5, 6 } });

      var point = grid.At(2, 1);

      Assert.Equal(2, point.Column);
      Assert.Equal(1, point.Row);
      Assert.Equal("6", point.AsText());
      Assert.Throws<OutOfBoundsException>(() => grid.At(3, 0));
    }

    // --- At("B4") -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("B4", 1, 3)]
    [InlineData("AB7", 27, 6)]
    public void AnA1AddressNamesTheCellTheFileWouldCallIt(string address, int column, int row)
    {
      // The file's 1-based, letter-columned spelling against the space's 0-based pair. AB7 is the
      // case worth having: column letters are bijective base-26, so AB is 27 and not 28.
      var sheet = CoordinateGrid(30, 8);

      Assert.Equal(sheet.At(column, row), sheet.At(address));
    }

    [Fact]
    public void TheLettersAreCaseInsensitive()
    {
      // Nothing about a sheet promises the case of an address, and a script is being typed by hand.
      var sheet = CoordinateGrid(30, 8);

      Assert.Equal(sheet.At("AB7"), sheet.At("ab7"));
    }

    [Theory]
    [InlineData("$A$1")]        // the locked spelling: a formula's business, and nothing is being copied here
    [InlineData("nonsense")]    // more letters than any column has
    [InlineData("")]            // nothing at all
    [InlineData("A")]           // a column with no row
    [InlineData("1")]           // a row with no column
    [InlineData("A0")]          // rows are 1-based in the file, so there is no row 0
    [InlineData("A 1")]         // a space in the middle is not whitespace to trim, it is a different word
    public void SomethingThatIsNotAnAddressIsAnArgumentBug(string address)
    {
      // The first half of the boundary. A string that does not name a cell is a typo in the SCRIPT —
      // nothing about the sheet could have produced it, and no amount of reading would make it name
      // a cell — so it is an argument bug, named after the parameter, quoting what was passed.
      var sheet = Sheet();

      var failure = Assert.Throws<ArgumentException>(() => sheet.At(address));

      Assert.Equal("address", failure.ParamName);
      Assert.StartsWith($"'{address}' is not an A1 cell address.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnAddressThatIsNoAddressAtAllIsTheNullOne()
      => Assert.Equal("address", Assert.Throws<ArgumentNullException>(() => Sheet().At((string)null!)).ParamName);

    [Fact]
    public void AWellFormedAddressPastTheEdgeOfTheSheetIsAnOverrunAndNotAnArgumentBug()
    {
      // The other half of the boundary, and the one that is easy to get wrong. "A9999" is a perfectly
      // good cell address — it is only this sheet that is five rows tall — so the refusal is about the
      // DATA and must arrive as the bounds condition, absorbable and recoverable, rather than as the
      // fault a malformed string gets. Getting the two the same way round would make a typo
      // recoverable, or make "the sheet is shorter than I thought" unrecoverable.
      var sheet = Sheet();

      Assert.Throws<OutOfBoundsException>(() => sheet.At("A9999"));
      Assert.Throws<OutOfBoundsException>(() => sheet.At("E1"));

      // The two refusals sit one character apart on the same sheet, which is the whole boundary:
      // D5 is the far corner and reads, E1 is off the edge, and "E" alone is not an address.
      Assert.Equal("44", sheet.At("D5").AsText());
      Assert.Throws<ArgumentException>(() => sheet.At("E"));
    }
  }
}
