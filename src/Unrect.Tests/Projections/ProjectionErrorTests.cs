using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The diagnostics contract. Every failure in the fused layer is a <see
  /// cref="ProjectionException"/> that names the projection, the declaration path that reached it,
  /// and where on the sheet it was looking. These tests pin the message's structure and the exact
  /// phrasings the spec fixes; they deliberately do not pin whole messages byte-for-byte.
  /// </summary>
  public class ProjectionErrorTests
  {
    private static ISheetCells Square() => Grid(new[,] { { 1, 2 }, { 3, 4 } });

    // --- Case A: the offset does not fit ---------------------------------------------------------------

    [Fact]
    public void AnOffsetThatRunsPastTheSpace_ReportsWhatWasRequestedAndWhatWasAvailable()
    {
      var failure = Assert.Throws<ProjectionException>(() => Down(5).Of(IntCell()).Map(Square()));

      Assert.Contains("an offset of 0x5 does not fit the available space", failure.Message);
      Assert.Contains("2x2 available", failure.Message);
      Assert.Equal(0, failure.Requested!.Value.Width);
      Assert.Equal(5, failure.Requested!.Value.Height);
      Assert.Equal(2, failure.Location.Available.Width);
      Assert.Equal(2, failure.Location.Available.Height);
    }

    [Fact]
    public void AnOffsetStrategyThatThrows_IsReportedAsAnOffsetFailure()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 3 }, { 4 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        OffsetBy(Then(SkipRows(3), SkipRows(3))).Of(IntCell()).Map(space));

      Assert.Contains("its offset ran past the available space", failure.Message);
      Assert.IsType<OutOfBoundsException>(failure.InnerException);
    }

    [Fact]
    public void ASeekThatFindsNothing_SaysWhatItWasLookingFor()
    {
      // A missing anchor resolves as a placement failure, but "its offset ran past the available
      // space" would be useless: the useful fact is which label was not there.
      var space = Mixed(new object?[,] { { "nothing", null }, { "relevant", null } });

      var failure = Assert.Throws<ProjectionException>(() =>
        On(RowContaining("Taxable Income")).Of(TextCell().Named("taxable income")).Map(space));

      Assert.Equal("'taxable income'", failure.Subject);
      Assert.Contains("no row containing 'Taxable Income' exists in the available space", failure.Message);
      Assert.Contains("  in 'taxable income' (Text)", failure.Message);
      Assert.Contains("(A1)", failure.Message);
      Assert.Contains("2x2 available", failure.Message);
    }

    [Fact]
    public void EverySeekDescribesItsOwnKindOfMiss()
    {
      var space = Mixed(new object?[,] { { "nothing", null }, { "relevant", null } });

      Assert.Contains("no row containing 'Total' exists", Missing(OffsetStrategies.To(RowContaining("Total")), space));
      Assert.Contains("no column containing 'Total' exists", Missing(OffsetStrategies.To(ColumnContaining("Total")), space));
      Assert.Contains("no row with a matching cell exists", Missing(OffsetStrategies.To(RowWithCell(_ => false).Landmark), space));
      Assert.Contains("no column with a matching cell exists", Missing(OffsetStrategies.To(ColumnWithCell(_ => false).Landmark), space));
      Assert.Contains("no matching row exists", Missing(OffsetStrategies.To(RowWhere((_, _) => false).Landmark), space));
      Assert.Contains("no matching column exists", Missing(OffsetStrategies.To(ColumnWhere((_, _) => false).Landmark), space));
    }

    [Fact]
    public void ASeekMissNeverEscapesAsABareOutOfBoundsException()
    {
      var space = Mixed(new object?[,] { { "nothing", null } });

      Assert.Throws<ProjectionException>(() => On(RowContaining("Total")).Of(IntCell()).Map(space));
      Assert.Throws<ProjectionException>(() => OffsetBy(FromRight(9)).Of(IntCell()).Map(space));
      Assert.Throws<ProjectionException>(() => OffsetBy(FromBottom(9)).Of(IntCell()).Map(space));
    }

    [Fact]
    public void AnOffsetThatMerelyRunsOutOfRoom_StillSaysSo()
    {
      // The seek description replaces the generic wording only when there was an anchor to name.
      var space = Mixed(new object?[,] { { "nothing", null } });

      Assert.Contains("its offset ran past the available space", Missing(FromRight(9), space));
    }

    private static string Missing(IOffsetStrategy offset, ISheetCells space)
      => Assert.Throws<ProjectionException>(() => OffsetBy(offset).Of(TextCell()).Map(space)).Message;

    // --- Case B: the area does not fit ------------------------------------------------------------------

    [Fact]
    public void AnAreaThatDoesNotFit_ReportsWhatWasRequestedAndWhatWasAvailable()
    {
      var failure = Assert.Throws<ProjectionException>(() => Range(3, 3, b => b.Width).Map(Square()));

      Assert.Contains("an extent of 3x3 does not fit here", failure.Message);
      Assert.Contains("2x2 available", failure.Message);
      Assert.Equal(3, failure.Requested!.Value.Width);
      Assert.Equal(3, failure.Requested!.Value.Height);
    }

    [Fact]
    public void AnAreaStrategyThatThrows_IsReportedAsAnAreaFailure()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Row(ColumnStrategies.TakeColumns(9), s => s.Count).Map(Square()));

      Assert.Contains("its area ran past the space available here", failure.Message);
      Assert.IsType<OutOfBoundsException>(failure.InnerException);
    }

    // --- Strategies that fail in some way other than running out of space -------------------------------
    //
    // Running out of space is a stopping condition a Repeat is allowed to act on. Every other way a
    // strategy can fail is a broken declaration, and must arrive as a ProjectionException that says which
    // strategy it was — not as a bare exception from somewhere in the strategy calculus.

    [Fact]
    public void AnAreaStrategyThatThrows_IsReportedAgainstTheProjectionThatDeclaredIt()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(AreaStrategies.SelectArea(_ => throw new InvalidOperationException("boom")), b => b.Width).Map(Square()));

      Assert.Contains("its area strategy threw InvalidOperationException: boom", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Equal("Range", failure.Subject);
    }

    [Fact]
    public void AnOffsetStrategyThatThrows_IsReportedAgainstTheProjectionThatDeclaredIt()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        OffsetBy(OffsetStrategies.SelectOffset(_ => throw new InvalidOperationException("boom"))).Of(IntCell()).Map(Square()));

      Assert.Contains("its offset strategy threw InvalidOperationException: boom", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void AStrategyThatReturnsANegativeSize_IsReportedAsAStrategyFailure()
    {
      // Size rejects the negative itself; the engine's job is to say which strategy produced it.
      var failure = Assert.Throws<ProjectionException>(() =>
        Range(AreaStrategies.SelectArea(_ => new Size(-1, 1)), b => b.Width).Map(Square()));

      Assert.Contains("its area strategy threw ArgumentOutOfRangeException", failure.Message);
      Assert.IsType<ArgumentOutOfRangeException>(failure.InnerException);
    }

    [Fact]
    public void ASeparatorStrategyThatThrows_IsReportedAgainstTheRepeat()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(Range(1, 1, b => b.Width), separatedBy: OffsetStrategies.SelectOffset(_ => throw new InvalidOperationException("boom")))
          .Map(Square()));

      Assert.Contains("its separator strategy threw InvalidOperationException: boom", failure.Message);
      Assert.Equal("VerticalRepeat", failure.Subject);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    // --- Case D: the projection threw -------------------------------------------------------------------

    [Fact]
    public void ACellReadFailure_IsReportedAsAReadingWithItsPathAndLocation()
    {
      // A leaf reading the wrong kind is a statement about the DATA, and since phase 6 it reads as
      // one: the backend raises a CellReadException, the leaf's Project catches it, and what the
      // engine reports is that sentence with the declaration path and the A1 around it.
      //
      // Until phase 6 the leaf threw an InvalidOperationException from inside its own lambda and the
      // engine had no way to tell it from a bug, so the message was the generic "the projection
      // threw InvalidOperationException: Cell value is Number; expected Text" — the reader's
      // vocabulary wrapped around the document's. The generic wrapper is still there and still
      // tested: see AProjectionThatThrows_IsWrappedWithItsPathAndLocation below, which now needs a
      // projection that really does throw something unexpected to provoke it.
      var failure = Assert.Throws<ProjectionException>(() => TextCell().Map(Square()));

      Assert.Contains("expected Text at A1, found Number", failure.Message);
      Assert.DoesNotContain("the projection threw", failure.Message);
      Assert.Contains("  in Text", failure.Message);

      // And nothing inside it. A kinded leaf IS the reader — the kind is declaration data, not a
      // lambda body — so there is no thrown exception to carry: the leaf asks the backend, is told
      // no, and reports it. An inner exception appears only where a read happened inside a LAMBDA
      // the projection called, which is the CellReadException protocol; see FlowProjectionTests and
      // BinderAccessorTests for that half.
      Assert.Null(failure.InnerException);
      Assert.Contains("(A1)", failure.Message);
    }

    [Fact]
    public void AProjectionThatThrows_IsWrappedWithItsPathAndLocation()
    {
      // The generic wrapper, over something the engine has no vocabulary for: a user's own selector
      // blowing up is a bug and reads as one, naming the exception type it was.
      var failure = Assert.Throws<ProjectionException>(() =>
        IntCell().Select<ISheetCells, int, int>(ThrowingSelector).Map(Square()));

      Assert.Contains("the projection threw InvalidOperationException", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Contains("  in Select", failure.Message);
      Assert.Contains("(A1)", failure.Message);
    }

    [Fact]
    public void AProjectionFailureIsWrappedOnceOnly()
    {
      // A failure that surfaces from deep in a tree passes back through every enclosing Project; it
      // must not accumulate a wrapper at each level.
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var projection = VerticalFlow(v =>
      {
        var first = v.Next(IntCell());
        v.Next(VerticalRepeat(TextCell()).Named("items"));
        return first;
      });

      var failure = Assert.Throws<ProjectionException>(() => projection.Map(space));

      // The leaf reports its own read, so there is nothing wrapped — until phase 6 the leaf threw
      // an InvalidOperationException from inside its lambda and the engine wrapped that.
      Assert.Null(failure.InnerException);
      Assert.Equal(1, Occurrences(failure.Message, "expected Text at"));
      Assert.Equal(1, Occurrences(failure.Message, "  in "));
    }

    [Fact]
    public void AnInvariantFailureIsNotWrappedByAnEnclosingProjection()
    {
      var space = Mixed(new object?[,] { { "Investor" }, { "Acme" } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{string.Join(",", v.Next(Table(r => r["Amount"].Integer())))}|{v.Next(IntCell())}").Map(space));

      Assert.Null(failure.InnerException);
      Assert.Equal(1, Occurrences(failure.Message, "  in "));
    }

    // --- Paths ------------------------------------------------------------------------------------------

    [Fact]
    public void AnUnnamedProjection_IsIdentifiedByItsDescription()
    {
      var failure = Assert.Throws<ProjectionException>(() => TextCell().Map(Square()));

      Assert.Equal("Text", failure.Subject);
      Assert.Equal("Text", failure.Path);
    }

    [Fact]
    public void ANamedProjection_IsIdentifiedByItsNameAndSaysWhatKindItIs()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        TextCell().Named("report id").Map(Square()));

      Assert.Equal("'report id'", failure.Subject);
      Assert.Equal("'report id' (Text)", failure.Path);
    }

    [Fact]
    public void TheKindSuffixOnlyDecoratesNamedSegments()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(TextCell())}").Named("header").Map(Square()));

      // The named flow is a plain segment; the failing segment carries the kind only when it
      // rendered as a quoted name, and an ordinal is not one.
      Assert.Equal("'header' -> Text#2", failure.Path);
    }

    [Fact]
    public void EnclosingProjectionsAppearInThePathInDeclarationOrder()
    {
      var space = Grid(new[,] { { 1 }, { 2 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          v.Next(IntCell());
          return v.Next(VerticalFlow(w => $"{w.Next(IntCell())}{w.Next(TextCell())}").Named("inner"));
        }).Map(space));

      Assert.Contains(" -> ", failure.Path);
      Assert.StartsWith("VerticalFlow -> 'inner'", failure.Path);
    }

    [Fact]
    public void ARepeatDecoratesItsOwnSegmentWithTheItemIndex()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(TextCell().Named("code")).Map(space));

      Assert.Equal("VerticalRepeat[0] -> 'code' (Text)", failure.Path);
    }

    [Fact]
    public void ARepeatsIndexCountsTheItemsAlreadyCollected()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var failure = Assert.Throws<ProjectionException>(() => VerticalRepeat(IntCell()).Map(space));

      Assert.Equal("VerticalRepeat[2] -> Integer", failure.Path);
    }

    [Fact]
    public void AnUnnamedSelectContributesNoPathSegment()
    {
      var withoutSelect = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(TextCell())}").Map(Square()));

      var withSelect = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(TextCell())}").Select(x => x).Map(Square()));

      Assert.Equal("VerticalFlow -> Text#2", withoutSelect.Path);
      Assert.Equal(withoutSelect.Path, withSelect.Path);
      Assert.DoesNotContain("Select", withSelect.Path);
    }

    [Fact]
    public void ATransparentSelectIsStillBlamedWhenItsOwnSelectorThrows()
    {
      // Being skipped as an intermediate segment does not mean being unnameable as the culprit: the
      // failing projection is appended to the path even though it contributes no segment of its
      // own.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          v.Next(IntCell());
          return v.Next(HorizontalFlow(h => h.Next(IntCell())).Select<ISheetCells, int, int>(ThrowingSelector));
        }).Map(Square()));

      Assert.Equal("Select#2", failure.Subject);
      Assert.Equal("VerticalFlow -> Select#2", failure.Path);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void ANamedSelectContributesAPathSegment()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(TextCell())}").Select(x => x).Named("report").Map(Square()));

      Assert.Equal("'report' -> VerticalFlow -> Text#2", failure.Path);
    }

    [Fact]
    public void ATransparentSelectStillAccumulatesTheOrigin()
    {
      // Being skipped in the path must not mean being skipped in the coordinate arithmetic.
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 }, { 1, 0 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          v.Next(Range(2, 2, b => b.Width));
          return v.Next(Right(1).Of(IntCell()));
        }).Select(x => x).Map(space));

      Assert.Equal("B3", failure.Location.A1);
    }

    // --- Locations ----------------------------------------------------------------------------------------

    [Fact]
    public void TheLocationIsOneBasedAndRelativeToTheSpaceMapWasCalledWith()
    {
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 }, { 1, 0 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(Range(2, 2, b => b.Width))}{v.Next(Right(1).Of(IntCell()))}").Map(space));

      Assert.Equal(3, failure.Location.Row);
      Assert.Equal(2, failure.Location.Column);
      Assert.Equal("B3", failure.Location.A1);
      Assert.Contains("at row 3, column 2 (B3)", failure.Message);
    }

    [Theory]
    [InlineData(1, 1, "A1")]
    [InlineData(26, 1, "Z1")]
    [InlineData(27, 1, "AA1")]
    [InlineData(28, 30, "AB30")]
    [InlineData(52, 2, "AZ2")]
    [InlineData(53, 2, "BA2")]
    public void TheA1ReferenceUsesSpreadsheetColumnLettering(int column, int row, string expected)
    {
      var width = column + 1;
      var values = new int[row, width];

      for (var r = 0; r < row; r++)
        for (var c = 0; c < width; c++)
          values[r, c] = 1;

      values[row - 1, column - 1] = 0;   // the blank the projection will trip over

      var projection = OffsetBy(Then(SkipColumns(column - 1), SkipRows(row - 1))).Of(IntCell());
      var failure = Assert.Throws<ProjectionException>(() => projection.Map(Grid(values)));

      Assert.Equal(expected, failure.Location.A1);
      Assert.Equal($"row {row}, column {column} ({expected})", failure.Location.ToString());
    }

    // --- The exception's projection --------------------------------------------------------------------------------

    [Fact]
    public void TheMessageCarriesSubjectProblemPathAndLocationOnSeparateLines()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        TextCell().Named("title").Map(Square()));

      var lines = failure.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

      Assert.Equal(3, lines.Length);
      Assert.StartsWith("'title': ", lines[0]);
      Assert.StartsWith("  in ", lines[1]);
      Assert.StartsWith("  at ", lines[2]);
      Assert.EndsWith("available", lines[2]);
    }

    [Fact]
    public void TheExceptionPointsAtTheProjectionThatFailed()
    {
      var cell = TextCell().Named("title");

      var failure = Assert.Throws<ProjectionException>(() => cell.Map(Square()));

      Assert.Same(cell, failure.Projection);
      Assert.Equal("title", failure.Projection.Name);
    }

    [Fact]
    public void RequestedIsNullWhenNothingSpecificWasAskedFor()
    {
      var failure = Assert.Throws<ProjectionException>(() => TextCell().Map(Square()));

      Assert.Null(failure.Requested);
    }

    // --- No bare substrate exceptions escape ---------------------------------------------------------------------

    [Fact]
    public void MapNeverThrowsABareOutOfBoundsException()
    {
      var space = Square();

      Assert.Throws<ProjectionException>(() => Row(9, s => s.Count).Map(space));
      Assert.Throws<ProjectionException>(() => Column(9, s => s.Count).Map(space));
      Assert.Throws<ProjectionException>(() => Range(9, 9, b => b.Width).Map(space));
      Assert.Throws<ProjectionException>(() => Down(9).Of(IntCell()).Map(space));
      Assert.Throws<ProjectionException>(() => Row(ColumnStrategies.TakeColumns(9), s => s.Count).Map(space));
      Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}").Map(Grid(new[,] { { 1 } })));
    }

    [Fact]
    public void AProjectionExceptionIsWhatAViewLevelFailureBecomesToo()
    {
      // A view's own ArgumentOutOfRangeException happens inside a projection, so it arrives wrapped
      // with the same path and location as everything else.
      var failure = Assert.Throws<ProjectionException>(() => Range(b => b[9, 0].Integer()).Map(Square()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.InnerException);
      Assert.Contains("the projection threw ArgumentOutOfRangeException", failure.Message);
    }

    [Fact]
    public void ApplyRejectsNullArguments()
    {
      Assert.Throws<ArgumentNullException>(() => IntCell().Map(null!));
      Assert.Throws<ArgumentNullException>(() => ((IProjection<ISheetCells, int>)null!).Map(Square()));
    }

    private static int ThrowingSelector(int only)
      => throw new InvalidOperationException("the selector failed");

  }
}
