using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.Observations;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The law: <c>p.Below(m)</c> and <c>p.On(m).Down(1)</c> are the same declaration, and its column
  /// twin <c>p.RightOf(m)</c> and <c>p.On(m).Right(1)</c>.
  /// <para>
  /// <strong>This suite promotes a coincidence to a law.</strong> The two spellings are not a
  /// delegation: <c>.Below</c> lifts the landmark through <c>Past</c>, while <c>.On</c> lifts it
  /// through <c>To</c> and <c>.Down(1)</c> then composes an explicit offset onto it. Two different
  /// strategy trees arrive at the same row, and until now nothing said they had to keep doing so —
  /// a change to how movements compose, or to the composite offset's own bounds check, would have
  /// moved one and not the other in silence.
  /// </para>
  /// <para>
  /// <strong>The law holds at L3</strong>: value, offset, consumed extent, diagnostics, and the
  /// failure sentence with its path and its cell. That is stronger than the inventory dared claim
  /// (SRC-54 records it as open, "by analysis"), and it survives the two edges analysis was least
  /// sure of — a landmark on the last row, where the composite has a row to spare and the lift does
  /// not, and a landmark that matches nothing at all.
  /// </para>
  /// </summary>
  public class AnchorMovementLawTests
  {
    /// <summary>Four rows, one of which is the landmark — so the anchor can be moved down the sheet.</summary>
    private static ISpace RowsWithLandmarkAt(int row)
    {
      var values = new object?[4, 1];

      for (var index = 0; index < 4; index++)
        values[index, 0] = index == row ? "Detail" : $"r{index}";

      return Mixed(values);
    }

    /// <summary>The same four cells turned on their side, so the column twin reads identically.</summary>
    private static ISpace ColumnsWithLandmarkAt(int column)
    {
      var values = new object?[1, 4];

      for (var index = 0; index < 4; index++)
        values[0, index] = index == column ? "Detail" : $"c{index}";

      return Mixed(values);
    }

    private static IRowLandmark Detail() => RowContaining("Detail");

    private static IColumnLandmark DetailColumn() => ColumnContaining("Detail");

    // --- The law, wherever the landmark sits ---------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BelowIsOnFollowedByOneRowDown(int landmark)
    {
      var space = RowsWithLandmarkAt(landmark);

      AssertL3(
        Observe(Text().Below(Detail()), space),
        Observe(Text().On(Detail()).Down(1), space));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RightOfIsOnFollowedByOneColumnRight(int landmark)
    {
      var space = ColumnsWithLandmarkAt(landmark);

      AssertL3(
        Observe(Text().RightOf(DetailColumn()), space),
        Observe(Text().On(DetailColumn()).Right(1), space));
    }

    // --- The two edges the analysis was least sure of -------------------------------------------------

    [Fact]
    public void TheTwoSpellingsRunOutOfSheetTogether()
    {
      // A landmark on the last row leaves nothing under it. The lift resolves an offset that reaches
      // the far edge in one step; the composite resolves the landmark, is handed the one row that is
      // left, and steps off the end of it. Different arithmetic, and it has to arrive at the same
      // refusal — including the A1 cell just past the sheet.
      var space = RowsWithLandmarkAt(3);

      AssertL3(
        Observe(Text().Below(Detail()), space),
        Observe(Text().On(Detail()).Down(1), space));

      var failure = Assert.Throws<ProjectionException>(() => Text().Below(Detail()).Map(space));

      Assert.Contains("an extent of 1x1 does not fit here", failure.Message);
      Assert.Equal("A5", failure.Location.A1);
    }

    [Fact]
    public void AMissingLandmarkIsTheSameRefusalInBothSpellings()
    {
      // The composite fails in its first element, which is the landmark lift itself, so the sentence
      // is the matcher's own in both spellings and nothing about the movement written after it
      // leaks into the message.
      var space = RowsWithLandmarkAt(1);
      var missing = RowContaining("Nope");

      AssertL3(
        Observe(Text().Below(missing), space),
        Observe(Text().On(missing).Down(1), space));

      var failure = Assert.Throws<ProjectionException>(() => Text().On(missing).Down(1).Map(space));

      Assert.Contains("no row containing 'Nope' exists in the available space", failure.Message);
      Assert.False(failure.IsFault);
    }

    // --- The other half of the placement ---------------------------------------------------------------

    [Fact]
    public void ADeclaredAreaSurvivesEitherSpellingIdentically()
    {
      // Placement is two independent halves. Both spellings touch only the offset, so a projection
      // that declared its own extent keeps it — and keeps the same one — wherever the landmark puts
      // it. A composite offset that leaked into the area would show up here first.
      var space = RowsWithLandmarkAt(1);
      var sized = Range(b => $"{b.Width}x{b.Height}").Sized(Extent(1, 2));

      AssertL3(
        Observe(sized.Below(Detail()), space),
        Observe(sized.On(Detail()).Down(1), space));

      Assert.Equal("1x2", sized.Below(Detail()).Map(space));
    }
  }
}
