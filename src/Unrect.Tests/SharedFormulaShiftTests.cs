using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The shifter behind a shared formula's follower, asserted directly rather than through a file.
  /// <para>
  /// It is reached through a workbook in <see cref="FormulaCapabilityTests"/>, which is the honest
  /// end-to-end statement; this class is the adversarial one. What it exercises cannot be arranged in
  /// a fixture without writing a workbook per case — a formula mentioning a table, an external
  /// workbook, a whole column, a reference one step from the edge of the sheet — and the interesting
  /// half of those are the strings a reference <em>finder</em> must decline to touch. Excel writes
  /// none of them into a shared group by accident; a real file eventually will.
  /// </para>
  /// <para>
  /// The reader is a reference finder rather than a formula parser, and the rules it judges a run by
  /// are three characters: a run followed by <c>(</c> is a function, by <c>!</c> a sheet, by
  /// <c>[</c> a table. Everything below is that one sentence being held to.
  /// </para>
  /// </summary>
  public class SharedFormulaShiftTests
  {
    // --- The $ marks decide what moves --------------------------------------------------------------

    [Theory]
    [InlineData("A1", "B3")]        // both halves relative: both move
    [InlineData("$A$1", "$A$1")]    // both pinned: neither does
    [InlineData("$A1", "$A3")]      // the column is pinned, the row is not
    [InlineData("A$1", "B$1")]      // and the other way round
    public void EachDollarMarkPinsItsOwnHalfOfTheReference(string formula, string expected)
      => Assert.Equal(expected, SharedFormulas.Shift(formula, columnDelta: 1, rowDelta: 2));

    [Fact]
    public void ARangeMovesAtBothEnds()
    {
      Assert.Equal("SUM(B3:D5)", SharedFormulas.Shift("SUM(A1:C3)", 1, 2));
      Assert.Equal("SUM($A$1:B3)", SharedFormulas.Shift("SUM($A$1:A1)", 1, 2));
    }

    [Fact]
    public void ShiftingNowhereIsTheIdentity()
    {
      // The master's own cell. It is the common case in any group — one cell in it is written out —
      // and the fast path has to be exactly the identity rather than approximately it.
      const string Formula = @"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")";

      Assert.Same(Formula, SharedFormulas.Shift(Formula, 0, 0));
      Assert.Same(string.Empty, SharedFormulas.Shift(string.Empty, 3, 4));
    }

    // --- What is not a reference, however much it looks like one ------------------------------------

    [Theory]
    [InlineData(@"IF(A1>0,""A1"",""B2"")", @"IF(B2>0,""A1"",""B2"")")]
    [InlineData(@"CONCAT(""say """"A1"""" now"",A1)", @"CONCAT(""say """"A1"""" now"",B2)")]
    public void TextInsideAStringLiteralIsNeverAReference(string formula, string expected)
      => Assert.Equal(expected, SharedFormulas.Shift(formula, 1, 1));

    [Fact]
    public void AFunctionNameIsJudgedByTheParenthesisAfterIt()
    {
      // The classic false reference: LOG10 is column LOG, row 10, spelled exactly like a cell. What
      // makes it a function here is the ( that follows it, and nothing else.
      Assert.Equal("LOG10(B8)+B8", SharedFormulas.Shift("LOG10(A8)+A8", 1, 0));

      // ...so the same run WITHOUT the parenthesis is a reference, and moves. That is not a
      // concession, it is the rule: a bare LOG10 in a formula IS the cell LOG10, and one column
      // right of column LOG is column LOH.
      Assert.Equal("LOH10+1", SharedFormulas.Shift("LOG10+1", 1, 0));
    }

    [Theory]
    [InlineData("A1*1.5E+10", "B2*1.5E+10")]                    // an exponent is not a column and a row
    [InlineData("A1*2", "B2*2")]                                // a bare number is not either
    [InlineData("Q1_total+A1", "Q1_total+B2")]                  // a defined name that starts like one
    [InlineData("TRUE+A1", "TRUE+B2")]                          // ...and a boolean
    public void ARunThatIsNotSpelledLikeACellIsLeftAlone(string formula, string expected)
      => Assert.Equal(expected, SharedFormulas.Shift(formula, 1, 1));

    [Theory]
    [InlineData("SUM(Table1[[Qty]:[Total]])+A1", "SUM(Table1[[Qty]:[Total]])+B2")]
    [InlineData("SUM(Table1[Qty])+A1", "SUM(Table1[Qty])+B2")]
    public void AStructuredReferenceIsCopiedWhole(string formula, string expected)
      // Column names inside brackets are names, and the brackets nest. Shifting one would rewrite a
      // table's schema into a coordinate.
      => Assert.Equal(expected, SharedFormulas.Shift(formula, 1, 1));

    // --- Where the sheet is named -------------------------------------------------------------------

    [Theory]
    [InlineData("Sheet1!A1", "Sheet1!B2")]
    [InlineData("'My Sheet'!A1", "'My Sheet'!B2")]
    [InlineData("[1]Sheet1!A1", "[1]Sheet1!B2")]
    [InlineData("'[1]My Sheet'!$A$1+A1", "'[1]My Sheet'!$A$1+B2")]
    public void AQualifiedReferenceMovesItsCellAndKeepsItsSheet(string formula, string expected)
      // The sheet name is a name — it is followed by ! — and an external workbook's index is
      // bracketed. What moves is the cell after them, exactly as it would have unqualified.
      => Assert.Equal(expected, SharedFormulas.Shift(formula, 1, 1));

    // --- Whole columns and whole rows ---------------------------------------------------------------

    [Fact]
    public void AWholeColumnMovesWithColumnsAndIgnoresRows()
    {
      Assert.Equal("SUM(B:B)", SharedFormulas.Shift("SUM(A:A)", 1, 0));
      Assert.Equal("SUM(A:A)", SharedFormulas.Shift("SUM(A:A)", 0, 7));
      Assert.Equal("SUM($A:B)", SharedFormulas.Shift("SUM($A:A)", 1, 0));
    }

    [Fact]
    public void AWholeRowMovesWithRowsAndIgnoresColumns()
    {
      Assert.Equal("SUM(3:3)", SharedFormulas.Shift("SUM(1:1)", 0, 2));
      Assert.Equal("SUM(1:1)", SharedFormulas.Shift("SUM(1:1)", 7, 0));
    }

    [Fact]
    public void AnAxisIsOnlyAnAxisNextToItsColon()
    {
      // Half a whole-column range and a plain number are the same characters. What tells them apart
      // is the colon on one side, so a run with no colon beside it is a name or a literal and stays.
      Assert.Equal("SUM(A:A)+1", SharedFormulas.Shift("SUM(A:A)+1", 0, 5));
      Assert.Equal("B2*3", SharedFormulas.Shift("A1*3", 1, 1));
    }

    // --- The edges of the sheet ---------------------------------------------------------------------
    //
    // Excel's own answer for a reference shifted off the grid is #REF!, and it is the answer here for
    // the same reason it is everywhere else in this reader: the alternative is a coordinate that does
    // not exist, spelled convincingly. (This is also the one place the openpyxl cross-check does not
    // agree — Translator either refuses or invents the column after XFD — so it is pinned here rather
    // than resting on that evidence.)

    [Theory]
    [InlineData("XFD1", 1, 0)]              // past the last column
    [InlineData("A1048576", 0, 1)]          // past the last row
    [InlineData("A1", -1, 0)]               // left of column A
    [InlineData("A1", 0, -1)]               // above row 1
    public void AReferenceShiftedOffTheSheetBecomesTheErrorExcelWrites(string formula, int columnDelta, int rowDelta)
      => Assert.Equal("#REF!", SharedFormulas.Shift(formula, columnDelta, rowDelta));

    [Fact]
    public void AReferenceThatStopsAtTheEdgeIsStillAReference()
    {
      // One short of the failures above: the last column and the last row are addressable, so
      // arriving at them is not an error. A check written with >= would turn the far corner of every
      // sheet into #REF!.
      Assert.Equal("XFD1", SharedFormulas.Shift("XFC1", 1, 0));
      Assert.Equal("A1048576", SharedFormulas.Shift("A1048575", 0, 1));
      Assert.Equal("A1", SharedFormulas.Shift("B2", -1, -1));
    }

    [Fact]
    public void APinnedHalfCannotBeShiftedOffTheSheetAtAll()
    {
      // It never moves, so there is no edge for it to fall off — including when the other half does.
      Assert.Equal("$A$1", SharedFormulas.Shift("$A$1", -5, -5));
      Assert.Equal("$XFD3", SharedFormulas.Shift("$XFD1", 1, 2));
    }

    [Fact]
    public void OnlyTheReferenceThatLeftTheSheetIsReplaced()
    {
      // The error is per reference, not per formula: what is still on the sheet still reads.
      Assert.Equal("SUM(#REF!:A1)", SharedFormulas.Shift("SUM(A1:B2)", -1, -1));
      Assert.Equal("SUM(#REF!:#REF!)", SharedFormulas.Shift("SUM(A:A)", -1, 0));
    }
  }
}
