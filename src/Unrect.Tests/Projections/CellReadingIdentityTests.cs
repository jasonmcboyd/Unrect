using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
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
  /// <strong>What the law covers.</strong> The <em>cell-describing sentence</em> only: the kind
  /// sentence (<c>expected Number at B2, found Text</c>) and the conversion sentence (<c>the Number
  /// at B2 (1.5) is not a whole number</c>), including the A1 address inside them. It does NOT
  /// cover the wrapper: the leaf's problem IS the sentence, while the binder prefixes it with
  /// <c>column '{caption}': </c>, and the two failures carry legitimately different subjects and
  /// paths (<c>Decimal</c> at <c>Decimal</c> versus <c>Table&lt;Money&gt;</c> at
  /// <c>Table&lt;Money&gt;</c>) because they are different declarations. Every test below therefore strips exactly that prefix and asserts the
  /// remainder is character-identical; <see cref="TheWrapperIsWhereTheTwoReadersDiffer"/> is the
  /// matching negative pin, spelling out the specific difference rather than "these differ".
  /// </para>
  /// <para>
  /// The <c>Observations</c> harness does not fit here. Its L1 facet compares the whole problem
  /// text, and the binder's prefix is a deliberate part of that text, so L1 would be false for a
  /// law that is true; and the two readers' values differ by construction (one cell versus a list
  /// of records). Plain asserts, one sentence at a time.
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
    /// asserts the binder said exactly what the leaf said, behind its column prefix. Hands back the
    /// shared sentence so the caller can pin the words themselves.
    /// </summary>
    private static string SameSentence<TValue, TRow>(
      string caption,
      object? offending,
      IProjection<TValue> leaf,
      IProjection<IReadOnlyList<TRow>> table)
    {
      var space = Mixed(new object?[,]
      {
        { "Client", caption },
        { "Acme", offending },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => leaf.Right(1).Down(1).Map(space));
      var byColumn = Assert.Throws<ProjectionException>(() => table.Map(space));

      Assert.Equal($"column '{caption}': {Problem(byLeaf)}", Problem(byColumn));

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
        SameSentence("Amount", CellValue.OfError(CellError.DivisionByZero), Decimal(), Table<Money>()));
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
      // The negative pin, spelled out: same sentence, different subject, different path, and one
      // prefix that the binder adds because a table failure has to say which column it was reading.
      var space = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Decimal().Right(1).Down(1).Map(space));
      var byColumn = Assert.Throws<ProjectionException>(() => Table<Money>().Map(space));

      Assert.Equal("Decimal", byLeaf.Subject);
      Assert.Equal("Decimal", byLeaf.Path);
      Assert.Equal("expected Number at B2, found Text", Problem(byLeaf));

      Assert.Equal("Table<Money>", byColumn.Subject);
      Assert.Equal("Table<Money>", byColumn.Path);
      Assert.Equal("column 'Amount': expected Number at B2, found Text", Problem(byColumn));

      // OrBlank moves the wrapper too — it renames the leaf to "Decimal?" — and leaves the sentence
      // about the cell exactly where it was.
      var tolerant = Assert.Throws<ProjectionException>(() => Decimal().OrBlank().Right(1).Down(1).Map(space));

      Assert.Equal("Decimal?", tolerant.Subject);
      Assert.Equal(Problem(byLeaf), Problem(tolerant));
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

      var byLeaf = Decimal().OrBlank().Right(1).Down(1).MapWithDiagnostics(space);
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

      Assert.Null(Text().OrBlank().Right(1).Down(1).Map(space));
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
