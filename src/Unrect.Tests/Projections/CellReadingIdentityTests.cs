using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The one law <c>CellReading</c> exists to guarantee: a typed leaf and a bound table column,
  /// looking at the SAME cell, describe it in the same words. Both readers route every kind failure
  /// through <c>CellReading.WrongKind</c> and every conversion failure through the same
  /// <c>Read*</c> pair, so the claim in the type's own doc comment — "a <c>Decimal()</c> leaf and a
  /// <c>decimal</c> column cannot describe the same cell differently" — is stated here rather than
  /// left to the shared call site.
  /// <para>
  /// <strong>What the law covers, and how phase 6 sharpened it.</strong> The <em>cell-describing
  /// sentence</em>: the kind sentence (<c>expected Number at B2, found Text</c>) and the conversion
  /// sentence (<c>the Number at B2 (1.5) is not a whole number</c>), including the A1 address inside
  /// them. The sentence is now <b>byte-identical on both readers, with nothing stripped</b> — where
  /// the binder used to prefix it with <c>column '{caption}': </c>, the caption is a PATH SEGMENT
  /// now (D-A): the column is a named unit the declaration produced, so it is named where every
  /// other declared thing is named, and the sentence underneath it is the backend's alone. One
  /// reading, one sentence, whether it was reached through a record's member or through a point.
  /// </para>
  /// <para>
  /// What still differs is the wrapper, and it differs in the two places a reader looks for "which
  /// declaration was this": the SUBJECT (<c>Decimal</c> versus <c>column 'Amount'</c>) and the PATH
  /// (<c>Decimal</c> versus <c>Table&lt;Money&gt;[0] -&gt; column 'Amount'</c>). Those are two
  /// different declarations and should read as two different declarations.
  /// <see cref="TheWrapperIsWhereTheTwoReadersDiffer"/> is the matching negative pin, spelling out
  /// the specific difference rather than "these differ".
  /// </para>
  /// <para>
  /// The <c>Observations</c> harness does not fit here: the two readers' values differ by
  /// construction (one cell versus a list of records), and what is compared is one sentence at a
  /// time. Plain asserts.
  /// </para>
  /// </summary>
  public class CellReadingIdentityTests
  {
    // --- The types the columns are bound through -----------------------------------------------------
    //
    // One record per cell kind, each with a leading Client column so the offending cell lands at B2
    // rather than at A1 — an address that is wrong in the same way on both readers would otherwise
    // pass unnoticed.

    public record Money(string Client, decimal Amount);

    public record Counted(string Client, int Count);

    public record Noted(string Client, string Note);

    public record Flagged(string Client, bool Flag);

    public record Dated(string Client, DateTime Date);

    public record Tolerant(string Client, decimal? Amount);

    public record Annotated(string Client, string? Note);

    /// <summary>
    /// Reads one offending cell twice — once by <paramref name="leaf"/> placed on B2, once by
    /// <paramref name="table"/> binding the column captioned <paramref name="caption"/> — and
    /// asserts the binder said exactly what the leaf said, character for character. Hands back the
    /// shared sentence so the caller can pin the words themselves.
    /// <para>
    /// Nothing is stripped. The assertion used to allow the binder a <c>column '{caption}': </c>
    /// prefix; the caption is a path segment now, so the sentences are simply equal, and the caption
    /// is checked where it went — on the subject.
    /// </para>
    /// </summary>
    private static string SameSentence<TValue, TRow>(
      string caption,
      object? offending,
      IProjectionDefinition<ISheetCells, TValue> leaf,
      IProjectionDefinition<ISheetCells, IReadOnlyList<TRow>> table)
    {
      var space = Mixed(new object?[,]
      {
        { "Client", caption },
        { "Acme", offending },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(leaf).Map(space));
      var byColumn = Assert.Throws<ProjectionException>(() => table.Map(space));

      Assert.Equal(Problem(byLeaf), Problem(byColumn));
      Assert.Equal($"column '{caption}'", byColumn.Subject);

      return Problem(byLeaf);
    }

    // --- Kind failures ----------------------------------------------------------------------------------

    [Fact]
    public void ADecimalLeafAndADecimalColumnDescribeATextCellIdentically()
    {
      Assert.Equal(
        "expected Number at B2, found Text",
        SameSentence("Amount", "x", Decimal(), Table<Money>()));
    }

    [Fact]
    public void ATextLeafAndAStringColumnDescribeANumberCellIdentically()
    {
      Assert.Equal(
        "expected Text at B2, found Number",
        SameSentence("Note", 5m, Text(), Table<Noted>()));
    }

    [Fact]
    public void ABooleanLeafAndABoolColumnDescribeANumberCellIdentically()
    {
      Assert.Equal(
        "expected Boolean at B2, found Number",
        SameSentence("Flag", 1m, Boolean(), Table<Flagged>()));
    }

    [Fact]
    public void ADateLeafAndADateTimeColumnDescribeATextCellIdentically()
    {
      // Temporal, not DateTime: the sentence speaks the document's six kinds, and it does so
      // identically whether the declaration named a leaf or a member type.
      Assert.Equal(
        "expected Temporal at B2, found Text",
        SameSentence("Date", "x", Date(), Table<Dated>()));
    }

    [Fact]
    public void AnErrorCellIsNamedAsTheErrorItIsByBothReaders()
    {
      // The one kind with no CLR literal, and the one whose rendering comes from Core rather than
      // from either reader — so this is the case where a duplicated message would show first.
      Assert.Equal(
        "expected Number at B2, found Error(#DIV/0!)",
        SameSentence("Amount", Cell.OfError(CellError.DivisionByZero), Decimal(), Table<Money>()));
    }

    [Fact]
    public void ABlankIsRefusedInTheSameWordsByAPlainLeafAndANonNullableMember()
    {
      Assert.Equal(
        "expected Number at B2, found Blank",
        SameSentence("Amount", null, Decimal(), Table<Money>()));
    }

    // --- Conversion failures ------------------------------------------------------------------------------

    [Fact]
    public void ANonWholeNumberIsRefusedInTheSameWordsByAnIntegerLeafAndAnIntColumn()
    {
      Assert.Equal(
        "the Number at B2 (1.5) is not a whole number",
        SameSentence("Count", 1.5, Integer(), Table<Counted>()));

      // The scale is the cell's, not the reader's: a decimal 1.50 prints as 1.50 through either
      // door, where rendering it through a double would print 1.5 and quietly disagree with the
      // sheet — on whichever reader drifted.
      Assert.Equal(
        "the Number at B2 (1.50) is not a whole number",
        SameSentence("Count", 1.50m, Integer(), Table<Counted>()));
    }

    [Fact]
    public void TheOtherTwoConversionsAreRefusedInTheSameWordsByBothReaders()
    {
      Assert.Equal(
        "the Number at B2 (5000000000) is outside the range of a 32-bit integer",
        SameSentence("Count", 5e9, Integer(), Table<Counted>()));

      Assert.Equal(
        "the Number at B2 (1E+30) is not representable as a decimal",
        SameSentence("Amount", 1e30, Decimal(), Table<Money>()));
    }

    // --- What the law does NOT cover -------------------------------------------------------------------------

    [Fact]
    public void TheWrapperIsWhereTheTwoReadersDiffer()
    {
      // The negative pin, spelled out: one sentence, two declarations, and the declaration is named
      // in the subject and the path rather than folded into the sentence.
      var space = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(Decimal()).Map(space));
      var byColumn = Assert.Throws<ProjectionException>(() => Table<Money>().Map(space));

      Assert.Equal("Decimal", byLeaf.Subject);
      Assert.Equal("Decimal", byLeaf.Path);
      Assert.Equal("expected Number at B2, found Text", Problem(byLeaf));

      // The column is a unit the table declared, so it is a path segment under the table's own —
      // and the record's index is on the table's segment, where a repeat's index has always been.
      // Until phase 6 both of these read "Table<Money>" and the caption lived inside the sentence.
      Assert.Equal("column 'Amount'", byColumn.Subject);
      Assert.Equal("Table<Money>[0] -> column 'Amount'", byColumn.Path);
      Assert.Equal("expected Number at B2, found Text", Problem(byColumn));

      // OrBlank moves the wrapper too — it renames the leaf to "Decimal?" — and leaves the sentence
      // about the cell exactly where it was.
      var tolerant = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(Decimal().OrBlank()).Map(space));

      Assert.Equal("Decimal?", tolerant.Subject);
      Assert.Equal(Problem(byLeaf), Problem(tolerant));
    }

    [Fact]
    public void TheColumnIsAPathSegmentAndTheSentenceUnderItIsUnprefixed()
    {
      // The whole message of a bind failure, assembled as a reader sees it. Where the caption is
      // said is the load-bearing part, and phase 6 moved it: the column is a declared unit, so
      // `column 'Amount'` is the SUBJECT and the last segment of the PATH, and what follows the
      // subject is the backend's sentence with nothing in front of it. Every other pin in this file
      // asserts one sentence, which leaves the relationship between subject, path and sentence
      // unstated — so it is stated here once, in full, and moving the caption anywhere else is a
      // visible diff on this test rather than a silent change of shape.
      //
      // Until phase 6 this read `Table<Money>: column 'Amount': …` in Table<Money> at A1 with 2x2
      // available: the caption was a prefix inside the problem, and the failure was located at the
      // table's own corner because the table was the innermost thing that had a name.
      var space = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var failure = Assert.Throws<ProjectionException>(() => Table<Money>().Map(space));

      Assert.Equal(
        "column 'Amount': expected Number at B2, found Text" + Environment.NewLine
        + "  in Table<Money>[0] -> column 'Amount'" + Environment.NewLine
        + "  at row 2, column 2 (B2); 1x1 available",
        failure.Message);

      Assert.Equal("Table<Money>[0] -> column 'Amount'", failure.Path);

      // And the scaffolding the column is read through is visible only in the full path: the
      // record's overlay is how a row places its columns, not something a reader declared, so it is
      // folded out of the path proper and kept here where a maintainer can still see it.
      // The full path is the drill-through: the bind rung is a header, then one band per row, the
      // same composition the row-slot rung is made of, with the record's overlay inside the band.
      Assert.Equal("Table<Money> -> UnderColumnLabels -> VerticalBands#2[0] -> Overlay -> column 'Amount'", failure.FullPath);
    }

    // --- Blank tolerance agrees too ------------------------------------------------------------------------------

    [Fact]
    public void ANullableMemberAndAnOrBlankLeafBothTakeABlank()
    {
      var space = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", null },
      });

      var byLeaf = Right(1).Down(1).Of(Decimal().OrBlank()).MapWithDiagnostics(space);
      var byColumn = Table<Tolerant>().Map(space);

      Assert.Null(byLeaf.Value);
      Assert.Null(byColumn.Single().Amount);

      // Quietly on both sides: a declared blank is the answer, not something to report. Optional()
      // is the operator that absorbs and says so.
      Assert.DoesNotContain(byLeaf.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AnAnnotatedStringMemberAndAnOrBlankTextLeafBothTakeABlank()
    {
      // The annotation carries the same tolerance Nullable<T> carries in the CLR, and OrBlank
      // declares it on the leaf; all three spellings mean one thing.
      var space = Mixed(new object?[,]
      {
        { "Client", "Note" },
        { "Acme", null },
      });

      Assert.Null(Right(1).Down(1).Of(Text().OrBlank()).Map(space));
      Assert.Null(Table<Annotated>().Map(space).Single().Note);
    }

    [Fact]
    public void BlankToleranceIsNotKindToleranceOnEitherReader()
    {
      // The other half, and the one that would rot silently: tolerating a blank must not tolerate a
      // Text cell in a Number column, and the refusal is still the same sentence on both readers.
      Assert.Equal(
        "expected Number at B2, found Text",
        SameSentence("Amount", "x", Decimal().OrBlank(), Table<Tolerant>()));

      Assert.Equal(
        "expected Text at B2, found Number",
        SameSentence("Note", 5m, Text().OrBlank(), Table<Annotated>()));
    }
  }
}
