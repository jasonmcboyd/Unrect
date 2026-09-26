using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The text a cell HOLDS, as distinct from what it says — a question about the value's kind, and
  /// so the value vocabulary's: <c>IsText()</c>, <c>TryGetText</c>, the asserting <c>Text()</c> read
  /// and the <c>Text()</c> leaf are all derived once from the value at the cell, so none of them can
  /// disagree with it, and the sheet words its own refusal.
  /// </summary>
  public class HeldTextTests
  {
    /// <summary>Words, a number, and nothing.</summary>
    private static ICellSpace Sheet() => Mixed(new object?[,] { { "Total", 42, null } });

    private static Point<ICellSpace> At(ICellSpace space, int column) => Plane<ICellSpace>.Of(space)[column, 0];

    [Fact]
    public void IsTextIsExactlyWhetherTheReadWouldSucceed()
    {
      var space = Sheet();

      for (var column = 0; column < 3; column++)
      {
        var point = At(space, column);

        Assert.Equal(point.TryGetText(out _), point.IsText());
        Assert.Equal(point.TryGetText(out _, out _), point.IsText());
      }

      Assert.True(At(space, 0).IsText());
      Assert.False(At(space, 1).IsText());
      Assert.False(At(space, 2).IsText());
    }

    [Fact]
    public void ACellThatSaysSomethingDoesNotTherebyHoldIt()
    {
      var number = At(Sheet(), 1);

      Assert.Equal("42", number.AsText());
      Assert.False(number.TryGetText(out _));
    }

    [Fact]
    public void ASuccessfulReadCarriesNoProblem()
    {
      Assert.True(At(Sheet(), 0).TryGetText(out var text, out var problem));
      Assert.Equal("Total", text);
      Assert.Null(problem);
    }

    [Fact]
    public void TheSheetWordsTheRefusalFromWhatTheCellIs()
    {
      var space = Sheet();

      Assert.False(At(space, 1).TryGetText(out _, out var number));
      Assert.Equal("expected Text at B1, found Number", number!.Value.Render("B1"));

      Assert.False(At(space, 2).TryGetText(out _, out var blank));
      Assert.Equal("expected Text at C1, found Blank", blank!.Value.Render("C1"));
    }

    [Fact]
    public void TheLeafReadsHeldText()
    {
      Assert.Equal("Total", Text().Map(Sheet()));

      var failure = Assert.Throws<ProjectionException>(() => Right(1).Of(Text()).Map(Sheet()));

      Assert.Equal("expected Text at B1, found Number", Problem(failure));
      Assert.Equal("Text", failure.Subject);
    }

    [Fact]
    public void ThePointReadThrowsTheSameSentenceTheLeafGives()
    {
      var space = Sheet();

      var byRead = Assert.Throws<CellReadException>(() => At(space, 1).Text());
      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Of(Text()).Map(space));

      Assert.Equal(Problem(byLeaf), byRead.Message);

      Assert.Null(At(space, 2).TextOrBlank());
      Assert.Throws<CellReadException>(() => At(space, 1).TextOrBlank());
    }
  }
}
