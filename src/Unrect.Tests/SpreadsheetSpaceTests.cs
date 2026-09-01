using System;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Excel;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The Excel adapter on its own: sheet selection, dimensions, blankness, and the cell kinds a real
  /// file adapts to. Decomposing these same workbooks into typed results is the shape layer's job and
  /// is covered end to end by <c>Shapes/ShapeExampleTests</c>.
  /// </summary>
  public class SpreadsheetSpaceTests
  {
    // The workbooks are copied into the test output, so tests never depend on the repository layout.
    private static string WorkbookPath(string fileName)
      => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private static ISpace SimpleReport() => SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "Report");

    private static ISpace InvestorsByDeal() => SpreadsheetSpace.Create(WorkbookPath("investors-by-deal.xlsx"), "Investors");

    // --- Adapter behaviour ------------------------------------------------------------------------

    [Fact]
    public void Create_ReadsTheSheetDimensions()
    {
      var space = SimpleReport();

      Assert.Equal(4, space.Area.Size.Width);
      Assert.Equal(16, space.Area.Size.Height);
    }

    [Fact]
    public void Create_MatchesSheetNamesCaseInsensitivelyByDefault()
    {
      var space = SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "report");

      Assert.Equal(16, space.Area.Size.Height);
    }

    [Fact]
    public void Create_WithCaseSensitiveMatchingAndTheWrongCase_FindsNoSheet()
    {
      Assert.Throws<InvalidOperationException>(() =>
        SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "report", caseSensitive: true));
    }

    [Fact]
    public void Create_WithAPredicate_ExposesTheSheetIndexAndName()
    {
      var contexts = new System.Collections.Generic.List<SpreadsheetContext>();

      var sheets = SpreadsheetSpace
        .Create(WorkbookPath("investors-by-deal.xlsx"), context =>
        {
          contexts.Add(context);
          return true;
        })
        .ToArray();

      Assert.Single(sheets);
      Assert.Equal(6, sheets[0].Area.Size.Width);
      Assert.Equal(18, sheets[0].Area.Size.Height);

      var only = Assert.Single(contexts);
      Assert.Equal(0, only.Index);
      Assert.Equal("Investors", only.Name);
    }

    [Fact]
    public void EmptyCellsInsideTheGrid_AreBlank()
    {
      var space = SimpleReport();

      // The title row has a value only in column 0; the rest of the row is genuinely empty.
      Assert.True(space[0, 0].HasValue);
      Assert.True(space[1, 0].IsBlank);
      Assert.Same(CellValue.Blank, space[1, 0]);
      Assert.Same(CellValue.Blank, space[3, 0]);
    }

    [Fact]
    public void SeparatorRowsBetweenBlocks_AreEntirelyBlank()
    {
      var space = InvestorsByDeal();

      Assert.All(
        Enumerable.Range(0, space.Area.Size.Width),
        column => Assert.True(space[column, 5].IsBlank));
    }

    [Fact]
    public void CellKinds_FollowTheUnderlyingSheetValues()
    {
      var space = SimpleReport();

      Assert.Equal(CellKind.Text, space[0, 0].Kind);
      Assert.Equal(CellKind.Temporal, space[0, 2].Kind);
      Assert.Equal(CellKind.Number, space[3, 8].Kind);
      Assert.Equal(CellKind.Blank, space[1, 4].Kind);
    }
  }
}
