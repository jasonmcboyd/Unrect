using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// The one place the file's 1-based, letter-columned coordinates become a space's 0-based ones —
  /// both ways, since a shifted formula has to be spelled back into letters.
  /// <para>
  /// Two things make this worth its own class. The conversion is used by every formula the reader
  /// returns, so an off-by-one here is an off-by-one in all of them at once; and it is the piece with
  /// genuine arithmetic in it, because column letters are bijective base-26 and not base-26 — there
  /// is no zero digit, so Z is followed by AA rather than by BA, and the carries only misbehave at
  /// the boundaries a hand-picked case list would miss.
  /// </para>
  /// </summary>
  public class A1ReferenceTests
  {
    // --- Column letters -----------------------------------------------------------------------------

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]        // the first carry: bijective, so Z is followed by AA
    [InlineData(51, "AZ")]
    [InlineData(52, "BA")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]      // the second carry
    [InlineData(16383, "XFD")]    // the last column a sheet has
    public void AColumnIsSpelledInBijectiveBase26(int column, string letters)
      => Assert.Equal(letters, A1Reference.ColumnName(column));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(26)]
    [InlineData(27)]
    [InlineData(51)]
    [InlineData(52)]
    [InlineData(701)]
    [InlineData(702)]
    [InlineData(703)]
    [InlineData(16382)]
    [InlineData(16383)]
    public void SpellingAColumnAndReadingItBackIsTheIdentity(int column)
    {
      var letters = A1Reference.ColumnName(column);

      Assert.Equal(column, A1Reference.ColumnIndex(letters, 0, letters.Length));
      Assert.True(A1Reference.TryParse(letters + "1", out var parsed, out var row));
      Assert.Equal(column, parsed);
      Assert.Equal(0, row);
    }

    [Fact]
    public void LowerCaseLettersNameTheSameColumn()
      // Nothing in the format promises the case of an r attribute, and a formula may be typed in
      // either. The two spellings must not be two columns.
      => Assert.Equal(A1Reference.ColumnIndex("AB", 0, 2), A1Reference.ColumnIndex("ab", 0, 2));

    // --- Whole references ---------------------------------------------------------------------------

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("D14", 3, 13)]
    [InlineData("AB12", 27, 11)]
    [InlineData("XFD1048576", 16383, 1048575)]    // the far corner of a sheet
    public void AReferenceNamesTheZeroBasedCellUnderIt(string reference, int column, int row)
    {
      Assert.True(A1Reference.TryParse(reference, out var parsedColumn, out var parsedRow));
      Assert.Equal(column, parsedColumn);
      Assert.Equal(row, parsedRow);
    }

    [Fact]
    public void ARunSpelledLikeAFunctionIsStillSpelledLikeACell()
    {
      // LOG10 is the reader's standing example of a false reference, and it is worth being exact
      // about WHERE the falseness lives: not here. LOG10 is a perfectly well-formed cell reference —
      // column LOG, row 10 — and this parser, whose job is the r attribute of a cell element, says
      // so. What decides that a LOG10 inside a formula is a function is the ( after it, which is a
      // judgment about context and belongs to the shifter (see SharedFormulaShiftTests).
      Assert.True(A1Reference.TryParse("LOG10", out var column, out var row));
      Assert.Equal(8508, column);
      Assert.Equal(9, row);
    }

    [Theory]
    [InlineData("")]                // nothing at all
    [InlineData("A")]               // a column with no row
    [InlineData("12")]              // a row with no column
    [InlineData("A0")]              // rows are 1-based in the file, so there is no row 0
    [InlineData("A1x")]             // a name that merely starts like a reference
    [InlineData("ABCD1")]           // more letters than any column has
    [InlineData("$A$1")]            // the absolute spelling: a formula's, never an r attribute's
    [InlineData("A1:B2")]           // a range is not a cell
    [InlineData("A:A")]             // ...nor is a whole column
    [InlineData("1:1")]             // ...nor a whole row
    [InlineData("ZZZ1")]            // 18,277 columns past A, and there are 16,384
    [InlineData("A1048577")]        // one row past the last
    [InlineData("A12345678")]       // more digits than any row has
    public void AnythingThatIsNotACellReferenceIsRefused(string reference)
      => Assert.False(A1Reference.TryParse(reference, out _, out _));

    [Fact]
    public void AMalformedReferenceLeavesNothingBehindInTheOutParameters()
    {
      // A caller that ignores the false must not find a half-parsed answer waiting for it, which is
      // how a TryParse quietly becomes a wrong reading.
      Assert.False(A1Reference.TryParse("A1x", out var column, out var row));
      Assert.Equal(0, column);
      Assert.Equal(0, row);
    }

    [Fact]
    public void AWellFormedReferenceOffTheEndOfTheSheetIsRefusedAndLeavesNothingBehind()
    {
      // Once a wart, pinned as found (the range check ran after the outs were assigned, so a
      // refusal left the parse's numbers behind); fixed the same day. The TryParse contract this
      // now states: a refusal leaves nothing behind — a caller that trusts the usual discipline
      // reads zeros, not a column that does not exist.
      Assert.False(A1Reference.TryParse("ZZZ1", out var column, out _));
      Assert.Equal(0, column);

      Assert.False(A1Reference.TryParse("A1048577", out _, out var row));
      Assert.Equal(0, row);
    }
  }
}
