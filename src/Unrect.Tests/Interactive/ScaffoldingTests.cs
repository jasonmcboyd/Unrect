using System;
using System.IO;

using Unrect.Core;
using Unrect.Interactive;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// Reading a header row and a few samples and handing back C# source to paste.
  /// <para>
  /// What is being tested is a GUESS, and the tests are written knowing it. Every member type here
  /// is what a handful of rows argued for, not what the file contains — so the statements below are
  /// about the argument, not about the column: "all the samples were whole numbers, so it wrote
  /// <c>int</c>", and separately "one of them was blank, so it wrote <c>?</c>". That is the only kind
  /// of claim a sample can support, and a test that pretended otherwise would be pinning a lie.
  /// </para>
  /// <para>
  /// The output is source text, which is why it is pinned as text. It is meant to be read and edited
  /// by a human, so the exact spelling — the order of the members, the <c>sealed record</c>, the
  /// binding line beneath it — is the artefact and not an implementation detail.
  /// </para>
  /// </summary>
  public class ScaffoldingTests
  {
    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    // --- A real workbook -------------------------------------------------------------------------------

    /// <summary>
    /// simple-report.xlsx: four title rows, a blank, then the captions on the sheet's row 8 — which
    /// is the 0-based row 7 — over eight transactions.
    /// </summary>
    private static ISheetCells Report()
      => SpreadsheetSpace.Create(Path.Combine(AppContext.BaseDirectory, "TestData", "simple-report.xlsx"), "Report");

    [Fact]
    public void AHeaderRowScaffoldsTheRecordTheScriptWroteByHand()
    {
      // The acceptance case, and the reason the whole thing exists: this is linqpad/simple-report.linq's
      // own record, arrived at from the file rather than from a human reading it — minus the two
      // demonstration caption overrides, because the members here are named after the FULL captions
      // and so need no override at all. Each of the four types is a different argument: text
      // throughout, a temporal column, text again, and numbers that do not all fit an int.
      Assert.Equal(
        Lines(
          "public sealed record Transaction(string Client, DateTime TransactionDate, string TransactionType, decimal Amount);",
          "var transaction = Table<Transaction>();"),
        Report().Scaffold("Transaction", headerRow: 7));
    }

    [Fact]
    public void SamplingFurtherDownTheSheetSaysTheSameThing()
    {
      // Non-vacuity for the five-row default: this file's columns are consistent all the way down,
      // so a hundred rows must argue for exactly what five did. A difference here would mean the
      // default had been reading a corner of the file rather than the file.
      Assert.Equal(
        Report().Scaffold("Transaction", headerRow: 7),
        Report().Scaffold("Transaction", headerRow: 7, sampleRows: 100));
    }

    [Fact]
    public void SamplingNothingLeavesEveryColumnUntyped()
    {
      // The honest floor. With no samples there is no argument for any type, and a column with
      // nothing to go on is `string?` — the one type every cell can satisfy and every cell can be
      // blank in. The captions are still read, because they come from the header and not from the
      // samples.
      Assert.Equal(
        Lines(
          "public sealed record Transaction(string? Client, string? TransactionDate, string? TransactionType, string? Amount);",
          "var transaction = Table<Transaction>();"),
        Report().Scaffold("Transaction", headerRow: 7, sampleRows: 0));
    }

    // --- The corners, over one grid --------------------------------------------------------------------

    /// <summary>
    /// A header row carrying every naming corner at once, over two sample rows carrying every typing
    /// corner. Written as one grid rather than nine, because the numbering rules are about columns
    /// SEEN TOGETHER — a duplicate is only a duplicate beside its twin, and a positional fallback
    /// name counts positions among the columns that were named.
    /// </summary>
    private static ISheetCells Corners()
      => SheetGrid.Of(new object?[,]
      {
        { "Amount", "amount", "2024 Total", "Net (USD)", "###", "Notes", "Flag", null, "Count" },
        { 1m, 2m, 3m, 4.5m, 5m, "a", true, "x", 10m },
        { 2m, null, 3m, 5.5m, 6m, null, false, "y", 20m },
      });

    [Fact]
    public void EveryNamingAndTypingCornerAtOnce()
    {
      // The whole artefact, so the ORDER of the members and the shape of the two lines are pinned
      // somewhere. Each corner is taken apart on its own below; this is the one that would notice a
      // column being dropped, reordered, or named after the wrong caption.
      Assert.Equal(
        Lines(
          "public sealed record Row(int Amount, int? Amount2, int _2024Total, decimal NetUSD, "
            + "int Column5, string? Notes, bool Flag, int Count);",
          "var row = Table<Row>();"),
        Corners().Scaffold("Row"));
    }

    [Fact]
    public void TwoCaptionsThatWouldBindToOneMemberAreNumberedApart()
    {
      // "Amount" and "amount" are ONE caption to the comparer that binds columns to members, so two
      // members spelled that way would both claim the first column. The second is numbered — and
      // numbered under the comparer rather than under string equality, because the collision worth
      // avoiding is the binder's, not the compiler's.
      Assert.Contains("int Amount,", Corners().Scaffold("Row"), StringComparison.Ordinal);
      Assert.Contains("int? Amount2,", Corners().Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void ACaptionStartingWithADigitIsPrefixedSoItIsAnIdentifierAtAll()
      // "2024 Total" is a perfectly ordinary caption and _2024Total is what C# will accept. The
      // prefix is not decoration: without it the scaffold emits source that does not compile, which
      // is the one failure a paste-and-run artefact must never have.
      => Assert.Contains("int _2024Total,", Corners().Scaffold("Row"), StringComparison.Ordinal);

    [Fact]
    public void ACaptionThatLosesCharactersOnTheWayToAnIdentifierIsStillWritten()
    {
      // "Net (USD)" becomes NetUSD, and NetUSD does NOT bind back — the comparer counts parentheses,
      // so this column needs an explicit caption before it will ever find its data. The scaffold
      // writes it anyway and says so in its own documentation: a name a human can see and correct
      // beats a column silently missing from the record.
      Assert.Contains("decimal NetUSD,", Corners().Scaffold("Row"), StringComparison.Ordinal);
      Assert.False(CaptionComparer.Default.Equals("NetUSD", "Net (USD)"), "the pin above is worth having only while this is true");
    }

    [Fact]
    public void ACaptionWithNothingUsableInItFallsBackToItsPosition()
    {
      // "###" has no identifier character in it at all, so there is no name to derive. The fallback
      // counts among the columns that were NAMED — "###" is the fifth of them — which is what makes
      // it stable against a blank header cell sitting further left.
      Assert.Contains("int Column5,", Corners().Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AColumnWithABlankSampleIsNullable()
    {
      // Two separate claims about one column, and the reason they are separate: the samples that
      // were there agreed on int, and one of them was missing. A blank is not a kind — it is the
      // absence of one — so it widens the member rather than changing what the column reads as.
      Assert.Contains("int? Amount2,", Corners().Scaffold("Row"), StringComparison.Ordinal);
      Assert.Contains("string? Notes,", Corners().Scaffold("Row"), StringComparison.Ordinal);

      // ...and a column with no blank among its samples is not nullable, which is the half that
      // makes the above mean something.
      Assert.Contains("bool Flag,", Corners().Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AColumnUnderABlankHeaderCellIsNotAColumn()
    {
      // The eighth column of the grid holds "x" and "y" under nothing at all. A member cannot be
      // named after a caption that is not there, and inventing one would invent a binding too — so
      // the column is skipped entirely, and the columns to its right keep their own captions.
      var source = Corners().Scaffold("Row");

      Assert.DoesNotContain("Column8", source, StringComparison.Ordinal);
      Assert.Contains("int Count)", source, StringComparison.Ordinal);
    }

    // --- The corners that need their own grid ----------------------------------------------------------

    [Fact]
    public void AWholeNumberAndAFractionalOneAreBothNumbersAndWidenToDecimal()
    {
      // The one pair of disagreeing samples that genuinely meets. int and decimal are two readings of
      // one kind, so a column holding both is a number column the sample happened to see twice — not
      // an untypable one. Stated in both orders, because folding pairwise is exactly where an
      // asymmetric rule would hide.
      Assert.Contains("decimal Amount)", Grid(1m, 2.5m).Scaffold("Row"), StringComparison.Ordinal);
      Assert.Contains("decimal Amount)", Grid(2.5m, 1m).Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void SamplesThatAgreeOnNothingAreTheTypeEveryCellCanSay()
    {
      // A number and a piece of text are not two readings of one kind, and there is nothing that
      // covers both but string. Non-nullable, deliberately: nothing was blank, so nothing argued for
      // a blank — the disagreement is about the KIND, and it does not make the column optional.
      Assert.Contains("string Amount)", Grid(1m, "text").Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AnErrorCellIsAConditionAndNotAValue()
    {
      // An error is what a formula did, not what the column holds — the same cell a nullable member
      // tolerates. So beside a number it widens the member exactly as a blank does, and a column of
      // nothing BUT errors has argued for no type at all and falls back to string?.
      var error = Cell.OfError(CellError.DivisionByZero);

      Assert.Contains("int? Amount)", Grid(1m, error).Scaffold("Row"), StringComparison.Ordinal);
      Assert.Contains("string? Amount)", Grid(error, error).Scaffold("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AHeaderWithNothingUnderItTypesNothing()
    {
      // A one-row sheet is the degenerate case of "no samples", reached a different way than
      // sampleRows: 0 — there is nowhere to look rather than no permission to look. The answer must
      // be the same, and the header must still be read.
      var headerOnly = SheetGrid.Of(new object?[,] { { "Date", "Amount" } });

      Assert.Equal(
        Lines(
          "public sealed record Row(string? Date, string? Amount);",
          "var row = Table<Row>();"),
        headerOnly.Scaffold("Row"));
    }

    /// <summary>A one-column sheet captioned "Amount", over the sample values given.</summary>
    private static ISheetCells Grid(params object?[] samples)
    {
      var values = new object?[samples.Length + 1, 1];

      values[0, 0] = "Amount";

      for (var index = 0; index < samples.Length; index++)
        values[index + 1, 0] = samples[index];

      return SheetGrid.Of(values);
    }

    // --- The name, and the refusals --------------------------------------------------------------------

    [Fact]
    public void TheBindingLineNamesItsVariableAfterTheType()
    {
      // Paste-ready means the second line has to be a declaration a human would have written, and a
      // C# variable is camel-cased. Only the first character moves, so an acronym or an already-cased
      // rest of the name survives.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" } });

      Assert.EndsWith("var transaction = Table<Transaction>();", sheet.Scaffold("Transaction"), StringComparison.Ordinal);
      Assert.EndsWith("var t = Table<T>();", sheet.Scaffold("T"), StringComparison.Ordinal);
    }

    [Fact]
    public void AHeaderRowPastTheEndOfTheSheetIsAnOverrun()
    {
      // Asking about a row the sheet does not have is a statement about the DATA — the file is
      // shorter than the script thought — so it is the bounds condition, not the fault a negative row
      // gets. The two live one line apart in the guard and they are not the same refusal.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { "Total" } });

      Assert.Throws<OutOfBoundsException>(() => sheet.Scaffold("Row", headerRow: 2));

      // ...and the last row the sheet has is a legitimate header with nothing under it.
      Assert.Contains("string? Total)", sheet.Scaffold("Row", headerRow: 1), StringComparison.Ordinal);
    }

    [Fact]
    public void ASheetWithNoColumnsStillKnowsHowTallItIs()
    {
      // Once a wart, pinned as found and fixed the same day: the overrun used to be discovered by
      // the first cell READ, and a sheet with no columns reads no cell at all — so a header row that
      // did not exist scaffolded `public sealed record Row();` from it, source that compiles and
      // binds to nothing. The height is a property of the sheet, not something only a cell can
      // reveal, so it is checked before anything is read.
      var noColumns = SheetGrid.Of(new object?[2, 0]);

      Assert.Equal(0, noColumns.Area.Width);
      Assert.Equal(2, noColumns.Area.Height);
      Assert.Throws<OutOfBoundsException>(() => noColumns.Scaffold("Row", headerRow: 9));

      // ...and the boundary itself, which is where an off-by-one would sit: the row AT the height is
      // one past the last, and the row before it is the last real one.
      Assert.Throws<OutOfBoundsException>(() => noColumns.Scaffold("Row", headerRow: 2));
      Assert.Equal(
        Lines("public sealed record Row();", "var row = Table<Row>();"),
        noColumns.Scaffold("Row", headerRow: 1));
    }

    [Fact]
    public void TheLastRowOfASheetIsAHeaderAndTheRowAfterItIsNot()
    {
      // The same boundary where there is something to read, so the two halves are not both stated
      // over the degenerate sheet. A header-only sheet is one row tall: row 0 is its header and has
      // no samples under it, and row 1 is off the end.
      var headerOnly = SheetGrid.Of(new object?[,] { { "Date", "Amount" } });

      Assert.Equal(1, headerOnly.Area.Height);
      Assert.Equal(
        Lines(
          "public sealed record Row(string? Date, string? Amount);",
          "var row = Table<Row>();"),
        headerOnly.Scaffold("Row", headerRow: 0));
      Assert.Throws<OutOfBoundsException>(() => headerOnly.Scaffold("Row", headerRow: 1));
    }

    [Fact]
    public void ANegativeRowOrSampleCountIsAFault()
    {
      // Neither is a smaller request — there is no row before the first and no negative number of
      // samples — so nothing about the sheet could have produced either, and both are argument bugs.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("headerRow", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Scaffold("Row", headerRow: -1)).ParamName);
      Assert.Equal("sampleRows", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Scaffold("Row", sampleRows: -1)).ParamName);
    }

    [Fact]
    public void ARecordNeedsAName()
    {
      // No name means no source: there is nothing to emit and nothing to guess. Null and blank are
      // told apart because they are different mistakes — one is a missing argument, the other an
      // empty one.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("typeName", Assert.Throws<ArgumentNullException>(() => sheet.Scaffold(null!)).ParamName);
      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => sheet.Scaffold("")).ParamName);
      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => sheet.Scaffold("   ")).ParamName);
    }
  }
}
