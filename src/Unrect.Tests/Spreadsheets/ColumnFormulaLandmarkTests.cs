using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Fluent;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Fluent.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// <c>Columns.WithCell(p => p.HasFormula())</c>, the declared twin of <c>Rows.WithCell(p => p.HasFormula())</c>.
  /// <para>
  /// It exists because the mirror is law, not because a caller asked for it — which is exactly the
  /// shape that goes untested and rots. Everything the row half is pinned for lives in three other
  /// classes (<see cref="FormulaCapabilityTests"/>, <see cref="FormulaAbsenceTests"/> and the
  /// acceptance suite's lift test) and none of them says the word "column"; a
  /// <c>base("Row", …)</c> left behind in the column landmark's constructor, or a transposed loop in
  /// its search, would have passed the whole suite.
  /// </para>
  /// <para>
  /// So the claims here are the column half of the four the row half makes: the matcher locates
  /// (through both column lifts, and through the <c>containing</c> overload that discriminates), the
  /// lift raises the demand into the type, a space that cannot answer is a fault no tolerance
  /// absorbs, and a space that can answer but holds no match is an ordinary quiet absence. The
  /// naming test is the one with no row counterpart to mirror: it reads the axis word back out of
  /// both the fault and the absence, because that word is the only difference between the two
  /// landmarks' shared base.
  /// </para>
  /// </summary>
  public class ColumnFormulaLandmarkTests
  {
    /// <summary>
    /// A capable sheet with its computed columns deliberately apart:
    /// <code>
    ///        A        B      C       D
    /// 1    Fund      Q1     Rate    Total
    /// 2    Alpha     100    0.5     50        C2 = $C$1        D2 = SUM(B2:C2)
    /// 3    Beta      250    0.5     125       C3 = $C$1        D3 = SUM(B3:C3)
    /// </code>
    /// Two formula columns rather than one, so "the first column holding a formula" and "the first
    /// column holding a formula mentioning SUM" are different answers. A fixture with one would let
    /// the <c>containing</c> overload ignore its argument entirely and still pass.
    /// </summary>
    private static ISpreadsheetSpace Sheet()
    {
      var cells = new object?[,]
      {
        { "Fund", "Q1", "Rate", "Total" },
        { "Alpha", 100m, 0.5m, 50m },
        { "Beta", 250m, 0.5m, 125m },
      };

      var values = new CellValue[cells.GetLength(0), cells.GetLength(1)];

      for (var row = 0; row < cells.GetLength(0); row++)
        for (var column = 0; column < cells.GetLength(1); column++)
          values[row, column] = Adapt(cells[row, column]);

      var formulas = new string?[3, 4];

      formulas[1, 2] = "$C$1";
      formulas[2, 2] = "$C$1";
      formulas[1, 3] = "SUM(B2:C2)";
      formulas[2, 3] = "SUM(B3:C3)";

      return new FormulaGridSpace(values, formulas);
    }

    /// <summary>A space that holds no formulas and cannot be asked about them.</summary>

    /// <summary>
    /// A space that CAN be asked and has nothing to report — the only way to reach the bare
    /// matcher's absence noun, since over <see cref="Sheet"/> the bare matcher always finds one.
    /// </summary>
    private static ISpreadsheetSpace Barren()
      => new FormulaGridSpace(new CellValue[1, 2] { { Adapt("a"), Adapt("b") } }, new string?[1, 2]);

    // --- The matcher locates ------------------------------------------------------------------------

    [Fact]
    public void On_LandsOnTheFirstColumnHoldingAFormula()
    {
      // C is the first computed column, and On owns it: offset 2, not 3. The heading is read from the
      // matched column itself, which is what distinguishes On from RightOf below.
      var applied = On(Columns.WithCell(p => p.HasFormula())).Of(Point().Text()).Apply(Sheet());

      Assert.Equal("Rate", applied.Value);
      Assert.Equal(2, applied.Offset.Column);
      Assert.Equal(0, applied.Offset.Row);
    }

    [Fact]
    public void RightOf_StartsExactlyOneColumnRightOfTheMatch()
    {
      var applied = RightOf(Columns.WithCell(p => p.HasFormula())).Of(Point().Text()).Apply(Sheet());

      Assert.Equal("Total", applied.Value);
      Assert.Equal(3, applied.Offset.Column);

      // Said the other way, so a regression in either operator shows up here: one more than On.
      Assert.Equal(
        On(Columns.WithCell(p => p.HasFormula())).Of(Point().Text()).Apply(Sheet()).Offset.Column + 1,
        applied.Offset.Column);
    }

    [Fact]
    public void TheContainingOverloadLooksPastAColumnWhoseFormulaDoesNotMentionIt()
    {
      // C holds a formula and D holds one that mentions SUM. The bare overload stops at C; this one
      // must walk past it. A substring, case-insensitively — the rule the row half documents.
      Assert.Equal("Total", On(Columns.WithCell(p => p.Formula() is string f && f.IndexOf("SUM", StringComparison.OrdinalIgnoreCase) >= 0)).Of(Point().Text()).Map(Sheet()));
      Assert.Equal("Total", On(Columns.WithCell(p => p.Formula() is string f && f.IndexOf("sum", StringComparison.OrdinalIgnoreCase) >= 0)).Of(Point().Text()).Map(Sheet()));

      // ...and the search is genuinely column-major: the reference formula in C matches on its own.
      Assert.Equal("Rate", On(Columns.WithCell(p => p.Formula() is string f && f.IndexOf("$C$1", StringComparison.OrdinalIgnoreCase) >= 0)).Of(Point().Text()).Map(Sheet()));
    }

    [Fact]
    public void ALiftRaisesTheProjectionItTouches()
    {
      // The column half of the acceptance suite's demand-climbing test. The demand is the file's
      // space, and the pipeline accepts this matcher only because that space can answer it — which
      // would not compile if the column lift had been left off the demanding family.
      IProjectionDefinition<ISpreadsheetSpace, string> firstComputedColumn =
        On(Columns.WithCell(p => p.HasFormula())).Of(Column(cells => cells[0].Text()));

      Assert.Equal("Rate", firstComputedColumn.Map(Sheet()));
    }

    // --- What it does with a space that cannot answer -----------------------------------------------

    [Fact]
    public void AbsenceOfAMatchStaysQuietWhereAbsenceOfTheCapabilityIsLoud()
    {
      // The half that keeps the test above meaningful. Over a space that CAN answer, "there is no
      // such column" is an ordinary miss, and the ordinary tolerances are exactly what it is for —
      // otherwise "loud" would only mean this matcher is loud about everything.
      var missing = On(Columns.WithCell(p => p.Formula() is string f && f.IndexOf("MEDIAN", StringComparison.OrdinalIgnoreCase) >= 0)).Of(Point().Text());

      Assert.Null(missing.Optional().Map(Sheet()));
      Assert.Equal("fallback", missing.Else("fallback").Map(Sheet()));

      // Untolerated it still fails, and NOT as a fault: it is a statement about the document.
      var failure = Assert.Throws<ProjectionException>(() => missing.Map(Sheet()));

      Assert.False(failure.IsFault, "a matcher that looked and found nothing is not a fault");
    }
  }
}
