using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// The compute-legal binder's coordinate-aware accessors: <c>row.Decimal("Amount")</c> and
  /// <c>row.Text(8)</c> on a <see cref="TableRow{TSpace}"/>, <c>strip.Decimal(i)</c> on a <see
  /// cref="CellStrip{TSpace}"/>. Each reads the cell through the one canonical accessor for its kind, so on a
  /// bad cell it must describe the failure with the very same words the matching leaf and the
  /// <c>Table&lt;T&gt;</c> binder use — the leaf firewall holding across the accessor layer, which is
  /// the reason the binder is faithful.
  /// <para>
  /// <strong>One sentence, wherever it is read from.</strong> A caption read and an index read of the
  /// same bad cell now say exactly the same thing — <c>expected Number at B2, found Text</c> — with
  /// no <c>column 'Amount': </c> in front of either. The caption is not part of the sentence; it is
  /// the name of a declared column, and phase 6 put it where declared things are named, on the path
  /// and the subject of the failure the BINDER raises. An accessor call has no declaration around it
  /// to name, so it has nothing to prefix the sentence with — which is the honest shape, and is what
  /// makes the sentence comparable across every reader in this file.
  /// </para>
  /// <para>
  /// <strong>Where the exception comes from, and the premise of the whole file.</strong> The
  /// accessors are exercised by obtaining the view through a projection that hands the view back
  /// (<c>Table((TableRow r) =&gt; r)</c>, <c>Row(w, r =&gt; r)</c>, <c>Range(b =&gt; b)</c>) and then
  /// calling the accessor AFTER the projection has finished — the way a user's post-parse
  /// computation calls it. Outside every lambda there is no path, no context and nothing to absorb
  /// the failure, so what surfaces is the bare <see cref="CellReadException"/>: the backend's
  /// sentence with the A1 already in it and nothing else. Call the same accessor from INSIDE the
  /// lambda and the projection that invoked it catches that exception and rethrows it as a located
  /// <see cref="ProjectionException"/> carrying the declaration path — and a tolerance boundary can
  /// absorb it. <see cref="TheSameReadIsBareOutsideALambdaAndLocatedInside"/> is that contrast, in
  /// one test.
  /// </para>
  /// <para>
  /// The lookup failures are the exception to the exception: a missing caption, an ambiguous one and
  /// an out-of-range column index are broken DECLARATIONS rather than statements about a cell, so
  /// they stay <see cref="ProjectionException"/> wherever they are raised from, and a CellStrip index
  /// out of range stays an <see cref="ArgumentOutOfRangeException"/>.
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
    /// value cell at B2, handed back as a <see cref="TableRow{TSpace}"/> without being read, so an accessor
    /// call on it is observed directly.
    /// </summary>
    private static TableRow<ICellSpace> RowOf(string caption, object? value)
      => Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(Mixed(new object?[,]
      {
        { "Client", caption },
        { "Acme", value },
      })));

    /// <summary>A row strip A2:B2 with <paramref name="value"/> at index 1 (B2), handed back unread.</summary>
    private static CellStrip<ICellSpace> StripOf(object? value)
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

      Assert.Equal("hi", RowOf("V", "hi")["V"].Text());
      Assert.Equal(1.5m, RowOf("V", 1.5m)["V"].Decimal());
      Assert.Equal(42, RowOf("V", 42)["V"].Integer());
      Assert.Equal(0.25, RowOf("V", 0.25)["V"].Double());
      Assert.Equal(moment, RowOf("V", moment)["V"].Date());
      Assert.True(RowOf("V", true)["V"].Boolean());
    }

    [Fact]
    public void ACaptionReadResolvesTheColumnByNameNotByPosition()
    {
      // The point of caption keying: a reordered export needs no change. "Client" is column B here,
      // and the read finds it there rather than at index 0.
      var row = Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(Mixed(new object?[,]
      {
        { "Amount", "Client" },
        { 10m, "Acme" },
      })));

      Assert.Equal("Acme", row["Client"].Text());
      Assert.Equal(10m, row["Amount"].Decimal());
    }

    // --- 1b. Index reads return the typed value -----------------------------------------------------

    [Fact]
    public void IndexReadsReturnTheTypedValue_Row()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hi", RowOf("V", "hi")[1].Text());
      Assert.Equal(1.5m, RowOf("V", 1.5m)[1].Decimal());
      Assert.Equal(42, RowOf("V", 42)[1].Integer());
      Assert.Equal(0.25, RowOf("V", 0.25)[1].Double());
      Assert.Equal(moment, RowOf("V", moment)[1].Date());
      Assert.True(RowOf("V", true)[1].Boolean());
    }

    [Fact]
    public void IndexReadsReturnTheTypedValue_Strip()
    {
      var moment = new DateTime(2026, 3, 4, 13, 45, 0);

      Assert.Equal("hi", StripOf("hi")[1].Text());
      Assert.Equal(1.5m, StripOf(1.5)[1].Decimal());
      Assert.Equal(42, StripOf(42)[1].Integer());
      Assert.Equal(0.25, StripOf(0.25)[1].Double());
      Assert.Equal(moment, StripOf(moment)[1].Date());
      Assert.True(StripOf(true)[1].Boolean());
    }

    // --- 1c. Wrong-kind failures speak kinds, with the A1 -------------------------------------------

    [Fact]
    public void ACaptionWrongKindSpeaksKindsWithTheA1()
    {
      // The caption form resolves WHICH cell; it contributes nothing to the sentence about it. So
      // the A1 and the kind words are all there is — the same six sentences an index read gives.
      // Until phase 6 each of these arrived behind a `column 'V': ` the accessor added.
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")["V"].Decimal()));
      Assert.Equal("expected Text at B2, found Number", Fails(() => RowOf("V", 5m)["V"].Text()));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => RowOf("V", 1m)["V"].Boolean()));
      Assert.Equal("expected Temporal at B2, found Text", Fails(() => RowOf("V", "x")["V"].Date()));
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")["V"].Integer()));
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")["V"].Double()));
    }

    [Fact]
    public void ACaptionWrongKindCarriesTheExactA1AndKindWords()
    {
      // The explicit form of the claim: the message contains the exact A1 and the document's kind
      // vocabulary, never the reader's.
      var problem = Fails(() => RowOf("V", "x")["V"].Decimal());

      Assert.Contains("at B2", problem);
      Assert.Contains("expected Number", problem);
      Assert.Contains("found Text", problem);
    }

    [Fact]
    public void AnIndexWrongKindSpeaksTheBareLeafSentence_Row()
    {
      // The A1 alone pins the column, and — since phase 6 — so it does on a caption read too.
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")[1].Decimal()));
      Assert.Equal("expected Text at B2, found Number", Fails(() => RowOf("V", 5m)[1].Text()));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => RowOf("V", 1m)[1].Boolean()));
      Assert.Equal("expected Temporal at B2, found Text", Fails(() => RowOf("V", "x")[1].Date()));
    }

    [Fact]
    public void AnIndexWrongKindSpeaksTheBareLeafSentence_Strip()
    {
      Assert.Equal("expected Number at B2, found Text", Fails(() => StripOf("x")[1].Decimal()));
      Assert.Equal("expected Text at B2, found Number", Fails(() => StripOf(5)[1].Text()));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => StripOf(1)[1].Boolean()));
      Assert.Equal("expected Temporal at B2, found Text", Fails(() => StripOf("x")[1].Date()));
    }

    [Fact]
    public void AnErrorCellIsNamedAsTheErrorItIs()
    {
      // The one kind with no CLR literal and whose rendering comes from Core — the same words on
      // caption and index, and since phase 6 not differing at all.
      Assert.Equal(
        "expected Number at B2, found Error(#DIV/0!)",
        Fails(() => RowOf("V", CellValue.OfError(CellError.DivisionByZero))["V"].Decimal()));

      Assert.Equal(
        "expected Number at B2, found Error(#DIV/0!)",
        Fails(() => RowOf("V", CellValue.OfError(CellError.DivisionByZero))[1].Decimal()));
    }

    [Fact]
    public void AStrictReadRefusesABlankAsAWrongKind()
    {
      // A strict read (not the OrBlank twin) treats a blank as the wrong kind, in the same words.
      Assert.Equal("expected Number at B2, found Blank", Fails(() => RowOf("V", null)["V"].Decimal()));
      Assert.Equal("expected Number at B2, found Blank", Fails(() => StripOf(null)[1].Decimal()));
    }

    // --- 1d. Conversion failures speak conversions --------------------------------------------------

    [Fact]
    public void ACaptionConversionSpeaksTheConversionSentence()
    {
      Assert.Equal("the Number at B2 (1.5) is not a whole number", Fails(() => RowOf("V", 1.5)["V"].Integer()));
      Assert.Equal("the Number at B2 (5000000000) is outside the range of a 32-bit integer", Fails(() => RowOf("V", 5e9)["V"].Integer()));
      Assert.Equal("the Number at B2 (1E+30) is not representable as a decimal", Fails(() => RowOf("V", 1e30)["V"].Decimal()));
    }

    [Fact]
    public void AnIndexConversionSpeaksTheBareConversionSentence()
    {
      Assert.Equal("the Number at B2 (1.5) is not a whole number", Fails(() => StripOf(1.5)[1].Integer()));
      Assert.Equal("the Number at B2 (5000000000) is outside the range of a 32-bit integer", Fails(() => StripOf(5e9)[1].Integer()));
      Assert.Equal("the Number at B2 (1E+30) is not representable as a decimal", Fails(() => StripOf(1e30)[1].Decimal()));
    }

    // --- 2. OrBlank returns null on a blank, still throws on a wrong kind ----------------------------

    [Fact]
    public void OrBlankReturnsNullOnABlankCell_Caption()
    {
      Assert.Null(RowOf("V", null)["V"].TextOrBlank());
      Assert.Null(RowOf("V", null)["V"].DecimalOrBlank());
      Assert.Null(RowOf("V", null)["V"].IntegerOrBlank());
      Assert.Null(RowOf("V", null)["V"].DoubleOrBlank());
      Assert.Null(RowOf("V", null)["V"].DateOrBlank());
      Assert.Null(RowOf("V", null)["V"].BooleanOrBlank());
    }

    [Fact]
    public void OrBlankReturnsNullOnABlankCell_Index()
    {
      Assert.Null(RowOf("V", null)[1].DecimalOrBlank());
      Assert.Null(RowOf("V", null)[1].TextOrBlank());
      Assert.Null(StripOf(null)[1].DecimalOrBlank());
      Assert.Null(StripOf(null)[1].TextOrBlank());
      Assert.Null(StripOf(null)[1].DateOrBlank());
      Assert.Null(StripOf(null)[1].BooleanOrBlank());
    }

    [Fact]
    public void OrBlankStillThrowsOnAWrongKindCell_Caption()
    {
      // Blank tolerance is not kind tolerance: a Text cell in a Number column still fails, in the
      // same words the strict read would use.
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")["V"].DecimalOrBlank()));
      Assert.Equal("expected Boolean at B2, found Number", Fails(() => RowOf("V", 1m)["V"].BooleanOrBlank()));
    }

    [Fact]
    public void OrBlankStillThrowsOnAWrongKindCell_Index()
    {
      Assert.Equal("expected Number at B2, found Text", Fails(() => RowOf("V", "x")[1].DecimalOrBlank()));
      Assert.Equal("expected Number at B2, found Text", Fails(() => StripOf("x")[1].DecimalOrBlank()));
      Assert.Equal("expected Temporal at B2, found Number", Fails(() => StripOf(5)[1].DateOrBlank()));
    }

    // --- 3. The identity claim: the accessor describes a bad cell as the leaf/binder does -----------

    [Fact]
    public void ACaptionReadDescribesABadCellExactlyAsTheBinderDoes()
    {
      // Same sheet, both readers, one sentence — and since phase 6 the equality needs no allowance
      // made for a prefix on either side.
      var sheet = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var byBinder = Assert.Throws<ProjectionException>(() => Table<Money>().Map(sheet));
      var row = Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(sheet));
      var byAccessor = Assert.Throws<CellReadException>(() => row["Amount"].Decimal());

      Assert.Equal("expected Number at B2, found Text", Problem(byBinder));
      Assert.Equal(Problem(byBinder), byAccessor.Message);
    }

    [Fact]
    public void AnAccessorOutsideALambdaCarriesTheSentenceAndNothingElse()
    {
      // The whole message of an accessor failure, assembled as a reader sees it — the twin of the
      // bind-side pin in CellReadingIdentityTests. There is nothing around it: called after the
      // projection has returned, the accessor has no declaration to be named by, no path to sit in
      // and no extent to be located against, so the message is the backend's sentence with the A1
      // already inside it. The other pins in this file assert that sentence one at a time; it is
      // the ABSENCE of a wrapper that is stated here, once, so growing one back is a visible diff.
      //
      // Until phase 6 this read `Table: column 'Amount': …` in Table at A2 with 2x1 available: the
      // accessor reached for the table's own context, prefixed the sentence with the caption, and
      // located the failure at the row rather than at the cell.
      var sheet = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var row = Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(sheet));
      var failure = Assert.Throws<CellReadException>(() => row["Amount"].Decimal());

      Assert.Equal("expected Number at B2, found Text", failure.Message);

      // And the point it carries is the cell itself, in the sheet's own coordinates — which is what
      // the projection layer fills the address hole from when the read happens inside a lambda.
      Assert.Equal(1, failure.At.Column);
      Assert.Equal(1, failure.At.Row);
    }

    [Fact]
    public void TheSameReadIsBareOutsideALambdaAndLocatedInside()
    {
      // The file's premise, as one test. The same accessor on the same cell: outside every lambda a
      // bare CellReadException with the sentence; inside the row lambda, the projection that invoked
      // the lambda catches it and rethrows it located — the declaration path, the cell's A1, and the
      // same sentence character for character.
      var sheet = Mixed(new object?[,]
      {
        { "Client", "Amount" },
        { "Acme", "x" },
      });

      var outside = Assert.Throws<CellReadException>(
        () => Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(sheet))["Amount"].Decimal());

      var inside = Assert.Throws<ProjectionException>(
        () => Table((TableRow<ICellSpace> r) => r["Amount"].Decimal()).Map(sheet));

      Assert.Equal(outside.Message, Problem(inside));
      Assert.Equal("Table", inside.Path);

      // The cell's own A1 is INSIDE the sentence, which is where the protocol puts it: the backend
      // knows which cell it read, the projection layer knows what to call that place, and the
      // address is filled in at the catch. The location cited beside the path is the extent of the
      // projection that caught the read — the table's, here — which is a different and also true
      // fact, and the reason the sentence carries its own address at all.
      Assert.Contains("at B2", inside.Message);
      Assert.Equal("A1", inside.Location.A1);

      // And because it is a statement about the data rather than a fault, the located one is
      // absorbable — which the bare one, having no boundary around it, could never be.
      Assert.False(inside.IsFault);
      Assert.Null(Table((TableRow<ICellSpace> r) => r["Amount"].Decimal()).Optional().Map(sheet));
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
      var row = Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(sheet));
      var byAccessor = Assert.Throws<CellReadException>(() => row["Count"].Integer());

      Assert.Equal("the Number at B2 (1.5) is not a whole number", Problem(byBinder));
      Assert.Equal(Problem(byBinder), byAccessor.Message);
    }

    [Fact]
    public void AnIndexReadDescribesABadCellExactlyAsABareLeafDoes()
    {
      // The leaf placed on B2, the strip index 1 (B2), the row index 1 (B2): one sentence, the A1
      // pinning the column on all three. Only the leaf runs inside a projection, so only the leaf's
      // arrives wrapped — which is the difference this comparison is designed to see through.
      var grid = Mixed(new object?[,]
      {
        { "top", "top" },
        { "left", "x" },
      });

      var byLeaf = Assert.Throws<ProjectionException>(() => Right(1).Down(1).Of(Decimal()).Map(grid));
      var byStrip = Assert.Throws<CellReadException>(() => Down(1).Of(Row(2, r => r)).Map(grid)[1].Decimal());
      var byRow = Assert.Throws<CellReadException>(() => Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(grid))[1].Decimal());

      Assert.Equal("expected Number at B2, found Text", Problem(byLeaf));
      Assert.Equal(Problem(byLeaf), byStrip.Message);
      Assert.Equal(Problem(byLeaf), byRow.Message);
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
      var byStrip = Assert.Throws<CellReadException>(() => Down(1).Of(Row(2, r => r)).Map(grid)[1].Integer());

      Assert.Equal("the Number at B2 (1.5) is not a whole number", Problem(byLeaf));
      Assert.Equal(Problem(byLeaf), byStrip.Message);
    }

    // --- 4. Lookup errors are clear — never a NullReferenceException --------------------------------

    [Fact]
    public void AMissingCaptionThrowsAClearProjectionException()
    {
      var row = RowOf("Amount", 10m);

      var failure = Assert.Throws<ProjectionException>(() => row["Net"].Decimal());

      Assert.Contains("there is no column named 'Net'", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Fact]
    public void AnAmbiguousCaptionThrowsAClearProjectionException()
    {
      var row = Assert.Single(Table((TableRow<ICellSpace> r) => r).Map(Mixed(new object?[,]
      {
        { "Amount", "Amount" },
        { 1m, 2m },
      })));

      var failure = Assert.Throws<ProjectionException>(() => row["Amount"].Decimal());

      Assert.Contains("appears at indices 0 and 1; use the index.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Fact]
    public void ACaptionLookupAgainstAHeaderlessTableThrowsAClearProjectionException()
    {
      // headerRows: 0 — no header a caption could resolve against, so the lookup is a broken
      // declaration, not a missing value.
      var row = Assert.Single(Table(0, (TableRow<ICellSpace> r) => r).Map(Mixed(new object?[,]
      {
        { "Acme", 10m },
      })));

      var failure = Assert.Throws<ProjectionException>(() => row["Amount"].Decimal());

      Assert.Contains("the table was declared without a header row; use column indices.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void AnOutOfRangeIndexOnARowThrowsAClearProjectionException(int column)
    {
      var row = RowOf("Amount", 10m);

      var failure = Assert.Throws<ProjectionException>(() => row[column].Decimal());

      Assert.Contains("is out of range; the table has 2 columns.", failure.Message);
      Assert.IsNotType<NullReferenceException>(failure);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void AnOutOfRangeIndexOnAStripThrowsArgumentOutOfRange(int index)
    {
      var strip = StripOf(10);

      var failure = Assert.Throws<ArgumentOutOfRangeException>(() => strip[index].Decimal());

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

      var failure = Assert.Throws<CellReadException>(() => block.Row(1)[1].Decimal());

      Assert.Equal("expected Number at B2, found Text", failure.Message);
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

      var failure = Assert.Throws<CellReadException>(() => block.Column(1)[1].Decimal());

      Assert.Equal("expected Number at B2, found Text", failure.Message);
    }

    // --- Small shared throw-and-read helpers -------------------------------------------------------

    /// <summary>
    /// The cell-describing sentence of a failed accessor call made outside every lambda — which is
    /// the whole message, because a bare <see cref="CellReadException"/> is the sentence and nothing
    /// else. Until phase 6 this unwrapped a <see cref="ProjectionException"/> instead, and a caption
    /// read's sentence arrived with a <c>column '…': </c> in front of it.
    /// </summary>
    private static string Fails(Action read) => Assert.Throws<CellReadException>(read).Message;
  }
}
