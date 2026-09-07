using System;

using Unrect.Core;
using Unrect.Strategies;

using Xunit;

using static Unrect.Tests.ProjectionTestSpaces;

using static Unrect.Strategies.OffsetStrategies;

namespace Unrect.Tests.Strategies
{
  /// <summary>
  /// A landmark locates; a lift turns that location into an offset. The two are one family and are
  /// pinned together here, because a lift is defined in terms of its landmark and either could
  /// quietly narrow what the other accepts.
  /// </summary>
  public class LandmarkAndLiftTests
  {
    // --- The lifts: To and Past -------------------------------------------------------------------
    //
    // A landmark says where something is; a lift turns that into an offset. `To` lands ON the match,
    // `Past` one after it — the whole of the old anchor-then-skip idiom. A skip-while anchors on
    // absence and is defeated by anything inserted above the thing being looked for; these anchor on
    // presence, which is what survives an inserted proof row.

    private static ISpace Labelled() => Text(new string?[,]
    {
      { "junk", null },
      { "an inserted proof row", null },
      { "  SECTION  ", null },
      { "a", "b" },
    });

    private static ISpace LabelledColumns() => Text(new string?[,]
    {
      { "a", "b", "  TOTAL  ", "d" },
      { null, null, null, null },
    });

    // --- To lands on the match ----------------------------------------------------------------------

    [Fact]
    public void To_RowContaining_LandsOnTheRowThatHoldsTheLabel()
    {
      // The offset stops short of the match, so the region it places starts AT the label — the two
      // junk rows above are exactly what a skip-while would have tripped on.
      var offset = To(RowLandmarks.RowContaining("SECTION")).GetOffset(Labelled());

      Assert.Equal(0, offset.Size.Width);
      Assert.Equal(2, offset.Size.Height);
    }

    [Fact]
    public void To_RowWithCell_LandsOnTheFirstRowWithAMatchingCell()
    {
      // Column 1 is empty until the last row, so this finds a row by a cell that is not the first.
      Assert.Equal(3, To(RowLandmarks.RowWithCell(cell => cell.TryGetString() == "b")).GetOffset(Labelled()).Size.Height);
    }

