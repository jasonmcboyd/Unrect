using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Core.ISpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The one reading every space answers: the text a cell HOLDS, as distinct from what it says.
  /// <para>
  /// The space's contract is one member, <see cref="ISpace.TryGetTextAt"/>, and everything else is
  /// derived from it once — <c>IsText</c>, <c>TryGetText</c>, the asserting <c>Text()</c> read and
  /// the <c>Text()</c> leaf — so none of them can disagree with it. A space words its own refusal
  /// where it has something to say; where it says nothing, the reader is still told what was
  /// expected and what the cell says instead.
  /// </para>
  /// </summary>
  public class HeldTextTests
  {
    /// <summary>A grid with no kinds at all: words are text, numbers are rendered.</summary>
    private static ISpace Plain()
      => GridSpace.Create(new object?[,] { { "Total", 42, null } });

    private static Point<ISpace> At(ISpace space, int column) => Plane<ISpace>.Of(space)[column, 0];

    [Fact]
    public void IsTextIsExactlyWhetherTheReadWouldSucceed()
    {
      var space = Plain();

      for (var column = 0; column < 3; column++)
      {
        var point = At(space, column);

        Assert.Equal(point.TryGetText(out _), point.IsText);
        Assert.Equal(point.TryGetText(out _, out _), point.IsText);
      }

      Assert.True(At(space, 0).IsText);
      Assert.False(At(space, 1).IsText);
      Assert.False(At(space, 2).IsText);
    }

    [Fact]
    public void ACellThatSaysSomethingDoesNotTherebyHoldIt()
    {
      var number = At(Plain(), 1);

      Assert.Equal("42", number.AsText());
      Assert.False(number.TryGetText(out _));
    }

    [Fact]
    public void ASuccessfulReadCarriesNoProblem()
    {
      Assert.True(At(Plain(), 0).TryGetText(out var text, out var problem));
      Assert.Equal("Total", text);
      Assert.Null(problem);
    }

    [Fact]
    public void ASpaceWithNothingToSayStillGetsASentence()
    {
      // The floor: what was expected, and what the cell says instead — the two things every space
      // can always answer. It claims nothing about what the cell IS.
      var space = Plain();

      Assert.False(At(space, 1).TryGetText(out _, out var number));
      Assert.Equal("expected Text at B1; the cell says '42'", number!.Value.Render("B1"));

      Assert.False(At(space, 2).TryGetText(out _, out var blank));
      Assert.Equal("expected Text at C1; the cell is blank", blank!.Value.Render("C1"));
    }

    [Fact]
    public void TheLeafReadsHeldTextOverAnySpace()
    {
      Assert.Equal("Total", Text().Map(Plain()));

      var failure = Assert.Throws<ProjectionException>(() => Right(1).Of(Text()).Map(Plain()));

      Assert.Equal("expected Text at B1; the cell says '42'", Problem(failure));
      Assert.Equal("Text", failure.Subject);
    }

    [Fact]
    public void ThePointReadThrowsTheSameSentenceTheLeafGives()
    {
      var space = Plain();
      var byRead = Assert.Throws<CellReadException>(() => At(space, 1).Text());
      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Of(Text()).Map(space));

      Assert.Equal(Problem(byLeaf), byRead.Message);
      Assert.Null(At(space, 2).TextOrBlank());
      Assert.Throws<CellReadException>(() => At(space, 1).TextOrBlank());
    }

    [Fact]
    public void ASheetWordsItsOwnRefusalAndTheCoreLeafCarriesIt()
    {
      // One leaf, the best sentence its space can give: a sheet knows the cell holds a number.
      var sheet = Mixed(new object?[,] { { 42 } });
      var failure = Assert.Throws<ProjectionException>(
        () => ProjectionBuilders<ISheetCells>.Text().Map(sheet));

      Assert.Equal("expected Text at A1, found Number", Problem(failure));
    }
  }
}
