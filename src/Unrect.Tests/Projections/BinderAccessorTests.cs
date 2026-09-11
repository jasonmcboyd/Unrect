using System;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The compute-legal binder's coordinate-aware accessors: <c>row.Decimal("Amount")</c> and
  /// <c>row.Text(8)</c> on a <see cref="TableRow"/>, <c>strip.Decimal(i)</c> on a <see
  /// cref="CellStrip"/>. Each reads the cell through the one canonical accessor for its kind, so on a
  /// bad cell it must describe the failure with the very same words the matching leaf and the
  /// <c>Table&lt;T&gt;</c> binder use — the leaf firewall holding across the accessor layer, which is
  /// the reason the binder is faithful.
  /// <para>
  /// <strong>The two spellings the accessors preserve.</strong> A CAPTION read names the column the
  /// way the binder does (<c>column 'Amount': </c> in front of the shared sentence); an INDEX read
  /// has no caption to name and speaks the bare leaf sentence, the A1 alone pinning the column. Every
  /// identity pin below asserts the exact string so a drift on either reader is caught.
  /// </para>
  /// <para>
  /// The accessors are exercised by obtaining the view through a projection that hands the view back
  /// (<c>Table((TableRow r) =&gt; r)</c>, <c>Row(w, r =&gt; r)</c>, <c>Range(b =&gt; b)</c>) and then
  /// calling the accessor directly — the way a user's inline computation calls it — so the exception
  /// the accessor itself raises is observed unwrapped: a <see cref="ProjectionException"/> for a
  /// TableRow lookup, an <see cref="ArgumentOutOfRangeException"/> for a CellStrip index.
  /// </para>
  /// </summary>
  public class BinderAccessorTests
  {
    // --- The records the binder side of the identity pins bind through -------------------------------
    //
    // Each has a leading Client column so the offending cell lands at B2, not A1 — an address wrong in
    // the same way on both readers would otherwise pass unnoticed.

    public record Money(string Client, decimal Amount);

    public record Counted(string Client, int Count);

    // --- Helpers: obtain a single view over a cell at B2 --------------------------------------------

    /// <summary>
    /// A one-record table — <c>{ "Client", caption }</c> over <c>{ "Acme", value }</c> — with its
    /// value cell at B2, handed back as a <see cref="TableRow"/> without being read, so an accessor
    /// call on it is observed directly.
    /// </summary>
    private static TableRow RowOf(string caption, object? value)
      => Assert.Single(Table((TableRow r) => r).Map(Mixed(new object?[,]
      {
        { "Client", caption },
        { "Acme", value },
      })));

    /// <summary>A row strip A2:B2 with <paramref name="value"/> at index 1 (B2), handed back unread.</summary>
    private static CellStrip StripOf(object? value)
      => Down(1).Of(Row(2, r => r)).Map(Mixed(new object?[,]
      {
        { "top", "top" },
        { "left", value },
      }));

    // --- 1a. Caption reads return the typed value ---------------------------------------------------

    [Fact]
    public void CaptionReadsReturnTheTypedValue()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hi", RowOf("V", "hi").Text("V"));
      Assert.Equal(1.5m, RowOf("V", 1.5m).Decimal("V"));
      Assert.Equal(42, RowOf("V", 42).Integer("V"));
      Assert.Equal(0.25, RowOf("V", 0.25).Double("V"));
      Assert.Equal(moment, RowOf("V", moment).Date("V"));
      Assert.True(RowOf("V", true).Boolean("V"));
    }

    [Fact]
    public void ACaptionReadResolvesTheColumnByNameNotByPosition()
    {
      // The point of caption keying: a reordered export needs no change. "Client" is column B here,
      // and the read finds it there rather than at index 0.
      var row = Assert.Single(Table((TableRow r) => r).Map(Mixed(new object?[,]
      {
        { "Amount", "Client" },
        { 10m, "Acme" },
      })));

      Assert.Equal("Acme", row.Text("Client"));
      Assert.Equal(10m, row.Decimal("Amount"));
    }

    // --- 1b. Index reads return the typed value -----------------------------------------------------

    [Fact]
    public void IndexReadsReturnTheTypedValue_Row()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hi", RowOf("V", "hi").Text(1));
      Assert.Equal(1.5m, RowOf("V", 1.5m).Decimal(1));
      Assert.Equal(42, RowOf("V", 42).Integer(1));
      Assert.Equal(0.25, RowOf("V", 0.25).Double(1));
      Assert.Equal(moment, RowOf("V", moment).Date(1));
      Assert.True(RowOf("V", true).Boolean(1));
    }

    [Fact]
    public void IndexReadsReturnTheTypedValue_Strip()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hi", StripOf("hi").Text(1));
      Assert.Equal(1.5m, StripOf(1.5m).Decimal(1));
      Assert.Equal(42, StripOf(42).Integer(1));
      Assert.Equal(0.25, StripOf(0.25).Double(1));
      Assert.Equal(moment, StripOf(moment).Date(1));
      Assert.True(StripOf(true).Boolean(1));
    }

    // --- 1c. Wrong-kind failures speak kinds, with the A1 -------------------------------------------

    [Fact]
    public void ACaptionWrongKindSpeaksKindsWithTheA1AndTheColumnPrefix()
    {
      // The caption form names the column, so the sentence carries the `column 'V': ` prefix — the
      // A1 and the kind words are inside it.
      Assert.Equal("column 'V': expected Number at B2, found Text", CaptionFails(() => RowOf("V", "x").Decimal("V")));
      Assert.Equal("column 'V': expected Text at B2, found Number", CaptionFails(() => RowOf("V", 5m).Text("V")));
      Assert.Equal("column 'V': expected Boolean at B2, found Number", CaptionFails(() => RowOf("V", 1m).Boolean("V")));
      Assert.Equal("column 'V': expected Temporal at B2, found Text", CaptionFails(() => RowOf("V", "x").Date("V")));
      Assert.Equal("column 'V': expected Number at B2, found Text", CaptionFails(() => RowOf("V", "x").Integer("V")));
      Assert.Equal("column 'V': expected Number at B2, found Text", CaptionFails(() => RowOf("V", "x").Double("V")));
    }

    [Fact]
    public void ACaptionWrongKindCarriesTheExactA1AndKindWords()
    {
      // The explicit form of the claim: the message contains the exact A1 and the document's kind
      // vocabulary, never the reader's.
      var problem = CaptionFails(() => RowOf("V", "x").Decimal("V"));

      Assert.Contains("at B2", problem);
      Assert.Contains("expected Number", problem);
      Assert.Contains("found Text", problem);
    }

    [Fact]
    public void AnIndexWrongKindSpeaksTheBareLeafSentence_Row()
    {
      // No caption to name, so no prefix — the A1 alone pins the column.
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x").Decimal(1)));
      Assert.Equal("expected Text at B2, found Number", Fails(() => RowOf("V", 5m).Text(1)));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => RowOf("V", 1m).Boolean(1)));
      Assert.Equal("expected Temporal at B2, found Text", Fails(() => RowOf("V", "x").Date(1)));
    }

    [Fact]
    public void AnIndexWrongKindSpeaksTheBareLeafSentence_Strip()
    {
      Assert.Equal("expected Number at B2, found Text", Fails(() => StripOf("x").Decimal(1)));
      Assert.Equal("expected Text at B2, found Number", Fails(() => StripOf(5m).Text(1)));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => StripOf(1m).Boolean(1)));
      Assert.Equal("expected Temporal at B2, found Text", Fails(() => StripOf("x").Date(1)));
    }

    [Fact]
    public void AnErrorCellIsNamedAsTheErrorItIs()
    {
      // The one kind with no CLR literal and whose rendering comes from Core — the same words on
      // caption and index, differing only by the prefix.
      Assert.Equal(
        "column 'V': expected Number at B2, found Error(#DIV/0!)",
        CaptionFails(() => RowOf("V", CellValue.OfError(CellError.DivisionByZero)).Decimal("V")));

      Assert.Equal(
        "expected Number at B2, found Error(#DIV/0!)",
        Fails(() => RowOf("V", CellValue.OfError(CellError.DivisionByZero)).Decimal(1)));
    }

    [Fact]
    public void AStrictReadRefusesABlankAsAWrongKind()
    {
      // A strict read (not the OrBlank twin) treats a blank as the wrong kind, in the same words.
      Assert.Equal("column 'V': expected Number at B2, found Blank", CaptionFails(() => RowOf("V", null).Decimal("V")));
      Assert.Equal("expected Number at B2, found Blank", Fails(() => StripOf(null).Decimal(1)));
    }

    // --- 1d. Conversion failures speak conversions --------------------------------------------------

    [Fact]
    public void ACaptionConversionSpeaksTheConversionSentence()
    {
      Assert.Equal("column 'V': the Number at B2 (1.5) is not a whole number", CaptionFails(() => RowOf("V", 1.5).Integer("V")));
      Assert.Equal("column 'V': the Number at B2 (5000000000) is outside the range of a 32-bit integer", CaptionFails(() => RowOf("V", 5e9).Integer("V")));
      Assert.Equal("column 'V': the Number at B2 (1E+30) is not representable as a decimal", CaptionFails(() => RowOf("V", 1e30).Decimal("V")));
    }

    [Fact]
    public void AnIndexConversionSpeaksTheBareConversionSentence()
    {
      Assert.Equal("the Number at B2 (1.5) is not a whole number", Fails(() => StripOf(1.5).Integer(1)));
      Assert.Equal("the Number at B2 (5000000000) is outside the range of a 32-bit integer", Fails(() => StripOf(5e9).Integer(1)));
      Assert.Equal("the Number at B2 (1E+30) is not representable as a decimal", Fails(() => StripOf(1e30).Decimal(1)));
    }

    // --- 2. OrBlank returns null on a blank, still throws on a wrong kind ----------------------------

    [Fact]
    public void OrBlankReturnsNullOnABlankCell_Caption()
    {
      Assert.Null(RowOf("V", null).TextOrBlank("V"));
      Assert.Null(RowOf("V", null).DecimalOrBlank("V"));
      Assert.Null(RowOf("V", null).IntegerOrBlank("V"));
      Assert.Null(RowOf("V", null).DoubleOrBlank("V"));
      Assert.Null(RowOf("V", null).DateOrBlank("V"));
      Assert.Null(RowOf("V", null).BooleanOrBlank("V"));
    }

    [Fact]
    public void OrBlankReturnsNullOnABlankCell_Index()
    {
      Assert.Null(RowOf("V", null).DecimalOrBlank(1));
      Assert.Null(RowOf("V", null).TextOrBlank(1));
      Assert.Null(StripOf(null).DecimalOrBlank(1));
      Assert.Null(StripOf(null).TextOrBlank(1));
      Assert.Null(StripOf(null).DateOrBlank(1));
      Assert.Null(StripOf(null).BooleanOrBlank(1));
    }

    [Fact]
    public void OrBlankStillThrowsOnAWrongKindCell_Caption()
    {
      // Blank tolerance is not kind tolerance: a Text cell in a Number column still fails, in the
      // same words the strict read would use.
      Assert.Equal("column 'V': expected Number at B2, found Text", CaptionFails(() => RowOf("V", "x").DecimalOrBlank("V")));
      Assert.Equal("column 'V': expected Boolean at B2, found Number", CaptionFails(() => RowOf("V", 1m).BooleanOrBlank("V")));
    }

    [Fact]
    public void OrBlankStillThrowsOnAWrongKindCell_Index()
    {
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x").DecimalOrBlank(1)));
      Assert.Equal("expected Number at B2, found Text", Fails(() => StripOf("x").DecimalOrBlank(1)));
      Assert.Equal("expected Temporal at B2, found Number", Fails(() => StripOf(5m).DateOrBlank(1)));
    }

    // --- 3. The identity claim: the accessor describes a bad cell as the leaf/binder does -----------

    [Fact]
    public void ACaptionReadDescribesABadCellExactlyAsTheBinderDoes()
    {
      // Same sheet, both readers: the caption accessor's sentence is the binder's, prefix and all.
      var sheet = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var byBinder = Assert.Throws<ProjectionException>(() => Table<Money>().Map(sheet));
      var row = Assert.Single(Table((TableRow r) => r).Map(sheet));
      var byAccessor = Assert.Throws<ProjectionException>(() => row.Decimal("Amount"));

      Assert.Equal("column 'Amount': expected Number at B2, found Text", Problem(byBinder));
      Assert.Equal(Problem(byBinder), Problem(byAccessor));
    }

    [Fact]
    public void ACaptionConversionDescribesABadCellExactlyAsTheBinderDoes()
    {
      var sheet = Mixed(new object?[,]
      {
        { "Client", "Count" },
        { "Acme", 1.5 },
      });

      var byBinder = Assert.Throws<ProjectionException>(() => Table<Counted>().Map(sheet));
      var row = Assert.Single(Table((TableRow r) => r).Map(sheet));
      var byAccessor = Assert.Throws<ProjectionException>(() => row.Integer("Count"));

      Assert.Equal("column 'Count': the Number at B2 (1.5) is not a whole number", Problem(byBinder));
      Assert.Equal(Problem(byBinder), Problem(byAccessor));
    }

    [Fact]
    public void AnIndexReadDescribesABadCellExactlyAsABareLeafDoes()
    {
      // The leaf placed on B2, the strip index 1 (B2), the row index 1 (B2): one bare sentence, no
      // `column '...'` prefix, the A1 pinning the column on all three.
      var grid = Mixed(new object?[,]
      {
        { "top", "top" },
        { "left", "x" },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(Decimal()).Map(grid));
      var byStrip = Assert.Throws<ProjectionException>(() => Down(1).Of(Row(2, r => r)).Map(grid).Decimal(1));
      var byRow = Assert.Throws<ProjectionException>(() => Assert.Single(Table((TableRow r) => r).Map(grid)).Decimal(1));

      Assert.Equal("expected Number at B2, found Text", Problem(byLeaf));
      Assert.Equal(Problem(byLeaf), Problem(byStrip));
      Assert.Equal(Problem(byLeaf), Problem(byRow));
    }

    [Fact]
    public void AnIndexConversionDescribesABadCellExactlyAsABareLeafDoes()
    {
      var grid = Mixed(new object?[,]
      {
        { "top", "top" },
        { "left", 1.5 },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(Integer()).Map(grid));
      var byStrip = Assert.Throws<ProjectionException>(() => Down(1).Of(Row(2, r => r)).Map(grid).Integer(1));

      Assert.Equal("the Number at B2 (1.5) is not a whole number", Problem(byLeaf));
      Assert.Equal(Problem(byLeaf), Problem(byStrip));
    }

    // --- 4. Lookup errors are clear — never a NullReferenceException --------------------------------

    [Fact]
    public void AMissingCaptionThrowsAClearProjectionException()
    {
      var row = RowOf("Amount", 10m);

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal("Net"));

      Assert.Contains("there is no column named 'Net'", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Fact]
    public void AnAmbiguousCaptionThrowsAClearProjectionException()
    {
      var row = Assert.Single(Table((TableRow r) => r).Map(Mixed(new object?[,]
      {
        { "Amount", "Amount" },
        { 1m, 2m },
      })));

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal("Amount"));

      Assert.Contains("appears at indices 0 and 1; use the index.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Fact]
    public void ACaptionLookupAgainstAHeaderlessTableThrowsAClearProjectionException()
    {
      // headerRows: 0 — no header a caption could resolve against, so the lookup is a broken
      // declaration, not a missing value.
      var row = Assert.Single(Table(0, (TableRow r) => r).Map(Mixed(new object?[,]
      {
        { "Acme", 10m },
      })));

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal("Amount"));

      Assert.Contains("the table was declared without a header row; use column indices.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void AnOutOfRangeIndexOnARowThrowsAClearProjectionException(int column)
    {
      var row = RowOf("Amount", 10m);

      var failure = Assert.Throws<ProjectionException>(() => row.Decimal(column));

      Assert.Contains("is out of range; the table has 2 columns.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void AnOutOfRangeIndexOnAStripThrowsArgumentOutOfRange(int index)
    {
      var strip = StripOf(10m);

      var failure = Assert.Throws<ArgumentOutOfRangeException>(() => strip.Decimal(index));

      Assert.IsNotType<NullReferenceException>(failure);
    }

    // --- 5. Coordinate threading: a sub-strip reports its own A1, not the parent's ------------------

    [Fact]
    public void ABlockRowSubStripReportsItsOwnA1NotTheParents()
    {
      // Range hands back the whole 2x2 block; block.Row(1) is a sub-strip whose context was advanced
      // by the row offset, so its cell 1 is B2 — the parent block's top-left is A1.
      var grid = Mixed(new object?[,]
      {
        { "a", "b" },
        { "c", "x" },   // B2
      });

      var block = Range(b => b).Map(grid);

      Assert.Equal("B1", block.Row(0).AddressOf(1).A1);
      Assert.Equal("B2", block.Row(1).AddressOf(1).A1);

      var failure = Assert.Throws<ProjectionException>(() => block.Row(1).Decimal(1));

      Assert.Equal("expected Number at B2, found Text", Problem(failure));
    }

    [Fact]
    public void ABlockColumnSubStripReportsItsOwnA1NotTheParents()
    {
      var grid = Mixed(new object?[,]
      {
        { "a", "b" },
        { "c", "x" },   // B2
      });

      var block = Range(b => b).Map(grid);

      Assert.Equal("B1", block.Column(1).AddressOf(0).A1);
      Assert.Equal("B2", block.Column(1).AddressOf(1).A1);

      var failure = Assert.Throws<ProjectionException>(() => block.Column(1).Decimal(1));

      Assert.Equal("expected Number at B2, found Text", Problem(failure));
    }

    // --- Small shared throw-and-read helpers -------------------------------------------------------

    /// <summary>The bare cell-describing sentence of a failed accessor call.</summary>
    private static string Fails(Action read) => Problem(Assert.Throws<ProjectionException>(read));

    /// <summary>The same, spelled to read as "a caption read fails" at the call site.</summary>
    private static string CaptionFails(Action read) => Problem(Assert.Throws<ProjectionException>(read));
  }
}
