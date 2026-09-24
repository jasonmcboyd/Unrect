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
  /// comments above it — is the artefact and not an implementation detail. It is the type and
  /// nothing else: what binds it is one call the reader already knows, and a line of it in the output
  /// was a line to delete after every paste.
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
    private static ICellSpace Report()
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
          "public sealed record Transaction(string Client, DateTime TransactionDate, string TransactionType, decimal Amount);"),
        Report().ScaffoldRecord("Transaction", at: 7));
    }

    [Fact]
    public void SamplingFurtherDownTheSheetSaysTheSameThing()
    {
      // Non-vacuity for the five-row default: this file's columns are consistent all the way down,
      // so a hundred rows must argue for exactly what five did. A difference here would mean the
      // default had been reading a corner of the file rather than the file.
      Assert.Equal(
        Report().ScaffoldRecord("Transaction", at: 7),
        Report().ScaffoldRecord("Transaction", at: 7, samples: 100));
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
          "public sealed record Transaction(string? Client, string? TransactionDate, string? TransactionType, string? Amount);"),
        Report().ScaffoldRecord("Transaction", at: 7, samples: 0));
    }

    // --- The corners, over one grid --------------------------------------------------------------------

    /// <summary>
    /// A header row carrying every naming corner at once, over two sample rows carrying every typing
    /// corner. Written as one grid rather than nine, because the numbering rules are about columns
    /// SEEN TOGETHER — a duplicate is only a duplicate beside its twin, and a positional fallback
    /// name counts positions among the columns that were named.
    /// </summary>
    private static ICellSpace Corners()
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
          "// \"Amount\" and \"amount\" are one caption to the binder: .Column(r => r.Amount, 0)",
          "// \"Amount\" and \"amount\" are one caption to the binder: .Column(r => r.Amount2, 1)",
          "// \"2024 Total\" does not bind to _2024Total by name: .Column(r => r._2024Total, \"2024 Total\")",
          "// \"Net (USD)\" does not bind to NetUSD by name: .Column(r => r.NetUSD, \"Net (USD)\")",
          "// \"###\" does not bind to Column5 by name: .Column(r => r.Column5, \"###\")",
          "public sealed record Row(",
          "    int Amount,",
          "    int? Amount2,",
          "    int _2024Total,",
          "    decimal NetUSD,",
          "    int Column5,",
          "    string? Notes,",
          "    bool Flag,",
          "    int Count);"),
        Corners().ScaffoldRecord("Row"));
    }

    [Fact]
    public void TwoCaptionsThatWouldBindToOneMemberAreNumberedApart()
    {
      // "Amount" and "amount" are ONE caption to the comparer that binds columns to members, so two
      // members spelled that way would both claim the first column. The second is numbered — and
      // numbered under the comparer rather than under string equality, because the collision worth
      // avoiding is the binder's, not the compiler's.
      Assert.Contains("int Amount,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
      Assert.Contains("int? Amount2,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void ACaptionStartingWithADigitIsPrefixedSoItIsAnIdentifierAtAll()
      // "2024 Total" is a perfectly ordinary caption and _2024Total is what C# will accept. The
      // prefix is not decoration: without it the scaffold emits source that does not compile, which
      // is the one failure a paste-and-run artefact must never have.
      => Assert.Contains("int _2024Total,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);

    [Fact]
    public void ACaptionThatLosesCharactersOnTheWayToAnIdentifierIsStillWritten()
    {
      // "Net (USD)" becomes NetUSD, and NetUSD does NOT bind back — the comparer counts parentheses,
      // so this column needs an explicit caption before it will ever find its data. The scaffold
      // writes it anyway and says so above the type, with the override that fixes it: a name a human
      // can see and correct beats a column silently missing from the record.
      Assert.Contains("decimal NetUSD,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
      Assert.Contains(
        "// \"Net (USD)\" does not bind to NetUSD by name: .Column(r => r.NetUSD, \"Net (USD)\")",
        Corners().ScaffoldRecord("Row"),
        StringComparison.Ordinal);
      Assert.False(CaptionComparer.Default.Equals("NetUSD", "Net (USD)"), "the pin above is worth having only while this is true");
    }

    [Fact]
    public void ACaptionWithNothingUsableInItFallsBackToItsPosition()
    {
      // "###" has no identifier character in it at all, so there is no name to derive. The fallback
      // counts among the columns that were NAMED — "###" is the fifth of them — which is what makes
      // it stable against a blank header cell sitting further left.
      Assert.Contains("int Column5,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AColumnWithABlankSampleIsNullable()
    {
      // Two separate claims about one column, and the reason they are separate: the samples that
      // were there agreed on int, and one of them was missing. A blank is not a kind — it is the
      // absence of one — so it widens the member rather than changing what the column reads as.
      Assert.Contains("int? Amount2,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
      Assert.Contains("string? Notes,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);

      // ...and a column with no blank among its samples is not nullable, which is the half that
      // makes the above mean something.
      Assert.Contains("bool Flag,", Corners().ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AColumnUnderABlankHeaderCellIsNotAColumn()
    {
      // The eighth column of the grid holds "x" and "y" under nothing at all. A member cannot be
      // named after a caption that is not there, and inventing one would invent a binding too — so
      // the column is skipped entirely, and the columns to its right keep their own captions.
      var source = Corners().ScaffoldRecord("Row");

      Assert.DoesNotContain("Column8", source, StringComparison.Ordinal);
      Assert.Contains("int Count);", source, StringComparison.Ordinal);
    }

    // --- The corners that need their own grid ----------------------------------------------------------

    [Fact]
    public void AWholeNumberAndAFractionalOneAreBothNumbersAndWidenToDecimal()
    {
      // The one pair of disagreeing samples that genuinely meets. int and decimal are two readings of
      // one kind, so a column holding both is a number column the sample happened to see twice — not
      // an untypable one. Stated in both orders, because folding pairwise is exactly where an
      // asymmetric rule would hide.
      Assert.Contains("decimal Amount)", Grid(1m, 2.5m).ScaffoldRecord("Row"), StringComparison.Ordinal);
      Assert.Contains("decimal Amount)", Grid(2.5m, 1m).ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void SamplesThatAgreeOnNothingAreTheTypeEveryCellCanSay()
    {
      // A number and a piece of text are not two readings of one kind, and there is nothing that
      // covers both but string. Non-nullable, deliberately: nothing was blank, so nothing argued for
      // a blank — the disagreement is about the KIND, and it does not make the column optional.
      Assert.Contains("string Amount)", Grid(1m, "text").ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AnErrorCellIsAConditionAndNotAValue()
    {
      // An error is what a formula did, not what the column holds — the same cell a nullable member
      // tolerates. So beside a number it widens the member exactly as a blank does, and a column of
      // nothing BUT errors has argued for no type at all and falls back to string?.
      var error = CellValue.OfError(CellError.DivisionByZero);

      Assert.Contains("int? Amount)", Grid(1m, error).ScaffoldRecord("Row"), StringComparison.Ordinal);
      Assert.Contains("string? Amount)", Grid(error, error).ScaffoldRecord("Row"), StringComparison.Ordinal);
    }

    [Fact]
    public void AHeaderWithNothingUnderItTypesNothing()
    {
      // A one-row sheet is the degenerate case of "no samples", reached a different way than
      // samples: 0 — there is nowhere to look rather than no permission to look. The answer must
      // be the same, and the header must still be read.
      var headerOnly = SheetGrid.Of(new object?[,] { { "Date", "Amount" } });

      Assert.Equal(
        Lines(
          "public sealed record Row(string? Date, string? Amount);"),
        headerOnly.ScaffoldRecord("Row"));
    }

    /// <summary>A one-column sheet captioned "Amount", over the sample values given.</summary>
    private static ICellSpace Grid(params object?[] samples)
    {
      var values = new object?[samples.Length + 1, 1];

      values[0, 0] = "Amount";

      for (var index = 0; index < samples.Length; index++)
        values[index + 1, 0] = samples[index];

      return SheetGrid.Of(values);
    }

    // --- Finding the labels ----------------------------------------------------------------------------

    [Fact]
    public void TheLabelsAreFoundUnderATitleBlock()
    {
      // The acceptance case again, with nothing said about where the captions are. Four title rows
      // of a cell or two each sit over a table four wide; the first row that is all text AND at least
      // half as full as the fullest is the caption row, which is what a human would have said.
      Assert.Equal(Report().ScaffoldRecord("Transaction", at: 7), Report().ScaffoldRecord("Transaction"));
    }

    [Fact]
    public void ASheetWithNoRowOfTextSaysSoRatherThanGuessing()
    {
      // Nothing here is a label, and naming members after numbers would be an invention. The refusal
      // names the way out.
      var numbers = SheetGrid.Of(new object?[,] { { 1m, 2m }, { 3m, 4m } });

      var refusal = Assert.Throws<InvalidOperationException>(() => numbers.ScaffoldRecord("Row"));

      Assert.Contains("Pass at:", refusal.Message, StringComparison.Ordinal);

      // ...and being told is enough — but a cell that is not text is still not a label, so the type
      // that comes back is empty rather than named after a number.
      Assert.Equal("public sealed record Row();", numbers.ScaffoldRecord("Row", at: 0));
    }

    // --- Labels down a column: the card ----------------------------------------------------------------

    /// <summary>A title over a card: labels down the first column, one value to the right of each.</summary>
    private static ICellSpace Card()
      => SheetGrid.Of(new object?[,]
      {
        { "Fund Report", null },
        { "Fund Name", "Alpha" },
        { "As Of", new DateTime(2026, 6, 30) },
        { "Net (USD)", 12.5m },
        { "Partners", 12m },
        { "Closed", null },
      });

    [Fact]
    public void ACardIsATableReadTheOtherWay()
    {
      // One record, its labels down a column and its values beside them. Everything is the same
      // argument turned a quarter: the label line is a column, a sample is the cell to the RIGHT, and
      // the title — text, alone in its row, a label like any other to this reading — is a member a
      // human deletes. The note carries no override, because no binder reads a card by caption yet.
      Assert.Equal(
        Lines(
          "// \"Net (USD)\" does not match NetUSD by name",
          "public sealed record Fund(",
          "    string? FundReport,",
          "    string FundName,",
          "    DateTime AsOf,",
          "    decimal NetUSD,",
          "    int Partners,",
          "    string? Closed);"),
        Card().ScaffoldRecord("Fund", LabelsIn.Column));
    }

    [Fact]
    public void ACardsLabelColumnCanBeSaid()
    {
      // The second column is all text too where it holds anything a label could be, and saying so
      // reads it: one label, nothing to its right.
      var sheet = SheetGrid.Of(new object?[,] { { 1m, "Name", "Alpha" }, { 2m, "Code", "A1" } });

      Assert.Equal("public sealed record Fund(string Name, string Code);", sheet.ScaffoldRecord("Fund", LabelsIn.Column));
      Assert.Equal("public sealed record Fund(string Name, string Code);", sheet.ScaffoldRecord("Fund", LabelsIn.Column, at: 1));
      Assert.Throws<OutOfBoundsException>(() => sheet.ScaffoldRecord("Fund", LabelsIn.Column, at: 3));
    }

    // --- The class form --------------------------------------------------------------------------------

    [Fact]
    public void TheSameGuessAsAClass()
    {
      // Same members, same types, the other shape the binder fills: a parameterless constructor and
      // init-only properties. A non-nullable string is initialised, so the paste is warning-free with
      // nullable on.
      Assert.Equal(
        Lines(
          "public sealed class Transaction",
          "{",
          "    public string Client { get; init; } = \"\";",
          "    public DateTime TransactionDate { get; init; }",
          "    public string TransactionType { get; init; } = \"\";",
          "    public decimal Amount { get; init; }",
          "}"),
        Report().ScaffoldClass("Transaction"));
    }

    [Fact]
    public void AClassCarriesEachNoteOnThePropertyItConcerns()
    {
      // A record's parameters are one list, so its notes sit above it; a class has a line per member,
      // and a note beside the member it is about is the one a reader cannot miss.
      var source = Corners().ScaffoldClass("Row");

      Assert.Contains(
        Lines(
          "    // \"Net (USD)\" does not bind to NetUSD by name: .Column(r => r.NetUSD, \"Net (USD)\")",
          "    public decimal NetUSD { get; init; }"),
        source,
        StringComparison.Ordinal);
      Assert.Contains("    public string? Notes { get; init; }" + Environment.NewLine, source, StringComparison.Ordinal);
      Assert.Contains(
        Lines("    public int Count { get; init; }", "}"),
        source,
        StringComparison.Ordinal);
    }

    [Fact]
    public void BothFormsRefuseTheSameThings()
    {
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => sheet.ScaffoldClass(" ")).ParamName);
      Assert.Equal("at", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.ScaffoldClass("Row", at: -1)).ParamName);
      Assert.Throws<OutOfBoundsException>(() => sheet.ScaffoldClass("Row", at: 2));
    }

    // --- The name, and the refusals --------------------------------------------------------------------

    [Fact]
    public void WhatComesBackIsTheTypeAndNothingElse()
    {
      // The output is pasted where types go, and a statement does not go there. A caption that binds
      // by name needs no comment either, so the common case is exactly one declaration.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("public sealed record Transaction(int Amount);", sheet.ScaffoldRecord("Transaction"));
    }

    [Fact]
    public void AHeaderRowPastTheEndOfTheSheetIsAnOverrun()
    {
      // Asking about a row the sheet does not have is a statement about the DATA — the file is
      // shorter than the script thought — so it is the bounds condition, not the fault a negative row
      // gets. The two live one line apart in the guard and they are not the same refusal.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { "Total" } });

      Assert.Throws<OutOfBoundsException>(() => sheet.ScaffoldRecord("Row", at: 2));

      // ...and the last row the sheet has is a legitimate header with nothing under it.
      Assert.Contains("string? Total)", sheet.ScaffoldRecord("Row", at: 1), StringComparison.Ordinal);
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
      Assert.Throws<OutOfBoundsException>(() => noColumns.ScaffoldRecord("Row", at: 9));

      // ...and the boundary itself, which is where an off-by-one would sit: the row AT the height is
      // one past the last, and the row before it is the last real one.
      Assert.Throws<OutOfBoundsException>(() => noColumns.ScaffoldRecord("Row", at: 2));
      Assert.Equal(
        "public sealed record Row();",
        noColumns.ScaffoldRecord("Row", at: 1));
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
          "public sealed record Row(string? Date, string? Amount);"),
        headerOnly.ScaffoldRecord("Row", at: 0));
      Assert.Throws<OutOfBoundsException>(() => headerOnly.ScaffoldRecord("Row", at: 1));
    }

    [Fact]
    public void ANegativeRowOrSampleCountIsAFault()
    {
      // Neither is a smaller request — there is no row before the first and no negative number of
      // samples — so nothing about the sheet could have produced either, and both are argument bugs.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("at", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.ScaffoldRecord("Row", at: -1)).ParamName);
      Assert.Equal("samples", Assert.Throws<ArgumentOutOfRangeException>(() => sheet.ScaffoldRecord("Row", samples: -1)).ParamName);
    }

    [Fact]
    public void ARecordNeedsAName()
    {
      // No name means no source: there is nothing to emit and nothing to guess. Null and blank are
      // told apart because they are different mistakes — one is a missing argument, the other an
      // empty one.
      var sheet = SheetGrid.Of(new object?[,] { { "Amount" }, { 1m } });

      Assert.Equal("typeName", Assert.Throws<ArgumentNullException>(() => sheet.ScaffoldRecord(null!)).ParamName);
      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => sheet.ScaffoldRecord("")).ParamName);
      Assert.Equal("typeName", Assert.Throws<ArgumentException>(() => sheet.ScaffoldRecord("   ")).ParamName);
    }
  }
}
