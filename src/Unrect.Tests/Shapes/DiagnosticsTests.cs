using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The result surface. <c>Map</c> is unchanged and simply discards what a parse noticed;
  /// <c>MapWithDiagnostics</c> hands it back. Neither ever softens a failure nothing declared
  /// tolerance for — declared tolerance is the only thing that does.
  /// </summary>
  public class DiagnosticsTests
  {
    private static ISpace Square() => Grid(new[,] { { 1, 2 }, { 3, 4 } });

    private static IShape<string> Title() => Cell(v => v.GetString()).Named("title");

    // --- The two entry points ---------------------------------------------------------------------

    [Fact]
    public void Map_DiscardsWhatTheParseNoticed()
    {
      // The same parse, the same value; Map simply does not offer the diagnostics.
      Assert.Null(Title().Optional().Map(Square()));
    }

    [Fact]
    public void MapWithDiagnostics_SurfacesWhatTheSameParseNoticed()
    {
      var result = Title().Optional().MapWithDiagnostics(Square());

      Assert.Null(result.Value);
      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void BothEntryPointsStillThrowForAnUndeclaredFailure()
    {
      // No boundary, no softening: this is the whole point of there being no lenient mode.
      Assert.Throws<ShapeException>(() => Title().Map(Square()));
      Assert.Throws<ShapeException>(() => Title().MapWithDiagnostics(Square()));
    }

    [Fact]
    public void MapWithDiagnostics_RejectsNullArguments()
    {
      Assert.Throws<ArgumentNullException>(() => Title().MapWithDiagnostics(null!));
      Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).MapWithDiagnostics(Square()));
    }

    [Fact]
    public void DiagnosticsAreASnapshotThatLaterParsesDoNotDisturb()
    {
      var shape = Title().Optional();

      var first = shape.MapWithDiagnostics(Square());
      var second = shape.MapWithDiagnostics(Square());

      Assert.Single(first.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Single(second.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ADefaultMapResult_HasNoDiagnostics()
    {
      Assert.Empty(default(MapResult<int>).Diagnostics);
    }

    // --- Unconsumed space -----------------------------------------------------------------------------

    [Fact]
    public void AShapeThatDescribesTheWholeSpace_ReportsNothing()
    {
      Assert.Empty(Range(b => b.Width).MapWithDiagnostics(Square()).Diagnostics);
    }

    [Fact]
    public void RowsLeftOver_AreReportedWithTheFirstUndescribedCell()
    {
      var info = Assert.Single(Range(2, 1, b => b.Width).MapWithDiagnostics(Square()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("the shape consumed 1 of 2 rows; rows 2+ were not described", info.Message);
      Assert.Equal("A2", info.Location.A1);
    }

    [Fact]
    public void ColumnsLeftOver_AreReportedWithTheFirstUndescribedCell()
    {
      var info = Assert.Single(Range(1, 2, b => b.Width).MapWithDiagnostics(Square()).Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, info.Severity);
      Assert.Equal("the shape consumed 1 of 2 columns; columns 2+ were not described", info.Message);
      Assert.Equal("B1", info.Location.A1);
    }

    [Fact]
    public void WhenBothAxesFallShort_BothAreReportedInOneDiagnostic()
    {
      var info = Assert.Single(Cell(v => v.GetInt()).MapWithDiagnostics(Square()).Diagnostics);

      Assert.Equal(
        "the shape consumed 1 of 2 rows and 1 of 2 columns; rows 2+ and columns 2+ were not described",
        info.Message);
    }

    [Fact]
    public void SpaceSkippedBeforeAShape_IsUndescribedToo()
    {
      // A shape that starts two rows down described neither the rows it skipped nor the rows after
      // it. Reporting only the tail would say the leading gap was accounted for.
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 }, { 4 } });

      var info = Assert.Single(Cell(v => v.GetInt()).Down(2).MapWithDiagnostics(space).Diagnostics);

      Assert.Equal("the shape consumed 1 of 4 rows; rows 1-2 and 4+ were not described", info.Message);

      // The earliest undescribed cell, which with a leading gap is the very first one.
      Assert.Equal("A1", info.Location.A1);
    }

    [Fact]
    public void ASingleSkippedRowIsNamedWithoutARange()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 }, { 4 } });

      var info = Assert.Single(Cell(v => v.GetInt()).Down(1).MapWithDiagnostics(space).Diagnostics);

      Assert.Equal("the shape consumed 1 of 4 rows; rows 1 and 3+ were not described", info.Message);
    }

    [Fact]
    public void SkippedColumnsAreReportedTheSameWay()
    {
      var space = Grid(new[,] { { 1, 2, 3, 4 } });

      var info = Assert.Single(Cell(v => v.GetInt()).Right(2).MapWithDiagnostics(space).Diagnostics);

      Assert.Equal("the shape consumed 1 of 4 columns; columns 1-2 and 4+ were not described", info.Message);
      Assert.Equal("A1", info.Location.A1);
    }

    [Fact]
    public void AnAbsorbedRootReportsItsWarningAndNothingElse()
    {
      // Tolerance declared at the root is the nearest thing to a lenient mode, and an absorbed root
      // consumed nothing — so "consumed 0 of 2 rows" would sit underneath a warning that already
      // named the shape, the reason, and the cell, saying the same thing more vaguely.
      var result = Title().Optional().MapWithDiagnostics(Mixed(new object?[,] { { 1 }, { 2 } }));

      var only = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Warning, only.Severity);
      Assert.Equal("'title'", only.Subject);
      Assert.DoesNotContain("not described", only.Message);
    }

    [Fact]
    public void AZeroConsumingRootWithNothingAbsorbed_StillReportsUnconsumedSpace()
    {
      // The other half of the rule: a repeat that found no sections described nothing either, but
      // tolerated nothing on the way, so there is no warning to make the Info redundant.
      var result = Repeat(Range(b => b.Height)).MapWithDiagnostics(Grid(new[,] { { 0, 0 }, { 0, 0 } }));

      var only = Assert.Single(result.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, only.Severity);
      Assert.Equal(
        "the shape consumed 0 of 2 rows and 0 of 2 columns; rows 1+ and columns 1+ were not described",
        only.Message);
    }

    [Fact]
    public void TwoAbsorbingChildrenAreNotAnAbsorbedRoot()
    {
      // Suppression is for the one case where the warning already says everything: the whole parse
      // was a single absorbed failure. A flow whose children each absorbed something described
      // nothing either, but no single warning covers the sheet, so the gap is still worth naming.
      var space = Mixed(new object?[,] { { 1 }, { 2 } });

      var result = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()).Named("a").Optional());
        return v.Next(Cell(c => c.GetString()).Named("b").Optional());
      }).MapWithDiagnostics(space);

      Assert.Equal(2, result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning));
      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info && d.Message.Contains("not described"));
    }

    [Fact]
    public void ARepeatThatDiscardsAnAttempt_DiscardsWhatTheAttemptTolerated()
    {
      // The item absorbs, and so consumes nothing, and so is not collected — the repetition ends
      // having read nothing. A warning about a reading that was thrown away would describe a parse
      // that never happened, exactly as with a losing choice branch.
      var result = Repeat(Cell(v => v.GetString()).Optional()).MapWithDiagnostics(Mixed(new object?[,] { { 1 }, { 2 }, { 3 } }));

      Assert.Empty(result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info && d.Message.Contains("not described"));
    }

    [Fact]
    public void APartiallyConsumingRoot_StillReportsUnconsumedSpaceAlongsideAWarning()
    {
      // Suppression is only for a root that read nothing at all; a shape that read some of the
      // sheet and tolerated something still wants to be told about the rest.
      var space = Mixed(new object?[,] { { "a" }, { 5 }, { 6 } });

      var result = VerticalFlow(v =>
      {
        v.Next(Cell(c => c.GetString()).Optional());
        return v.Next(Cell(c => c.GetInt()));
      }).MapWithDiagnostics(space);

      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info && d.Message.Contains("not described"));
    }

    [Fact]
    public void UnconsumedSpace_IsAttributedToTheRootShape()
    {
      var info = Assert.Single(Cell(v => v.GetInt()).Named("just one cell").MapWithDiagnostics(Square()).Diagnostics);

      Assert.Equal("'just one cell'", info.Subject);
    }

    // --- Severity --------------------------------------------------------------------------------------

    [Fact]
    public void AlternationIsInfoAndToleranceIsWarning()
    {
      // The split is the whole judgement the framework makes about how alarming an event is: a
      // choice going to its second variant is expected; a boundary supplying a filler is not.
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      var choice = Choice(
        VerticalFlow(v => { v.Next(Cell(c => c.GetInt())); return v.Next(Cell(c => c.GetInt())); }).Named("A"),
        VerticalFlow(v => { v.Next(Cell(c => c.GetString())); return v.Next(Cell(c => c.GetInt())); }).Named("B"));

      Assert.All(
        choice.MapWithDiagnostics(space).Diagnostics,
        d => Assert.Equal(DiagnosticSeverity.Info, d.Severity));

      Assert.Equal(
        DiagnosticSeverity.Warning,
        Assert.Single(
          Title().Optional().MapWithDiagnostics(Mixed(new object?[,] { { 1 } })).Diagnostics,
          d => d.Severity == DiagnosticSeverity.Warning).Severity);
    }

    [Fact]
    public void ADiagnosticRendersItsSeveritySubjectMessagePathAndLocation()
    {
      var warning = Assert.Single(
        Title().Optional().MapWithDiagnostics(Mixed(new object?[,] { { 1 } })).Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning);

      var rendered = warning.ToString();

      Assert.StartsWith("Warning: 'title': ", rendered);
      Assert.Contains(" — in 'title' (Cell) at row 1, column 1 (A1)", rendered);
    }

    // --- The documented recovery recipe -------------------------------------------------------------------
    //
    // "One malformed section among a hundred" is recovered by re-anchoring, not by a Repeat
    // parameter. The seek belongs to the ITEM, outside the boundary: running out of anchors is how
    // the repetition stops, so that one failure must never be tolerated, while anything wrong
    // inside an anchored section is exactly what the boundary is for.

    private static IShape<IReadOnlyList<string>> Sections()
    {
      var section =
        VerticalFlow(v =>
        {
          v.Next(Cell(c => c.GetString()).Named("label"));
          return (string?)v.Next(Row(2, r => r[0].GetString()).Named("body"));
        });

      var item = section
        .Else(Row(2, _ => (string?)null).Named("unreadable section"))
        .After(SeekRowContaining("Section"));

      return Repeat(item).Select(all => (IReadOnlyList<string>)all.Where(s => s is not null).ToList()!);
    }

    [Fact]
    public void TheRecoveryRecipe_ReadsEveryGoodSection()
    {
      var space = Mixed(new object?[,]
      {
        { "Section", null },
        { "A", 10 },
        { "Section", null },
        { "B", 20 },
        { "trailing junk nobody described", null },
      });

      var result = Sections().MapWithDiagnostics(space);

      Assert.Equal(new[] { "A", "B" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void TheRecoveryRecipe_CarriesOnPastAMalformedSection()
    {
      // The middle section's body is unreadable. The good ones on either side still arrive, and the
      // one that did not is reported rather than silently dropped.
      var space = Mixed(new object?[,]
      {
        { "Section", null },
        { "A", 10 },
        { "Section", null },
        { 999, "not a label" },
        { "Section", null },
        { "B", 20 },
        { "trailing junk nobody described", null },
      });

      var result = Sections().MapWithDiagnostics(space);

      Assert.Equal(new[] { "A", "B" }, result.Value);
    }

    [Fact]
    public void TheRecoveryRecipe_ReportsWhereAndWhyTheMalformedSectionFailed()
    {
      var space = Mixed(new object?[,]
      {
        { "Section", null },
        { "A", 10 },
        { "Section", null },
        { 999, "not a label" },
        { "Section", null },
        { "B", 20 },
        { "trailing junk nobody described", null },
      });

      var warning = Assert.Single(
        Sections().MapWithDiagnostics(space).Diagnostics,
        d => d.Severity == DiagnosticSeverity.Warning);

      Assert.Equal("'body'", warning.Subject);
      Assert.Contains("Cell value is Number; expected Text", warning.Message);
      Assert.Contains("Repeat[1]", warning.Path);

      // Row 4 is the malformed body row — the warning points into the junk, not at the repeat.
      Assert.Equal("A4", warning.Location.A1);
    }

    [Fact]
    public void TheRecoveryRecipe_StopsCleanlyWhenTheAnchorsRunOut()
    {
      // The trailing row carries no section label, so the item's own seek fails and the repetition
      // stops. Had the seek been inside the boundary, that stop signal would have been absorbed as
      // a tolerance event and the recipe would never terminate.
      var space = Mixed(new object?[,]
      {
        { "Section", null },
        { "A", 10 },
        { "trailing junk nobody described", null },
      });

      var result = Sections().MapWithDiagnostics(space);

      Assert.Equal(new[] { "A" }, result.Value);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);

      // The unread trailing row is what "stopped cleanly" looks like from the outside.
      var info = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);
      Assert.Equal("the shape consumed 2 of 3 rows; rows 3+ were not described", info.Message);
    }

    [Fact]
    public void TheRecoveryRecipe_OnASheetWithNoSectionsAtAll_IsEmptyRatherThanAnError()
    {
      var space = Mixed(new object?[,] { { "nothing", null }, { "here", null } });

      Assert.Empty(Sections().Map(space));
    }
  }
}
