using System;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Spreadsheets;

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
      var failure = Assert.Throws<ArgumentException>(() =>
        SpreadsheetSpace.Create(WorkbookPath("simple-report.xlsx"), "report", caseSensitive: true));

      Assert.Equal("sheetName", failure.ParamName);
      Assert.Contains("No sheet named 'report'", failure.Message);
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
      // Was Assert.Same: CellValue is a value type, so blankness is a value, not an instance.
      Assert.Equal(CellValue.Blank, space[1, 0]);
      Assert.Equal(CellValue.Blank, space[3, 0]);
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

    // --- A sheet that will not say how big it is -------------------------------------------------
    //
    // no-extent.xlsx has four rows of formatted, valueless cells, with the dimension element
    // stripped. The missing dimension is not by itself what does it — ExcelDataReader pre-scans the
    // cells on every format and derives its counts from them — so the reachable trigger is that no
    // cell carries a VALUE for that scan to find, which is what a pre-formatted export region looks
    // like. The reader then reports neither a row count nor a field count for it, and sizing a grid
    // from those counts yielded an empty space for a sheet with rows in it, and said nothing about
    // having done so.
    //
    // Coverage this file does NOT give, recorded so nobody reads more into it than it says: the
    // measured fill takes each row's own width and keeps the widest, and no committed file exercises
    // that rule. None can — the one condition that reaches the measured path is a sheet with no
    // valued cell, which is exactly the condition in which the reader names no columns at all, so
    // every file that gets there is zero wide however ragged its rows are (these are 3, 3, 2 and 4
    // cells, and all of them measure 0). The rule is mirrored from Workbook.Measure as
    // forward-proofing; "a width learned from the rows rather than declared" is pinned at the
    // streaming door, where a fake row source can report one — see
    // Streaming/SheetStoreTests.ASheetThatReportsNoDimensionIsMeasuredByReadingIt.

    [Fact]
    public void Create_OnASheetThatReportsNoExtent_MeasuresItByReadingTheRows()
    {
      var space = SpreadsheetSpace.Create(WorkbookPath("no-extent.xlsx"), "Undeclared");

      // Four rows, because four rows arrive. Zero wide, because not one of them carries a value and
      // the reader will not name a column it never saw one in — an honest extent either way, where
      // the height was previously 0 for no reason the file gave. Both axes, because the fix is a
      // claim about the whole extent: a width that started reporting something here would mean the
      // adapter had begun inventing columns out of formatting.
      Assert.Equal(4, space.Area.Size.Height);
      Assert.Equal(0, space.Area.Size.Width);
    }

    [Fact]
    public void TheMeasuredExtentOfASheet_BoundsItLikeADeclaredOneWould()
    {
      // The measured extent is a real extent: it bounds the space the way a declared one does, so a
      // declaration that runs off it gets the ordinary bounds condition rather than blank rows
      // stretching away past the end of the file. Its two axes fail in different places, so both are
      // said.
      var space = SpreadsheetSpace.Create(WorkbookPath("no-extent.xlsx"), "Undeclared");

      // Zero wide means there is no cell to read at all, the first one included — that is the whole
      // of what a zero-wide space refuses, and it refuses it rather than answering Blank, which a
      // scan would happily walk forever.
      Assert.Throws<OutOfBoundsException>(() => space[0, 0]);

      // The rows are real even though no cell is, so they bound a slice the way a declared height
      // would: four are there to be taken, and a fifth is off the end of the file. This is the half
      // the indexer cannot state — with no columns, every cell read fails on the width before it
      // ever reaches the height.
      Assert.Equal(4, space.GetSubspace(new Offset(0, 0), new Area(0, 4)).Area.Size.Height);
      Assert.Throws<OutOfBoundsException>(() => space.GetSubspace(new Offset(0, 0), new Area(0, 5)));
    }

    // --- Repeated text is shared ------------------------------------------------------------------
    //
    // repeated-text.xlsx is the fixture these read, and it is the only committed file written to have
    // the properties they need (the csproj entry records them): its text is spelled INLINE, so the
    // reader hands the adapter a fresh instance per cell rather than the deduplicated one a
    // shared-string table would give, and its duplicates include the length guard's two neighbours.
    // Several other fixtures happen to repeat a value — every committed workbook is written inline —
    // but they repeat it incidentally, and a test resting on that would be pinning an accident.
    //
    //         A              B               C                      D
    //   Ledger
    //   1     Fund           Category        Note                   Amount
    //   2     Alpha Fund     Capital Call    256 characters         1000
    //   3     Alpha Fund     Capital Call    256 characters         2000
    //   4     Beta Fund      Distribution    257 characters         3000
    //   5     Beta Fund      Distribution    257 characters         4000
    //   Notes
    //   1     Fund
    //   2     Alpha Fund
    //   3     Beta Fund
    //
    // What is NOT covered here, recorded so nobody reads more into it than it says: the measured fill
    // (ReadMeasured, for a sheet whose reader will not say how big it is) shares through the same
    // table by the same call, and no file can show it doing so. The one condition that reaches that
    // path is a sheet with no VALUED cell — which is a sheet with no text to share. The rule is the
    // same one already recorded for the measured path's width above, and for the same reason.

    private static ISpace RepeatedText(string sheetName = "Ledger")
      => SpreadsheetSpace.Create(WorkbookPath("repeated-text.xlsx"), sheetName);

    [Fact]
    public void EqualTextCellsInOneSheetAreGivenOneInstanceOfTheirCharacters()
    {
      // What sharing removes is the duplicate that would have SURVIVED the fill, not the one the
      // reader made: on a file that spells its text inline the reader hands over a fresh instance per
      // cell, and on a text-heavy sheet those copies are most of what the grid retains.
      var space = RepeatedText();

      Assert.Same(space[0, 1].GetString(), space[0, 2].GetString());     // "Alpha Fund", twice
      Assert.Same(space[1, 1].GetString(), space[1, 2].GetString());     // "Capital Call", twice

      // A different value is a different instance, which is the half that says the first assertion is
      // about identity rather than about the adapter handing back one string for everything.
      Assert.NotSame(space[0, 1].GetString(), space[0, 3].GetString());
      Assert.Equal("Alpha Fund", space[0, 2].GetString());
      Assert.Equal("Beta Fund", space[0, 3].GetString());
    }

    [Fact]
    public void TextIsSharedAcrossEverySheetOfOneCreateCall()
    {
      // The table is scoped to the call, not to a sheet: captions, codes and categories repeat across
      // the sheets of a workbook, so a caller enumerating several of them gets one instance per
      // distinct value across all of them.
      var sheets = SpreadsheetSpace.Create(WorkbookPath("repeated-text.xlsx"), _ => true).ToArray();
      var ledger = sheets[0];
      var notes = sheets[1];

      Assert.Same(ledger[0, 0].GetString(), notes[0, 0].GetString());    // "Fund"
      Assert.Same(ledger[0, 1].GetString(), notes[0, 1].GetString());    // "Alpha Fund"

      // ...and the scope is the call. A second call reads the file again and builds its own table, so
      // nothing here is a process-wide intern pool that would outlive the grid it was made for.
      Assert.NotSame(ledger[0, 1].GetString(), RepeatedText()[0, 1].GetString());
    }

    [Fact]
    public void AStringAtTheLengthGuardIsSharedAndOneCharacterLongerIsNot()
    {
      // The eager door applies the same guard as the streaming one, at the same boundary, so the two
      // doors share exactly the same values and a caller cannot tell which one produced a grid. Long
      // text in a spreadsheet is a memo or a free-text note — nearly always unique, so it would
      // occupy an entry that never scores a hit while pinning the most bytes of anything in the table.
      var space = RepeatedText();

      Assert.Equal(256, space[2, 1].GetString().Length);
      Assert.Same(space[2, 1].GetString(), space[2, 2].GetString());

      Assert.Equal(257, space[2, 3].GetString().Length);
      Assert.NotSame(space[2, 3].GetString(), space[2, 4].GetString());

      // Not shared is not the same as not equal: the cell is exactly what the file says either way.
      Assert.Equal(space[2, 3], space[2, 4]);
    }

    [Fact]
    public void SharingLeavesEveryOtherKindAlone()
    {
      // Strings only. A number, a date, a boolean and a blank hold no heap object to share, and the
      // grid must come back from a fill that shares text indistinguishable from one that did not.
      // (The kinds themselves are pinned in full by SpreadsheetSpaceEdgeCaseTests; this is the one
      // claim those tests cannot make, because they do not know a table exists.)
      var space = RepeatedText();

      Assert.Equal(CellKind.Number, space[3, 1].Kind);
      Assert.Equal(1000, space[3, 1].GetInt());
      Assert.Equal(4000, space[3, 4].GetInt());
      Assert.True(space[0, 0].HasValue);
    }
  }
}
