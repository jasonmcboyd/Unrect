using System;
using System.IO;

using Unrect.Core;
using Unrect.Spreadsheets;
using Unrect.Projections;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// The awkward cells: formula errors, and text that looks empty but is not. Both are adapter
  /// concerns — the adapter decides what a backend value becomes and what counts as empty space —
  /// so these tests read a real workbook and check the canonical values that come out of it.
  /// </summary>
  public class SpreadsheetSpaceEdgeCaseTests
  {
    /// <summary>
    /// The "Edges" sheet, 5 columns by 4 rows:
    /// <code>
    ///        0            1          2         3            4
    ///   0    "text"       42         3.14      2026-01-15   TRUE
    ///   1    #VALUE!      #DIV/0!    #N/A      #REF!        #NAME?
    ///   2    "  "         " "        ""        (no cell)    "x"
    ///   3    #NULL!       #NUM!      (none)    (none)       7
    /// </code>
    /// </summary>
    private static ICellSpace Edges(Func<string, bool>? isBlank = null)
      => SpreadsheetSpace.Create(
        Path.Combine(AppContext.BaseDirectory, "TestData", "edge-cases.xlsx"),
        "Edges",
        isBlank: isBlank);

    [Fact]
    public void TheFixtureIsFiveColumnsByFourRows()
    {
      var space = Edges();

      Assert.Equal(5, space.Area.Size.Width);
      Assert.Equal(4, space.Area.Size.Height);
    }

    // --- Ordinary kinds ---------------------------------------------------------------------------

    [Fact]
    public void TheFirstRowCarriesOneCellOfEachOrdinaryKind()
    {
      var space = Edges();

      var sheet = Plane<ICellSpace>.Of(space);

      Assert.Equal("text", sheet[0, 0].Text());
      Assert.Equal(42, sheet[1, 0].Integer());
      Assert.Equal(3.14m, sheet[2, 0].Decimal());
      Assert.Equal(new DateTime(2026, 1, 15), sheet[3, 0].Date().Date);
      Assert.True(sheet[4, 0].Boolean());
      Assert.Equal(7, sheet[4, 3].Integer());
    }

    // --- Errors ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1, "#VALUE!")]
    [InlineData(1, 1, "#DIV/0!")]
    [InlineData(2, 1, "#N/A")]
    [InlineData(3, 1, "#REF!")]
    [InlineData(4, 1, "#NAME?")]
    [InlineData(0, 3, "#NULL!")]
    [InlineData(1, 3, "#NUM!")]
    public void AnErrorCellReadsAsTheErrorTheSheetHolds(int column, int row, string spelling)
    {
      var space = Edges();

      Assert.True(space.ValueAt(column, row).Kind == CellKind.Error);
      Assert.Equal(spelling, space.AsText(column, row));
      Assert.Equal($"Error({spelling})", space.Describe(column, row));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 1)]
    [InlineData(0, 3)]
    [InlineData(1, 3)]
    public void AnErrorCellIsNeverBlank(int column, int row)
    {
      // The adapter reads the error before it reads the value: ExcelDataReader reports an error
      // cell's value as null, which would otherwise be adapted into a missing cell.
      var space = Edges();

      Assert.False(space.IsBlank(column, row));
      Assert.NotNull(space.AsText(column, row));
    }

    [Fact]
    public void TheDefaultBlanknessRuleCannotBlankAnError()
    {
      // #REF! is not text, so the whitespace rule never sees it — which is the right outcome: an
      // error is something the sheet says, not empty space to be skipped past.
      Assert.True(Edges().ValueAt(3, 1).Kind == CellKind.Error);
      Assert.Equal("#REF!", Edges().AsText(3, 1));
      Assert.True(Edges().ValueAt(0, 3).Kind == CellKind.Error);
      Assert.Equal("#NULL!", Edges().AsText(0, 3));
    }

    [Fact]
    public void ARowOfNothingButErrorsStillCarriesValues()
    {
      // The consequence that matters downstream: a discovered region does not stop at such a row.
      var errorsOnly = Edges().Region().Slice(new Offset(0, 1), new Area(5, 1));

      Assert.Equal(1, SizeStrategies.RowsWhileAnyIsNotBlank().GetSize(errorsOnly).Height);
    }

    // --- Blankness is the adapter's decision ---------------------------------------------------------

    [Fact]
    public void ByDefault_WhitespaceOnlyTextIsBlank()
    {
      // Exported workbooks are full of "  " cells that look empty and are meant to be empty; left
      // as text they would anchor a region that should have ended.
      var space = Edges();

      Assert.True(space.IsBlank(0, 2));   // two spaces
      Assert.True(space.IsBlank(1, 2));   // one space
      Assert.True(space.IsBlank(2, 2));   // an empty string
      Assert.True(space.IsBlank(3, 2));   // no cell at all
      Assert.Equal("x", space.AsText(4, 2));
    }

    [Fact]
    public void WithStrictFidelity_WhitespaceIsTextAgain()
    {
      var space = Edges(isBlank: _ => false);

      Assert.Equal("  ", space.AsText(0, 2));
      Assert.True(space.IsText(0, 2));
      Assert.Equal(" ", space.AsText(1, 2));
      Assert.Equal("x", space.AsText(4, 2));
    }

    [Fact]
    public void EvenUnderStrictFidelity_AnAbsentCellIsBlank()
    {
      // Null and empty are mapped to Blank before the predicate is consulted: whether a cell exists
      // is not a judgement call a blankness rule gets to overrule.
      var space = Edges(isBlank: _ => false);

      Assert.True(space.IsBlank(2, 2));
      Assert.True(space.IsBlank(3, 2));
    }

    [Fact]
    public void ACustomPredicateDecidesBlanknessForThisSheet()
    {
      var space = Edges(isBlank: text => text == "x");

      Assert.True(space.IsBlank(4, 2));

      // The custom rule replaces the default rather than adding to it, so whitespace is text again.
      Assert.Equal("  ", space.AsText(0, 2));
    }

    // --- Blankness changes decomposition, which is the whole point ---------------------------------------

    [Fact]
    public void BlanknessDecidesWhereADiscoveredRegionEnds()
    {
      // Column 4 carries "x" on the whitespace row, so the difference only shows on the columns
      // that do not: under the default the row is empty and ends the region; under strict fidelity
      // it carries two text cells and the region runs to the bottom of the sheet.
      var byDefault = Edges();
      var strict = Edges(isBlank: _ => false);

      var firstFour = new Offset(0, 0);
      var block = new Area(4, 4);

      Assert.Equal(2, SizeStrategies.RowsWhileAnyIsNotBlank().GetSize(byDefault.Region().Slice(firstFour, block)).Height);
      Assert.Equal(4, SizeStrategies.RowsWhileAnyIsNotBlank().GetSize(strict.Region().Slice(firstFour, block)).Height);

      // ...and the leaf that discovers its own extent sees exactly what the strategy does, which is
      // the half that says blankness reaches the declaration and not merely the calculus. The first
      // four columns are named here because the fifth carries "x" on the whitespace row under every
      // rule, and a region that included it could not tell the two apart.
      var fourColumns = AreaStrategies.ColumnsThenRows(
        ColumnStrategies.TakeColumns(4),
        RowStrategies.TakeRowsWhileAnyIsNotBlank());

      Assert.Equal((4, 2), Range(fourColumns, b => (b.Width, b.Height)).Map(byDefault));
      Assert.Equal((4, 4), Range(fourColumns, b => (b.Width, b.Height)).Map(strict));
    }

    [Fact]
    public void BlanknessDecidesWhetherThereIsAGapToSkip()
    {
      var byDefault = Edges().Region().Slice(new Offset(0, 2), new Area(4, 2));
      var strict = Edges(isBlank: _ => false).Region().Slice(new Offset(0, 2), new Area(4, 2));

      Assert.Equal(1, OffsetStrategies.SkipBlankRows().GetOffset(byDefault).Size.Height);
      Assert.Equal(0, OffsetStrategies.SkipBlankRows().GetOffset(strict).Size.Height);
    }

    [Fact]
    public void TheWholeSheetLooksTheSameToBothSpacesWhereItCarriesRealValues()
    {
      // Blankness only ever reclassifies whitespace text: every other cell reads identically, so
      // choosing a rule cannot quietly change what a value is.
      var byDefault = Edges();
      var strict = Edges(isBlank: _ => false);

      foreach (var (column, row) in new[] { (0, 0), (1, 0), (3, 0), (0, 1), (4, 2), (4, 3) })
      {
        Assert.Equal(byDefault.Describe(column, row), strict.Describe(column, row));
        Assert.Equal(byDefault.AsText(column, row), strict.AsText(column, row));
        Assert.Equal(byDefault.IsText(column, row), strict.IsText(column, row));
      }
    }

    // --- An error the adapter cannot name -------------------------------------------------------
    //
    // On the .xls path the reader casts a raw byte from the file to its own error enum, so an
    // undefined code is a workbook this library should still read: the adapter maps it to Other
    // carrying whatever the reader called it, and never throws.
    //
    // These pin the value the adapter is required to produce, not the mapping itself. The mapping
    // lives in an internal extension of Unrect.Spreadsheets, which grants no InternalsVisibleTo, and no
    // committed fixture can drive it: an .xls with a byte no version of Excel writes is not
    // something this project can honestly author. The gap is between "the adapter picks Other and
    // the reader's text" and "Other plus that text behaves like this" — the latter is here.

    [Fact]
    public void AnErrorWithNoCanonicalNameIsCarriedRatherThanRejected()
    {
      // What the adapter builds when its switch falls through: the code it could not name, plus
      // the reader's own text for it, which for an undefined enum value is the number itself.
      var space = SheetGrid.Of(new object?[,] { { CellValue.OfError(CellError.Other, "42") } });

      Assert.True(space.ValueAt(0, 0).Kind == CellKind.Error);
      Assert.False(space.IsBlank(0, 0));
      Assert.Equal("42", space.AsText(0, 0));
    }

    [Fact]
    public void AnErrorTheAdapterCannotNameStillSaysSomethingUseful()
    {
      // The reason the literal is carried at all: a reader looking at this message has a string to
      // search the file for. "Error(Other)" would tell them only that something, somewhere, failed.
      var space = SheetGrid.Of(new object?[,] { { CellValue.OfError(CellError.Other, "42") } });

      Assert.Equal("Error(42)", space.Describe(0, 0));
      Assert.Equal("42", space.AsText(0, 0));

      Assert.False(Plane<ICellSpace>.Of(space)[0, 0].TryGetDouble(out _, out var problem));
      Assert.Equal("expected Number at B4, found Error(42)", problem!.Value.Render("B4"));
    }
  }
}
