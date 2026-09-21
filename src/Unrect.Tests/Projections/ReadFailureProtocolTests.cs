using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// What happens when a cell will not read from inside a LAMBDA — the seam between a backend that
  /// knows what went wrong and a projection layer that knows what to call the place it went wrong at.
  /// <para>
  /// <b>The protocol.</b> A backend that reads a cell raises a <see cref="CellReadException"/>: a
  /// sentence with the address left as a hole, plus the point that fills it. The projection whose
  /// lambda made the call catches it and rethrows it as a <see cref="ProjectionException"/> carrying
  /// the declaration path — and that is the whole of it. No arithmetic, no conditional, one catch
  /// clause per site, and one place (<c>ProjectionContext.Reading</c>) that decides the wording, so a
  /// cell that would not read describes itself identically whether it was reached from a leaf, a row,
  /// a block, a record, a table or a layout body.
  /// </para>
  /// <para>
  /// <b>Why it needs a test per site rather than one test.</b> The catch is copied, not shared: each
  /// primitive that invokes user code has its own clause. A site that forgets one does not fail
  /// loudly — the read still fails, but it escapes as a bare exception with no path and no A1, which
  /// looks like an engine bug to the reader and like nothing at all to a tolerance boundary. That is
  /// risk R-6, and this file is its catch.
  /// </para>
  /// <para>
  /// <b>What is asserted at each site.</b> The declaration path, so the failure says which part of
  /// the declaration was reading; the cell's own A1 inside the sentence, so it says which cell; that
  /// it is not a fault; and that a tolerance boundary can therefore absorb it. The extent cited
  /// beside the path is the extent of the projection that CAUGHT the read — a different and also
  /// true fact, and the reason the sentence carries its own address at all.
  /// </para>
  /// </summary>
  public class ReadFailureProtocolTests
  {
    /// <summary>
    /// A two-by-two sheet whose B2 is text. Every case below reads B2 as a decimal, so one cell and
    /// one sentence serve the whole file and the only thing that varies is which lambda asked.
    /// </summary>
    private static ICellSpace Sheet() => Mixed(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", "oops" },
    });

    /// <summary>
    /// A labelled card: two columns, one row per field, with the Amount field's value at B2 — the
    /// same cell as the sheet above, so the sentence is the same one.
    /// </summary>
    private static ICellSpace Card() => Mixed(new object?[,]
    {
      { "Client", "Acme" },
      { "Amount", "oops" },
    });

    private const string Sentence = "expected Number at B2, found Text";

    /// <summary>
    /// Every lambda site a cell read can happen inside, each declared to read B2 as a decimal.
    /// <para>
    /// The name is the site; the value is the declaration and the path its failure must carry.
    /// Written as a switch rather than as inline theory data so each case can say what it is.
    /// </para>
    /// </summary>
    private static (IProjectionDefinition<ICellSpace, object?> Declaration, string Path, ICellSpace Sheet) Site(string site) => site switch
    {
      // SelectDefinition — a point handed to Select, which is how every reading the vocabulary does not
      // name is spelled.
      //
      // Named, so this case pins a Select that claims a path segment of its own. The unnamed —
      // transparent — spelling at the root of a declaration is its own corner and is pinned by
      // ATransparentProjectionAtTheRootSpeaksForItself below.
      "Select" => (Right(1).Down(1).Of(Point().Select(p => (object?)p.Decimal()).Named("amount")), "'amount' (Select)", Sheet()),

      // StripDefinition, both axes: a row and a column handed over as cells to be read by index.
      "Row" => (Down(1).Of(Row(2, cells => (object?)cells[1].Decimal())), "Row(2)", Sheet()),
      "Column" => (Right(1).Of(Column(2, cells => (object?)cells[1].Decimal())), "Column(2)", Sheet()),

      // BlockDefinition — a rectangle handed over whole.
      "Range" => (Range(2, 2, block => (object?)block[1, 1].Decimal()), "Range(2, 2)", Sheet()),

      // RecordDefinition — one labelled row, resolved through the ambient columns.
      "Record" => (
        WithColumnLabels(LabelMap.Of(("Client", 0), ("Amount", 1)), Down(1).Of(Record((TableRow<ICellSpace> row) => (object?)row["Amount"].Decimal()))),
        "Record",
        Sheet()),

      // TableViewDefinition, the row-lambda rung...
      "Table(row)" => (
        Table((TableRow<ICellSpace> row) => (object?)row["Amount"].Decimal()).Select(rows => (object?)rows).Named("rows"),
        "Table",
        Sheet()),

      // ...and the view-lambda rung, which reads the whole table at once.
      "Table(view)" => (Table((TableView<ICellSpace> table) => (object?)table.Rows[0]["Amount"].Decimal()), "Table", Sheet()),

      // Fields' consumer: the card yields points, and reading one is the consumer's own business.
      // Its sheet is a labelled CARD — two columns by one row per field — rather than a table, and
      // the Select is named for the reason the Select case above is.
      "Fields" => (
        Fields(Field("Client"), Field("Amount")).Select(card => (object?)card["Amount"].Decimal()).Named("card"),
        "'card' (Select)",
        Card()),

      _ => throw new ArgumentOutOfRangeException(nameof(site), site, "No such site."),
    };

    public static TheoryData<string> Sites => new TheoryData<string>
    {
      "Select", "Row", "Column", "Range", "Record", "Table(row)", "Table(view)", "Fields",
    };

    [Theory]
    [MemberData(nameof(Sites))]
    public void AReadThatFailsInsideALambdaCarriesThePathAndTheCellsOwnA1(string site)
    {
      var (declaration, path, sheet) = Site(site);

      var failure = Assert.Throws<ProjectionException>(() => declaration.Map(sheet));

      // One sentence, whichever lambda asked — which is the claim the shared Reading() call is for.
      Assert.Equal(Sentence, Problem(failure));

      // The declaration path, so the reader can find the line that was reading...
      Assert.Contains(path, failure.Path);

      // ...and the cell, in the sentence, because the point knew where it was and the layer knew
      // what to call the place.
      Assert.Contains("at B2", failure.Message);
    }

    [Theory]
    [MemberData(nameof(Sites))]
    public void AndItIsNotAFaultSoEveryToleranceBoundaryTakesIt(string site)
    {
      // A cell of the wrong kind is a statement about the DATA. Classifying it as a fault would make
      // a malformed section unrecoverable everywhere at once, which is the opposite of what the
      // tolerance boundaries exist for — so the three of them are each asked here rather than the
      // flag being read directly.
      var (declaration, _, sheet) = Site(site);

      Assert.False(Assert.Throws<ProjectionException>(() => declaration.Map(sheet)).IsFault);

      Assert.Null(declaration.Optional().Map(sheet));
      Assert.Equal("absorbed", declaration.Select(_ => "read").Named("read").Else("absorbed").Map(sheet));
      Assert.Equal("second", Choice(declaration.Select(_ => "first").Named("first"), AsText().Select(_ => "second")).Map(sheet));
    }

    // --- The corner at the root --------------------------------------------------------------------

    [Fact]
    public void ATransparentProjectionAtTheRootSpeaksForItself()
    {
      // An unnamed Select is TRANSPARENT: it contributes no path segment, so the engine does not
      // descend into it and hands it the context it was called with. At the root of a declaration
      // that is the ROOT context, which is inside no projection at all — so the engine tells it
      // which projection it is applying (ProjectionContext.Blaming), and a read that fails in the
      // Select's own lambda is reported against the Select rather than against nothing.
      //
      // Reached through ProjectionContext.Reading at every transparent catch site. Nested one level
      // down the enclosing projection's context is what translates it (the control below), and a
      // NAMED Select is opaque and speaks for itself — so this is exactly the root-and-unnamed
      // corner, which is also the shortest declaration anybody writes while exploring a sheet.
      var failure = Assert.Throws<ProjectionException>(
        () => Right(1).Down(1).Of(Point().Select(p => p.Decimal())).Map(Sheet()));

      Assert.Equal(Sentence, Problem(failure));
      Assert.DoesNotContain("no projection to blame", failure.Message);
    }

    [Fact]
    public void AndTheSameReadOneLevelDownIsTranslatedNormally()
    {
      // The control for the Skip above, and the reason it is a corner rather than a hole: the
      // instant the transparent Select has anything above it, the enclosing projection's context is
      // what translates the read, and the sentence survives. A transparent wrapper contributes no
      // segment, so the failure is attributed to the layout — which is the transparency rule, not a
      // second bug.
      var failure = Assert.Throws<ProjectionException>(
        () => Overlay(o =>
        {
          var right = o.Next(Right(1).Down(1).Of(Point().Select(p => p.Decimal())));

          return right;
        }).Map(Sheet()));

      Assert.Equal(Sentence, Problem(failure));
      Assert.Equal("Overlay", failure.Path);
    }

    // --- The other half: a lambda that throws something the engine has no vocabulary for -----------

    [Fact]
    public void AUsersOwnExceptionInALambdaIsStillWrappedGenerically()
    {
      // The protocol catches CellReadException and nothing else, deliberately: anything else is an
      // exception the engine cannot have a sentence for, so it says what it was and hands the
      // original along.
      var failure = Assert.Throws<ProjectionException>(
        () => Point().Select<ICellSpace, Point<ICellSpace>, int>(_ => throw new InvalidOperationException("boom")).Map(Sheet()));

      Assert.Contains("the projection threw InvalidOperationException: boom", failure.Message);
      Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    [Fact]
    public void AndAUsersOwnInvalidCastExceptionIsAFault()
    {
      // The law that is easy to get backwards. A cast that did not hold is an invariant this library
      // or its caller owes itself — it is never a statement about the data — so it must NOT be
      // absorbable, or a broken declaration would be reported as an absent section and the parse
      // would carry on over it.
      //
      // It is worth a pin of its own because it is the one entry on the fault list a reader would
      // expect to find on the other side: every other fault is plainly a bug or a broken file
      // (NullReference, IndexOutOfRange, IO, ObjectDisposed), and a failed cast can look like bad
      // data if you squint at it.
      IProjectionDefinition<ICellSpace, int> miscast =
        Point().Select<ICellSpace, Point<ICellSpace>, int>(_ => throw new InvalidCastException("not that type"));

      Assert.True(Assert.Throws<ProjectionException>(() => miscast.Map(Sheet())).IsFault);

      // And no boundary takes it — which is the property the flag exists to produce.
      Assert.Throws<ProjectionException>(() => miscast.Optional().Map(Sheet()));
      Assert.Throws<ProjectionException>(() => miscast.Else(-1).Map(Sheet()));
      Assert.Throws<ProjectionException>(() => Choice(miscast, Integer()).Map(Sheet()));
    }

    [Fact]
    public void AndAReadAfterTheWorkbookClosedIsAFaultToo()
    {
      // The same law from the streaming side, and the reason it matters: a read that failed because
      // the file is gone must never be reported as a section that was not there. Classified from the
      // exception type, so every absorbing site gets it without knowing what a workbook is.
      Assert.True(EngineRules.IsFault(new ObjectDisposedException("Workbook")));
      Assert.True(EngineRules.IsFault(new System.IO.IOException("the share went away")));
      Assert.False(EngineRules.IsFault(new InvalidOperationException("a lambda blew up")));
    }
  }
}
