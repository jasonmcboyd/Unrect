using System.Linq;

using Unrect.Projections;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Core.IValueSpace<int>>;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The same file-scoped vocabulary over a grid of values rather than a sheet: the space named at
  /// the top is <c>IValueSpace&lt;int&gt;</c>, so every cell below answers <c>Value()</c> and a rule
  /// can be written about the number itself. Nothing here mentions a spreadsheet, which is the point
  /// — the typed layer belongs to the vocabulary, not to a backend.
  /// </summary>
  public class TypedValueGridDeclarationTests
  {
    /// <summary>Two rows under seven, then one over it. Zero is this grid's blank.</summary>
    private static GridSpace<int> Numbers()
      => GridSpace.Create(
        new[,]
        {
          { 1, 2 },
          { 3, 4 },
          { 8, 9 },
        },
        isBlank: value => value == 0);

    [Fact]
    public void AGridsExtentCanBeDeclaredByTheValuesItsCellsHold()
    {
      var small = Sized(RowsWhileAny(cell => cell.Value() < 7))
        .Range(block => block.Rows.Sum(row => row.Sum(cell => cell.Value())));

      Assert.Equal(10, small.Map(Numbers()));
    }
  }
}
