using System;

using Unrect.Core;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.Shapes.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// The diagnostics contract. Every failure in the fused layer is a <see cref="ShapeException"/>
  /// that names the shape, the declaration path that reached it, and where on the sheet it was
  /// looking. These tests pin the message's structure and the exact phrasings the spec fixes; they
  /// deliberately do not pin whole messages byte-for-byte.
  /// </summary>
  public class ShapeErrorTests
  {
    private static IShape<int> IntCell() => Cell(v => v.GetInt());

    private static ISpace Square() => Grid(new[,] { { 1, 2 }, { 3, 4 } });

    // --- Case A: the offset does not fit ---------------------------------------------------------------

    [Fact]
    public void AnOffsetThatRunsPastTheSpace_ReportsWhatWasRequestedAndWhatWasAvailable()
    {
      var failure = Assert.Throws<ShapeException>(() => IntCell().Down(5).Map(Square()));

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

      var failure = Assert.Throws<ShapeException>(() =>
        IntCell().After(Then(SkipRows(3), SkipRows(3))).Map(space));

      Assert.Contains("its offset ran past the available space", failure.Message);
      Assert.IsType<OutOfBoundsException>(failure.InnerException);
    }

    [Fact]
    public void ASeekThatFindsNothing_SaysWhatItWasLookingFor()
    {
      // A missing anchor resolves as a placement failure, but "its offset ran past the available
      // space" would be useless: the useful fact is which label was not there.
      var space = Mixed(new object?[,] { { "nothing", null }, { "relevant", null } });

      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).Named("taxable income").After(SeekRowContaining("Taxable Income")).Map(space));

      Assert.Equal("'taxable income'", failure.Subject);
      Assert.Contains("no row containing 'Taxable Income' exists in the available space", failure.Message);
      Assert.Contains("  in 'taxable income' (Cell)", failure.Message);
      Assert.Contains("(A1)", failure.Message);
      Assert.Contains("2x2 available", failure.Message);
    }

    [Fact]
    public void EverySeekDescribesItsOwnKindOfMiss()
    {
      var space = Mixed(new object?[,] { { "nothing", null }, { "relevant", null } });

      Assert.Contains("no row containing 'Total' exists", Missing(SeekRowContaining("Total"), space));
      Assert.Contains("no column containing 'Total' exists", Missing(SeekColumnContaining("Total"), space));
      Assert.Contains("no row with a matching cell exists", Missing(SeekRowWhere(_ => false), space));
      Assert.Contains("no column with a matching cell exists", Missing(SeekColumnWhere(_ => false), space));
      Assert.Contains("no matching row exists", Missing(SeekRow((_, _) => false), space));
      Assert.Contains("no matching column exists", Missing(SeekColumn((_, _) => false), space));
    }

    [Fact]
    public void ASeekMissNeverEscapesAsABareOutOfBoundsException()
    {
      var space = Mixed(new object?[,] { { "nothing", null } });

      Assert.Throws<ShapeException>(() => IntCell().After(SeekRowContaining("Total")).Map(space));
      Assert.Throws<ShapeException>(() => IntCell().After(FromRight(9)).Map(space));
      Assert.Throws<ShapeException>(() => IntCell().After(FromBottom(9)).Map(space));
    }

    [Fact]
    public void AnOffsetThatMerelyRunsOutOfRoom_StillSaysSo()
    {
      // The seek description replaces the generic wording only when there was an anchor to name.
      var space = Mixed(new object?[,] { { "nothing", null } });

      Assert.Contains("its offset ran past the available space", Missing(FromRight(9), space));
    }

    private static string Missing(IOffsetStrategy offset, ISpace space)
      => Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).After(offset).Map(space)).Message;

    // --- Case B: the area does not fit ------------------------------------------------------------------

    [Fact]
    public void AnAreaThatDoesNotFit_ReportsWhatWasRequestedAndWhatWasAvailable()
    {
      var failure = Assert.Throws<ShapeException>(() => Range(3, 3, b => b.Width).Map(Square()));

      Assert.Contains("an extent of 3x3 does not fit here", failure.Message);
      Assert.Contains("2x2 available", failure.Message);
      Assert.Equal(3, failure.Requested!.Value.Width);
      Assert.Equal(3, failure.Requested!.Value.Height);
    }

    [Fact]
    public void AnAreaStrategyThatThrows_IsReportedAsAnAreaFailure()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Row(ColumnStrategies.TakeColumns(9), s => s.Count).Map(Square()));

      Assert.Contains("its area ran past the space available here", failure.Message);
      Assert.IsType<OutOfBoundsException>(failure.InnerException);
    }

    // --- Strategies that fail in some way other than running out of space -------------------------------
    //
    // Running out of space is a stopping condition a Repeat is allowed to act on. Every other way a
    // strategy can fail is a broken declaration, and must arrive as a ShapeException that says which
    // strategy it was — not as a bare exception from somewhere in the strategy calculus.

    [Fact]
    public void AnAreaStrategyThatThrows_IsReportedAgainstTheShapeThatDeclaredIt()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Range(AreaStrategies.SelectArea(_ => throw new InvalidOperationException("boom")), b => b.Width).Map(Square()));

      Assert.Contains("its area strategy threw InvalidOperationException: boom", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Equal("Range", failure.Subject);
    }

    [Fact]
    public void AnOffsetStrategyThatThrows_IsReportedAgainstTheShapeThatDeclaredIt()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        IntCell().After(OffsetStrategies.SelectOffset(_ => throw new InvalidOperationException("boom"))).Map(Square()));

      Assert.Contains("its offset strategy threw InvalidOperationException: boom", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void AStrategyThatReturnsANegativeSize_IsReportedAsAStrategyFailure()
    {
      // Size rejects the negative itself; the engine's job is to say which strategy produced it.
      var failure = Assert.Throws<ShapeException>(() =>
        Range(AreaStrategies.SelectArea(_ => new Size(-1, 1)), b => b.Width).Map(Square()));

      Assert.Contains("its area strategy threw ArgumentOutOfRangeException", failure.Message);
      Assert.IsType<ArgumentOutOfRangeException>(failure.InnerException);
    }

    [Fact]
    public void ASeparatorStrategyThatThrows_IsReportedAgainstTheRepeat()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Repeat(Range(1, 1, b => b.Width), separatedBy: OffsetStrategies.SelectOffset(_ => throw new InvalidOperationException("boom")))
          .Map(Square()));

      Assert.Contains("its separator strategy threw InvalidOperationException: boom", failure.Message);
      Assert.Equal("Repeat", failure.Subject);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    // --- Case D: the projection threw -------------------------------------------------------------------

    [Fact]
    public void AProjectionThatThrows_IsWrappedWithItsPathAndLocation()
    {
      var failure = Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).Map(Square()));

      Assert.Contains("the projection threw InvalidOperationException", failure.Message);
      Assert.Contains("Cell value is Number; expected Text", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Contains("  in Cell", failure.Message);
      Assert.Contains("(A1)", failure.Message);
    }

    [Fact]
    public void AProjectionFailureIsWrappedOnceOnly()
    {
      // A failure that surfaces from deep in a tree passes back through every enclosing Project; it
      // must not accumulate a wrapper at each level.
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var shape = VerticalFlow(v =>
      {
        var first = v.Next(IntCell());
        v.Next(Repeat(Cell(c => c.GetString())).Named("items"));
        return first;
      });

      var failure = Assert.Throws<ShapeException>(() => shape.Map(space));

      Assert.IsType<InvalidOperationException>(failure.InnerException);
      Assert.Equal(1, Occurrences(failure.Message, "the projection threw"));
      Assert.Equal(1, Occurrences(failure.Message, "  in "));
    }

    [Fact]
    public void AnInvariantFailureIsNotWrappedByAnEnclosingShape()
    {
      var space = Mixed(new object?[,] { { "Investor" }, { "Acme" } });

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{string.Join(",", v.Next(TableRows(r => r["Amount"].GetInt())))}|{v.Next(IntCell())}").Map(space));

      Assert.Null(failure.InnerException);
      Assert.Equal(1, Occurrences(failure.Message, "  in "));
    }

    // --- Paths ------------------------------------------------------------------------------------------

    [Fact]
    public void AnUnnamedShape_IsIdentifiedByItsDescription()
    {
      var failure = Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).Map(Square()));

      Assert.Equal("Cell", failure.Subject);
      Assert.Equal("Cell", failure.Path);
    }

    [Fact]
    public void ANamedShape_IsIdentifiedByItsNameAndSaysWhatKindItIs()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).Named("report id").Map(Square()));

      Assert.Equal("'report id'", failure.Subject);
      Assert.Equal("'report id' (Cell)", failure.Path);
    }

    [Fact]
    public void TheKindSuffixOnlyDecoratesNamedSegments()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Named("header").Map(Square()));

      // The named flow is a plain segment; the failing segment carries the kind only when it
      // rendered as a quoted name, and an ordinal is not one.
      Assert.Equal("'header' -> Cell#2", failure.Path);
    }

    [Fact]
    public void EnclosingShapesAppearInThePathInDeclarationOrder()
    {
      var space = Grid(new[,] { { 1 }, { 2 } });

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v =>
        {
          v.Next(IntCell());
          return v.Next(VerticalFlow(w => $"{w.Next(IntCell())}{w.Next(Cell(c => c.GetString()))}").Named("inner"));
        }).Map(space));

      Assert.Contains(" -> ", failure.Path);
      Assert.StartsWith("VerticalFlow -> 'inner'", failure.Path);
    }

    [Fact]
    public void ARepeatDecoratesItsOwnSegmentWithTheItemIndex()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var failure = Assert.Throws<ShapeException>(() =>
        Repeat(Cell(v => v.GetString()).Named("code")).Map(space));

      Assert.Equal("Repeat[0] -> 'code' (Cell)", failure.Path);
    }

    [Fact]
    public void ARepeatsIndexCountsTheItemsAlreadyCollected()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 } });

      var failure = Assert.Throws<ShapeException>(() => Repeat(IntCell()).Map(space));

      Assert.Equal("Repeat[2] -> Cell", failure.Path);
    }

    [Fact]
    public void AnUnnamedSelectContributesNoPathSegment()
    {
      var withoutSelect = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Map(Square()));

      var withSelect = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Select(x => x).Map(Square()));

      Assert.Equal("VerticalFlow -> Cell#2", withoutSelect.Path);
      Assert.Equal(withoutSelect.Path, withSelect.Path);
      Assert.DoesNotContain("Select", withSelect.Path);
    }

    [Fact]
    public void ATransparentSelectIsStillBlamedWhenItsOwnSelectorThrows()
    {
      // Being skipped as an intermediate segment does not mean being unnameable as the culprit: the
      // failing shape is appended to the path even though it contributes no segment of its own.
      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v =>
        {
          v.Next(IntCell());
          return v.Next(HorizontalFlow(h => h.Next(IntCell())).Select<int, int>(ThrowingSelector));
        }).Map(Square()));

      Assert.Equal("Select#2", failure.Subject);
      Assert.Equal("VerticalFlow -> Select#2", failure.Path);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void ANamedSelectContributesAPathSegment()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(Cell(c => c.GetString()))}").Select(x => x).Named("report").Map(Square()));

      Assert.Equal("'report' -> VerticalFlow -> Cell#2", failure.Path);
    }

    [Fact]
    public void ATransparentSelectStillAccumulatesTheOrigin()
    {
      // Being skipped in the path must not mean being skipped in the coordinate arithmetic.
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 }, { 1, 0 } });

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v =>
        {
          v.Next(Range(2, 2, b => b.Width));
          return v.Next(IntCell().Right(1));
        }).Select(x => x).Map(space));

      Assert.Equal("B3", failure.Location.A1);
    }

    // --- Locations ----------------------------------------------------------------------------------------

    [Fact]
    public void TheLocationIsOneBasedAndRelativeToTheSpaceMapWasCalledWith()
    {
      var space = Grid(new[,] { { 1, 1 }, { 1, 1 }, { 1, 0 } });

      var failure = Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(Range(2, 2, b => b.Width))}{v.Next(IntCell().Right(1))}").Map(space));

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

      var shape = IntCell().After(Then(SkipColumns(column - 1), SkipRows(row - 1)));
      var failure = Assert.Throws<ShapeException>(() => shape.Map(Grid(values)));

      Assert.Equal(expected, failure.Location.A1);
      Assert.Equal($"row {row}, column {column} ({expected})", failure.Location.ToString());
    }

    // --- The exception's shape --------------------------------------------------------------------------------

    [Fact]
    public void TheMessageCarriesSubjectProblemPathAndLocationOnSeparateLines()
    {
      var failure = Assert.Throws<ShapeException>(() =>
        Cell(v => v.GetString()).Named("title").Map(Square()));

      var lines = failure.Message.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

      Assert.Equal(3, lines.Length);
      Assert.StartsWith("'title': ", lines[0]);
      Assert.StartsWith("  in ", lines[1]);
      Assert.StartsWith("  at ", lines[2]);
      Assert.EndsWith("available", lines[2]);
    }

    [Fact]
    public void TheExceptionPointsAtTheShapeThatFailed()
    {
      var cell = Cell(v => v.GetString()).Named("title");

      var failure = Assert.Throws<ShapeException>(() => cell.Map(Square()));

      Assert.Same(cell, failure.Shape);
      Assert.Equal("title", failure.Shape.Name);
    }

    [Fact]
    public void RequestedIsNullWhenNothingSpecificWasAskedFor()
    {
      var failure = Assert.Throws<ShapeException>(() => Cell(v => v.GetString()).Map(Square()));

      Assert.Null(failure.Requested);
    }

    // --- No bare substrate exceptions escape ---------------------------------------------------------------------

    [Fact]
    public void MapNeverThrowsABareOutOfBoundsException()
    {
      var space = Square();

      Assert.Throws<ShapeException>(() => Row(9, s => s.Count).Map(space));
      Assert.Throws<ShapeException>(() => Column(9, s => s.Count).Map(space));
      Assert.Throws<ShapeException>(() => Range(9, 9, b => b.Width).Map(space));
      Assert.Throws<ShapeException>(() => IntCell().Down(9).Map(space));
      Assert.Throws<ShapeException>(() => Row(ColumnStrategies.TakeColumns(9), s => s.Count).Map(space));
      Assert.Throws<ShapeException>(() =>
        VerticalFlow(v => $"{v.Next(IntCell())}{v.Next(IntCell())}{v.Next(IntCell())}").Map(Grid(new[,] { { 1 } })));
    }

    [Fact]
    public void AShapeExceptionIsWhatAViewLevelFailureBecomesToo()
    {
      // A view's own ArgumentOutOfRangeException happens inside a projection, so it arrives wrapped
      // with the same path and location as everything else.
      var failure = Assert.Throws<ShapeException>(() => Range(b => b[9, 0].GetInt()).Map(Square()));

      Assert.IsType<ArgumentOutOfRangeException>(failure.InnerException);
      Assert.Contains("the projection threw ArgumentOutOfRangeException", failure.Message);
    }

    [Fact]
    public void ApplyRejectsNullArguments()
    {
      Assert.Throws<ArgumentNullException>(() => IntCell().Map(null!));
      Assert.Throws<ArgumentNullException>(() => ((IShape<int>)null!).Map(Square()));
    }

    private static int ThrowingSelector(int only)
      => throw new InvalidOperationException("the selector failed");

    private static int Occurrences(string text, string value)
    {
      var count = 0;

      for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + 1, StringComparison.Ordinal))
        count++;

      return count;
    }
  }
}