    [Fact]
    public void To_RowWhere_LandsOnTheFirstRowSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 0 }, { 2, 0 }, { 3, 0 } });

      Assert.Equal(2, To(RowLandmarks.RowWhere((s, row) => s[0, row].GetInt() == 3)).GetOffset(space).Size.Height);
    }

    [Fact]
    public void To_ColumnContaining_LandsOnTheColumnThatHoldsTheLabel()
    {
      var offset = To(ColumnLandmarks.ColumnContaining("Total")).GetOffset(LabelledColumns());

      Assert.Equal(2, offset.Size.Width);
      Assert.Equal(0, offset.Size.Height);
    }

    [Fact]
    public void To_ColumnWithCell_LandsOnTheFirstColumnWithAMatchingCell()
    {
      Assert.Equal(3, To(ColumnLandmarks.ColumnWithCell(cell => cell.TryGetString() == "d")).GetOffset(LabelledColumns()).Size.Width);
    }

    [Fact]
    public void To_ColumnWhere_LandsOnTheFirstColumnSatisfyingAPositionalPredicate()
    {
      var space = Grid(new[,] { { 1, 2, 3 } });

      Assert.Equal(1, To(ColumnLandmarks.ColumnWhere((s, column) => s[column, 0].GetInt() == 2)).GetOffset(space).Size.Width);
    }

    // --- Past lands one after -----------------------------------------------------------------------

    [Fact]
    public void Past_LandsOnTheRowAfterTheMatch()
    {
      Assert.Equal(3, Past(RowLandmarks.RowContaining("SECTION")).GetOffset(Labelled()).Size.Height);
    }

    [Fact]
    public void Past_IsToPlusOneRow()
    {
      // The lift replaced Then(To(...), SkipRows(1)) at every call site; this is the arithmetic it
      // absorbed, pinned on one grid so the two spellings cannot drift.
      var space = Labelled();

      Assert.Equal(
        Then(To(RowLandmarks.RowContaining("SECTION")), ExplicitOffset(0, 1)).GetOffset(space).Size.Height,
        Past(RowLandmarks.RowContaining("SECTION")).GetOffset(space).Size.Height);
    }

    [Fact]
    public void Past_LandsOnTheColumnAfterTheMatch()
    {
      Assert.Equal(3, Past(ColumnLandmarks.ColumnContaining("Total")).GetOffset(LabelledColumns()).Size.Width);
    }

    [Fact]
    public void Past_OnTheLastRow_YieldsAZeroRowSubspaceRatherThanFailing()
    {
      // The lift's job is the arithmetic; running out of rows is the caller's problem, and the
      // caller is what reports it.
      var space = Text(new string?[,] { { "a" }, { "TARGET" } });

      Assert.Equal(2, Past(RowLandmarks.RowContaining("TARGET")).GetOffset(space).Size.Height);
      Assert.Equal(2, space.Area.Size.Height);
    }

    // --- Matching rules are the landmark's ------------------------------------------------------------

    [Fact]
    public void ALift_TrimsBothSidesAndIgnoresCase()
    {
      // The sheet says "  SECTION  "; the declaration may say it any way that reads well.
      Assert.Equal(2, To(RowLandmarks.RowContaining("Section")).GetOffset(Labelled()).Size.Height);
      Assert.Equal(2, To(RowLandmarks.RowContaining("section")).GetOffset(Labelled()).Size.Height);
      Assert.Equal(2, To(RowLandmarks.RowContaining("  section  ")).GetOffset(Labelled()).Size.Height);

      Assert.Equal(2, To(ColumnLandmarks.ColumnContaining("total")).GetOffset(LabelledColumns()).Size.Width);
      Assert.Equal(2, To(ColumnLandmarks.ColumnContaining("  Total  ")).GetOffset(LabelledColumns()).Size.Width);
    }

    [Theory]
    [InlineData("ecti")]
    [InlineData("SEC")]
    [InlineData("SECTION HEADER")]
    public void ALift_MatchesWholeCellsNotSubstrings(string needle)
    {
      // Labels are whole cell values; substring matching would anchor on the first cell that merely
      // mentions the word. Anything fancier is what the predicate landmark is for.
      Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowContaining(needle)).GetOffset(Labelled()));
      Assert.ThrowsAny<OutOfBoundsException>(() => To(ColumnLandmarks.ColumnContaining("Tot")).GetOffset(LabelledColumns()));
    }

    // --- A miss is a placement failure, which is what lets a Repeat stop -------------------------------

    [Theory]
    [InlineData("Nope")]
    [InlineData("")]
    public void ALiftWithNoMatch_Throws(string needle)
    {
      Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowContaining(needle)).GetOffset(Labelled()));
      Assert.ThrowsAny<OutOfBoundsException>(() => Past(RowLandmarks.RowContaining(needle)).GetOffset(Labelled()));
    }

    [Fact]
    public void EveryLiftProjectionThrowsOnAMiss_OnBothAxes()
    {
      Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowWhere((_, _) => false)).GetOffset(Labelled()));
      Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowWithCell(_ => false)).GetOffset(Labelled()));
      Assert.ThrowsAny<OutOfBoundsException>(() => Past(RowLandmarks.RowWhere((_, _) => false)).GetOffset(Labelled()));

      Assert.ThrowsAny<OutOfBoundsException>(() => To(ColumnLandmarks.ColumnContaining("Nope")).GetOffset(LabelledColumns()));
      Assert.ThrowsAny<OutOfBoundsException>(() => To(ColumnLandmarks.ColumnWhere((_, _) => false)).GetOffset(LabelledColumns()));
      Assert.ThrowsAny<OutOfBoundsException>(() => To(ColumnLandmarks.ColumnWithCell(_ => false)).GetOffset(LabelledColumns()));
      Assert.ThrowsAny<OutOfBoundsException>(() => Past(ColumnLandmarks.ColumnContaining("Nope")).GetOffset(LabelledColumns()));
    }

    [Fact]
    public void AMissIsTheAnchorNotFoundKind_WhichIsWhatARepeatStopsOn()
    {
      // The derived type is internal, so this is what a caller can see: a miss is an
      // OutOfBoundsException, which is a placement failure, which is a Repeat's stop condition.
      // Nothing narrower is asserted, deliberately.
      var miss = Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowContaining("Nope")).GetOffset(Labelled()));

      Assert.IsAssignableFrom<OutOfBoundsException>(miss);
    }

    // --- Composition across the axes --------------------------------------------------------------------

    [Fact]
    public void ToComposesAcrossBothAxesInOneOffset()
    {
      // The K-1 entity anchor: find the column that says EIN:, then the row that does, and start
      // there. Neither lift knows about the other; Then is what puts them together.
      var space = Text(new string?[,]
      {
        { "z", "q" },
        { "w", "EIN:" },
      });

      var offset = Then(
        To(ColumnLandmarks.ColumnContaining("EIN:")),
        To(RowLandmarks.RowContaining("EIN:")))
        .GetOffset(space);

      Assert.Equal(1, offset.Size.Width);
      Assert.Equal(1, offset.Size.Height);
    }

    // --- Landmarks: the same content rules, without the offset -------------------------------------
    //
    // A landmark says where a projection ends, where a seek says where one starts. The trio mirrors the
    // seeks exactly and matches on the same rules; the difference is that a landmark reports "not
    // found" as null and lets the projection bounding itself decide, where a seek throws.

    private static ISpace RowsWithATotal() => Text(new string?[,]
    {
      { "x", "y" },
      { "  TOTAL  ", null },
      { "z", null },
    });

    private static ISpace ColumnsWithATotal() => Text(new string?[,]
    {
      { "a", "  TOTAL  ", "c" },
      { null, null, "z" },
    });

    [Fact]
    public void RowWhere_FindsTheFirstRowSatisfyingAPositionalPredicate()
    {
      Assert.Equal(2, RowLandmarks.RowWhere((space, row) => space[0, row].TryGetString() == "z").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowWithCell_FindsTheFirstRowWithAMatchingCell()
    {
      // Column 1 is empty except on the first row, so this finds a row by a cell that is not its
      // first — the reason the "any cell" form exists at all.
      Assert.Equal(0, RowLandmarks.RowWithCell(cell => cell.TryGetString() == "y").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowContaining_MatchesWholeCellsTrimmedAndCaseInsensitively()
    {
      // The sheet says "  TOTAL  "; the declaration may say it any way that reads well.
      Assert.Equal(1, RowLandmarks.RowContaining("Total").FindRow(RowsWithATotal()));
      Assert.Equal(1, RowLandmarks.RowContaining("  total  ").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowContaining_MatchesWholeCellsNotSubstrings()
    {
      Assert.Null(RowLandmarks.RowContaining("TOT").FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowContaining("TOTALS").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void RowLandmarks_ReportAMissAsNullRatherThanThrowing()
    {
      // The whole difference from a seek: a missing end is a question for the projection being
      // bounded, not a failure in itself.
      Assert.Null(RowLandmarks.RowWhere((_, _) => false).FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowWithCell(_ => false).FindRow(RowsWithATotal()));
      Assert.Null(RowLandmarks.RowContaining("Nope").FindRow(RowsWithATotal()));
    }

    [Fact]
    public void ColumnWhere_FindsTheFirstColumnSatisfyingAPositionalPredicate()
    {
      Assert.Equal(2, ColumnLandmarks.ColumnWhere((space, column) => space[column, 0].TryGetString() == "c").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnWithCell_FindsTheFirstColumnWithAMatchingCell()
    {
      Assert.Equal(2, ColumnLandmarks.ColumnWithCell(cell => cell.TryGetString() == "z").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnContaining_MatchesWholeCellsTrimmedAndCaseInsensitively()
    {
      Assert.Equal(1, ColumnLandmarks.ColumnContaining("Total").FindColumn(ColumnsWithATotal()));
      Assert.Equal(1, ColumnLandmarks.ColumnContaining("  total  ").FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnContaining("TOT").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ColumnLandmarks_ReportAMissAsNullRatherThanThrowing()
    {
      Assert.Null(ColumnLandmarks.ColumnWhere((_, _) => false).FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnWithCell(_ => false).FindColumn(ColumnsWithATotal()));
      Assert.Null(ColumnLandmarks.ColumnContaining("Nope").FindColumn(ColumnsWithATotal()));
    }

    [Fact]
    public void ALandmarkDescribesWhatItLookedFor()
    {
      // The descriptions are the negative noun phrases the failure templates are built from, so a
      // bound reads beside a seek rather than in its own dialect.
      Assert.Equal("no matching row", RowLandmarks.RowWhere((_, _) => false).Description);
      Assert.Equal("no row with a matching cell", RowLandmarks.RowWithCell(_ => false).Description);
      Assert.Equal("no row containing \'Total\'", RowLandmarks.RowContaining("Total").Description);

      Assert.Equal("no matching column", ColumnLandmarks.ColumnWhere((_, _) => false).Description);
      Assert.Equal("no column with a matching cell", ColumnLandmarks.ColumnWithCell(_ => false).Description);
      Assert.Equal("no column containing \'Total\'", ColumnLandmarks.ColumnContaining("Total").Description);
    }

    [Fact]
    public void ALiftAndItsLandmarkAgreeOnWhatContainingMeans()
    {
      // A lift is defined in terms of its landmark, so this is not two implementations agreeing —
      // it is the lift surfacing the landmark's rule unchanged. Worth pinning because the lift adds
      // the arithmetic and the failure, and either could have quietly narrowed what "containing"
      // accepts on the way through.
      var space = RowsWithATotal();

      foreach (var needle in new[] { "Total", "  total  ", "TOTAL" })
      {
        Assert.Equal(1, RowLandmarks.RowContaining(needle).FindRow(space));
        Assert.Equal(1, To(RowLandmarks.RowContaining(needle)).GetOffset(space).Size.Height);
      }

      // ...including on what does not match, which the two report differently: the landmark returns
      // null and leaves the decision to its caller, and the lift turns that into a placement
      // failure.
      Assert.Null(RowLandmarks.RowContaining("TOT").FindRow(space));
      Assert.ThrowsAny<OutOfBoundsException>(() => To(RowLandmarks.RowContaining("TOT")).GetOffset(space));
    }

    [Fact]
    public void LandmarkFactories_RejectNullArguments()
    {
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowWhere(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowWithCell(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => RowLandmarks.RowContaining(null!)).ParamName);
      Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnWhere(null!)).ParamName);
      Assert.Equal("anyCell", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnWithCell(null!)).ParamName);
      Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => ColumnLandmarks.ColumnContaining(null!)).ParamName);
    }
  }
}
