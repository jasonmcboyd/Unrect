using System;
using System.IO;

using Unrect.Core;
using Unrect.Excel;
using Unrect.Shapes;
using Unrect.Strategies;

using Xunit;

using static Unrect.Shapes.Shape;

namespace Unrect.Tests
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
    private static ISpace Edges(Func<CellValue, bool>? isBlank = null)
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

      Assert.Equal("text", space[0, 0].GetString());
      Assert.Equal(42, space[1, 0].GetInt());
      Assert.Equal(3.14m, space[2, 0].GetDecimal());
      Assert.Equal(new DateTime(2026, 1, 15), space[3, 0].GetDate());
      Assert.True(space[4, 0].GetBoolean());
      Assert.Equal(7, space[4, 3].GetInt());
    }

    // --- Errors ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1, CellError.Value)]
    [InlineData(1, 1, CellError.DivisionByZero)]
    [InlineData(2, 1, CellError.NotAvailable)]
    [InlineData(3, 1, CellError.Reference)]
    [InlineData(4, 1, CellError.Name)]
    [InlineData(0, 3, CellError.Null)]
    [InlineData(1, 3, CellError.Number)]
    public void AnErrorCellReadsAsTheErrorTheSheetHolds(int column, int row, CellError expected)
    {
      var cell = Edges()[column, row];

      Assert.Equal(CellKind.Error, cell.Kind);
      Assert.Equal(expected, cell.GetError());
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
      var cell = Edges()[column, row];

      Assert.True(cell.HasValue);
      Assert.False(cell.IsBlank);
    }

    [Fact]
    public void TheDefaultBlanknessRuleCannotBlankAnError()
    {
      // #REF! is not text, so the whitespace rule never sees it — which is the right outcome: an
      // error is something the sheet says, not empty space to be skipped past.
      Assert.Equal(CellError.Reference, Edges()[3, 1].GetError());
      Assert.Equal(CellError.Null, Edges()[0, 3].GetError());
    }

    [Fact]
    public void ARowOfNothingButErrorsStillCarriesValues()
    {
      // The consequence that matters downstream: a discovered region does not stop at such a row.
      var errorsOnly = Edges().GetSubspace(new Offset(0, 1), new Area(5, 1));

      Assert.Equal(1, SizeStrategies.RowsWhileAnyValue().GetSize(errorsOnly).Height);
    }

    // --- Blankness is the adapter's decision ---------------------------------------------------------

    [Fact]
    public void ByDefault_WhitespaceOnlyTextIsBlank()
    {
      // Exported workbooks are full of "  " cells that look empty and are meant to be empty; left
      // as text they would anchor a region that should have ended.
      var space = Edges();

      Assert.True(space[0, 2].IsBlank);   // two spaces
      Assert.True(space[1, 2].IsBlank);   // one space
      Assert.True(space[2, 2].IsBlank);   // an empty string
      Assert.True(space[3, 2].IsBlank);   // no cell at all
      Assert.Equal("x", space[4, 2].GetString());
    }

    [Fact]
    public void WithStrictFidelity_WhitespaceIsTextAgain()
    {
      var space = Edges(isBlank: _ => false);

      Assert.Equal("  ", space[0, 2].GetString());
      Assert.Equal(" ", space[1, 2].GetString());
      Assert.Equal("x", space[4, 2].GetString());
    }

    [Fact]
    public void EvenUnderStrictFidelity_AnAbsentCellIsBlank()
    {
      // Null and empty are mapped to Blank before the predicate is consulted: whether a cell exists
      // is not a judgement call a blankness rule gets to overrule.
      var space = Edges(isBlank: _ => false);

      Assert.True(space[2, 2].IsBlank);
      Assert.True(space[3, 2].IsBlank);
    }

    [Fact]
    public void ACustomPredicateDecidesBlanknessForThisSheet()
    {
      var space = Edges(isBlank: v => v.TryGetString() == "x");

      Assert.True(space[4, 2].IsBlank);

      // The custom rule replaces the default rather than adding to it, so whitespace is text again.
      Assert.Equal("  ", space[0, 2].GetString());
    }

    // --- Blankness changes decomposition, which is the whole point ---------------------------------------

    [Fact]
    public void BlanknessDecidesWhereADiscoveredRegionEnds()
    {
      // Column 4 carries "x" on the whitespace row, so the difference only shows on the columns
      // that do not: under the default the row is empty and ends the region; under strict fidelity
      // it carries two text cells and the region runs to the bottom of the sheet.
      var byDefault = Edges().GetSubspace(new Offset(0, 0), new Area(4, 4));
      var strict = Edges(isBlank: _ => false).GetSubspace(new Offset(0, 0), new Area(4, 4));

      Assert.Equal(2, SizeStrategies.RowsWhileAnyValue().GetSize(byDefault).Height);
      Assert.Equal(4, SizeStrategies.RowsWhileAnyValue().GetSize(strict).Height);

      Assert.Equal((4, 2), Cells(b => (b.Width, b.Height)).Map(byDefault));
      Assert.Equal((4, 4), Cells(b => (b.Width, b.Height)).Map(strict));
    }

    [Fact]
    public void BlanknessDecidesWhetherThereIsAGapToSkip()
    {
      var byDefault = Edges().GetSubspace(new Offset(0, 2), new Area(4, 2));
      var strict = Edges(isBlank: _ => false).GetSubspace(new Offset(0, 2), new Area(4, 2));

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

      Assert.Equal(byDefault[0, 0], strict[0, 0]);
      Assert.Equal(byDefault[1, 0], strict[1, 0]);
      Assert.Equal(byDefault[3, 0], strict[3, 0]);
      Assert.Equal(byDefault[0, 1], strict[0, 1]);
      Assert.Equal(byDefault[4, 2], strict[4, 2]);
      Assert.Equal(byDefault[4, 3], strict[4, 3]);
    }
  }
}
