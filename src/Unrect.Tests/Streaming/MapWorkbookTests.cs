using System;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests.Streaming
{
  /// <summary>
  /// <c>MapWorkbook</c> and <c>MapWorkbookWithDiagnostics</c>: the streaming loop's body as one
  /// expression. The sugar is one open, one sheet, one map, one close, so what these pin is that it
  /// is exactly that and nothing else — the same reading as the <see cref="Workbook"/> idiom, the
  /// same diagnostics, the caller's options honoured, and the book really closed when it returns.
  /// <para>
  /// The last of those is the one worth reading carefully. A leaked file handle is not detectable by
  /// reopening the file (the reader shares for reading, so a second open succeeds either way), so
  /// disposal is pinned through the door the design already documents: a value that still points at
  /// the workbook is dead on arrival, and an <see cref="ObjectDisposedException"/> off it is proof
  /// the book was closed. That is asserted on both exits — a successful map and a failing one.
  /// </para>
  /// </summary>
  public class MapWorkbookTests
  {
    private static string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", file);

    private static WorkbookOptions Cold() => new WorkbookOptions();

    /// <summary>
    /// The Detail sheet of multi-sheet.xlsx: captions Fund / Date / Amount over five records. Date is
    /// a column no member claims, which is the strict-one-way rule doing its job.
    /// </summary>
    private sealed record FundAmount(string Fund, decimal Amount);

    // --- Which declarations it takes, and which it refuses at COMPILE time ----------------------------

    [Fact]
    public void ACanonicalDeclarationIsTakenToo_BecauseASheetIsASpace()
    {
      // Two overloads, and the second is not a convenience. The streaming door hands back a plain
      // ICellSpace — the honest absence, because a streamed sheet reads values and has no formulas
      // — and an ICellSpace IS an ISpace, so a declaration written over the canonical surface is a
      // declaration this file can answer. Without the overload, the whole canonical vocabulary would
      // be unusable through the sugar for no reason a reader could name.
      IProjectionDefinition<ISpace, string?> canonical = ProjectionBuilders<ISpace>.AsText().OrBlank();

      Assert.Equal("Fund", canonical.MapWorkbook(Path("multi-sheet.xlsx"), "Detail", Cold()));
    }

    [Fact]
    public void AndADeclarationTheSheetCannotAnswerDoesNotCompile()
    {
      // MUST NOT COMPILE, and there is no way to assert that from inside a test — so it is recorded
      // here instead, beside the two overloads that make it true:
      //
      //   IProjectionDefinition<IFormulaSpace, string?> formula = SpreadsheetProjections.Formula<IFormulaSpace>();
      //   formula.MapWorkbook(path, sheet);          // CS1929/CS0411 — no overload takes it
      //
      // The receiver is IProjectionDefinition<ICellSpace, T> on one overload and IProjectionDefinition<ISpace, T> on the
      // other, and IProjectionDefinition is INVARIANT in its space (phase-6 ruling (i): `in TSpace` and a real
      // Project(Plane<TSpace>, …) are mutually exclusive), so a declaration over IFormulaSpace,
      // ISpreadsheetSpace or IValueCells<T> matches neither. That is the whole guard: the alternative
      // to a compile error here is a file's formulas quietly reading as absent, which is the failure
      // mode the capability seam exists to make unspellable.
      //
      // What the analyzers do with it: UNR003 names both spaces beside the compiler's inference
      // failure (Unrect.Analyzers.Tests.DemandsExceedOfferTests), and UNR002 offers the vocabulary
      // that could carry the demand (DemandDoorTests). Neither is a rule of its own — the refusal is
      // the type system's, and they only make it legible.
      //
      // The positive half, asserted rather than described: the two receivers the overloads name.
      Assert.Equal(
        new[] { "ICellSpace", "ISpace" },
        typeof(SpreadsheetProjectionExtensions)
          .GetMethods()
          .Where(method => method.Name == nameof(SpreadsheetProjectionExtensions.MapWorkbook) && method.GetParameters().Length == 4)
          .Select(method => method.GetParameters()[0].ParameterType.GetGenericArguments()[0].Name)
          .OrderBy(name => name, StringComparer.Ordinal));
    }

    // --- The same reading as the idiom it abbreviates ------------------------------------------------

    [Fact]
    public void ItReadsWhatOpeningTheBookYourselfReads()
    {
      var funds = Table<FundAmount>();

      var viaSugar = funds.MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());
      var viaBook = funds.Map(book.Sheet("Detail"));

      // Value equality, not merely the same shape: the records are records.
      Assert.Equal(viaBook, viaSugar);

      Assert.Equal(5, viaSugar.Count);
      Assert.Equal(new FundAmount("Alpha Fund", 100m), viaSugar[0]);
      Assert.Equal(2650m, viaSugar.Sum(fund => fund.Amount));
    }

    [Fact]
    public void AndOneDeclarationReadsAWholeDirectoryOfThem()
    {
      // The use case the sugar exists for, and the reason the peak is bounded per file rather than by
      // the largest file in the run: the declaration is a value, the loop is a Select.
      var funds = Table<FundAmount>();

      // Two workbooks that agree about nothing but the two captions this declaration binds, each
      // read against the idiom it abbreviates — one open per file, and the same records either way.
      var sheets = new[] { ("multi-sheet.xlsx", "Detail"), ("repeated-text.xlsx", "Ledger") };

      foreach (var (file, sheetName) in sheets)
      {
        using var book = Workbook.Open(Path(file), Cold());

        var read = funds.MapWorkbook(Path(file), sheetName);

        Assert.NotEmpty(read);
        Assert.Equal(funds.Map(book.Sheet(sheetName)), read);
      }
    }

    [Fact]
    public void TheDiagnosticsAreTheOnesTheTwoStepFormCollects()
    {
      // A declaration that describes one row of a six-row sheet, so there is something to notice:
      // the pairing earns its place precisely where nobody is watching.
      var firstRow = Row(cells => cells.Count);

      var viaSugar = firstRow.MapWorkbookWithDiagnostics(Path("multi-sheet.xlsx"), "Detail");

      using var book = Workbook.Open(Path("multi-sheet.xlsx"), Cold());
      var viaBook = firstRow.MapWithDiagnostics(book.Sheet("Detail"));

      Assert.Equal(viaBook.Value, viaSugar.Value);

      // Not vacuous: this reading really does leave space undescribed.
      Assert.NotEmpty(viaSugar.Diagnostics);
      Assert.Equal(
        viaBook.Diagnostics.Select(diagnostic => diagnostic.ToString()),
        viaSugar.Diagnostics.Select(diagnostic => diagnostic.ToString()));
    }

    [Fact]
    public void AndAFailureCarriesThePathAndTheCellItWouldHaveAnyway()
    {
      // Nothing about reading through the sugar changes what a failure says: the coordinates are the
      // sheet's own, because the space handed to the declaration is the whole sheet either way.
      var declaration = VerticalFlow(v => $"{v.Next(Text())}{v.Next(Decimal())}");

      var failure = Assert.Throws<ProjectionException>(
        () => declaration.MapWorkbook(Path("multi-sheet.xlsx"), "Detail"));

      Assert.Equal("VerticalFlow -> Decimal#2", failure.Path);
      Assert.Equal("A2", failure.Location.A1);
    }

    // --- Options ---------------------------------------------------------------------------------------

    [Fact]
    public void OptionsAreValidatedBeforeAnythingIsRead()
    {
      // The option object reaches Workbook.Open unchanged, which is what this catches: a cap of no
      // rows is refused by the workbook's own validation, and it is refused over a file that does not
      // exist — so the refusal cannot have come from reading anything.
      var failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
        Text().MapWorkbook(Path("no-such-workbook.xlsx"), "Any", new WorkbookOptions { BufferRows = 0 }));

      Assert.Equal("BufferRows", failure.ParamName);
    }

    [Fact]
    public void CaseSensitiveSheetNamesIsHonouredThroughTheSugar()
    {
      // An option whose effect is visible from outside: by default the name is matched loosely, and
      // the same call under the strict option fails to find the sheet at all.
      var funds = Table<FundAmount>();

      Assert.Equal(5, funds.MapWorkbook(Path("multi-sheet.xlsx"), "detail").Count);

      Assert.Throws<ArgumentException>(() => funds.MapWorkbook(
        Path("multi-sheet.xlsx"),
        "detail",
        new WorkbookOptions { CaseSensitiveSheetNames = true }));
    }

    [Fact]
    public void IsBlankIsHonouredAndChangesWhatIsRead()
    {
      // Blankness belongs to the adapter, and through this door the adapter is configured by the
      // options argument: the same cell reads as an absence under the default and as its own two
      // spaces under strict fidelity.
      var cell = Down(2).Of(Point().Select(point => point.IsText ? point.Text() : "<blank>"));

      Assert.Equal("<blank>", cell.MapWorkbook(Path("edge-cases.xlsx"), "Edges"));
      Assert.Equal(
        "  ",
        cell.MapWorkbook(Path("edge-cases.xlsx"), "Edges", new WorkbookOptions { IsBlank = _ => false }));
    }

    // --- Refusals ---------------------------------------------------------------------------------------

    [Fact]
    public void AnUnknownSheetIsTheWorkbooksOwnRefusal()
    {
      var failure = Assert.Throws<ArgumentException>(
        () => Text().MapWorkbook(Path("multi-sheet.xlsx"), "Nope"));

      Assert.Contains("No sheet named 'Nope'", failure.Message, StringComparison.Ordinal);
      Assert.Contains("multi-sheet.xlsx", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANullDeclarationIsRefusedBeforeTheFileIsOpened()
    {
      // The ordering is the pin, and the nonexistent path is how it is stated: a null projection has
      // to be an ArgumentNullException rather than whatever opening a missing file would have been.
      var missing = Path("no-such-workbook.xlsx");

      Assert.Throws<ArgumentNullException>(() => ((IProjectionDefinition<ICellSpace, string>)null!).MapWorkbook(missing, "Any"));
      Assert.Throws<ArgumentNullException>(() => ((IProjectionDefinition<ICellSpace, string>)null!).MapWorkbookWithDiagnostics(missing, "Any"));

      // ...and the path really is one that would have failed, so the assertion above is not vacuous.
      Assert.ThrowsAny<IOException>(() => Text().MapWorkbook(missing, "Any"));
    }

    // --- The workbook is gone when this returns ------------------------------------------------------------

    [Fact]
    public void TheWorkbookIsClosedOnTheWayOut()
    {
      // A view is invalidated by exactly one thing — its workbook going away — so an
      // ObjectDisposedException off a value that outlived the call is proof the close happened.
      // (Reopening the file would prove nothing: the reader shares for reading, so a leaked handle
      // does not block a second open.)
      var space = Range(WholeExtent(), block => block.Space).MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Throws<ObjectDisposedException>(() => space[0, 0].AsText());
    }

    [Fact]
    public void AndAlsoWhenTheDeclarationFails()
    {
      // The other exit, which a `using` gives for free and a hand-rolled open would be one early
      // return away from losing. The probe captures the extent it was handed on the way past; the
      // sibling after it then fails, and the captured view is dead by the time the exception
      // surfaces.
      Plane<ICellSpace>? captured = null;

      var probe = Range(1, 1, block =>
      {
        captured = block.Space;

        return block.Width;
      });

      var declaration = VerticalFlow(v => $"{v.Next(probe)}{v.Next(Decimal())}");

      Assert.Throws<ProjectionException>(() => declaration.MapWorkbook(Path("multi-sheet.xlsx"), "Detail"));

      Assert.NotNull(captured);
      Assert.Throws<ObjectDisposedException>(() => captured!.Value[0, 0].AsText());
    }

    [Fact]
    public void AValueStillKnowsItsOwnExtentAfterTheBookIsClosed()
    {
      // Deliberate, and stated beside its opposite below so the pair reads as one rule. A region is
      // WHERE a declaration was told to look — three numbers and a reference — so its extent is part
      // of the value handed to the projection rather than something read out of the file, and
      // answering it after the close costs nothing and touches nothing. Making it throw would mean
      // the region carried a lifetime it does not have, and every dimension a projection had already
      // been given would become a trap.
      var block = Range(2, 3, cells => cells).MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Equal(2, block.Width);
      Assert.Equal(3, block.Height);
      Assert.Equal(2, block.Space.Area.Width);
      Assert.Equal(3, block.Space.Area.Height);
      Assert.Equal("A1", block.Location.A1);
    }

    [Fact]
    public void AndIsStillDeadOnArrivalForAnythingThatWouldReadTheFile()
    {
      // The other half of the pair above: the extent survives, the cells do not. A read is the one
      // thing that needs the workbook, so it is the one thing that fails — which is what makes the
      // ObjectDisposedException proof that the close happened rather than an accident of what was
      // cached.
      var block = Range(2, 3, cells => cells).MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Throws<ObjectDisposedException>(() => block.Space[0, 0].AsText());
      Assert.Throws<ObjectDisposedException>(() => block[0, 0].AsText());
      Assert.Throws<ObjectDisposedException>(() => block.Row(0)[0].AsText());

      // Taking the row itself does not throw, for the same reason the extent above does not: a strip
      // is another region, named and not read. Nor does NAMING a cell of it — a point is an address,
      // and an address of a cell in a closed workbook is a perfectly good address with nothing to
      // read through.
      Assert.Equal(2, block.Row(0).Count);
      _ = block[0, 0];
      _ = block.Row(0)[0];
    }

    [Fact]
    public void ThePointsTheExploringRungsYieldAreAddressesThatOutliveTheBook()
    {
      // The two rungs that hand back POINTS rather than values — the dictionary table and the
      // labelled card — read through the same rule as everything else here, and it is worth stating
      // on them because a dictionary of points looks like data and is not.
      //
      // What a point is: a space, a column and a row. Minting one costs nothing and touches nothing,
      // so the dictionary survives the close intact and can be counted, keyed and looked up
      // afterwards. Reading through one needs the file, so that is what fails — and it fails as a
      // FAULT, which is the half that matters: a workbook that is gone must never be reported as a
      // section that was not there.
      var rows = Table().MapWorkbook(Path("multi-sheet.xlsx"), "Detail", Cold());

      Assert.Equal(5, rows.Count);
      Assert.True(rows[0].ContainsKey("Fund"));

      var fund = rows[0]["Fund"];

      Assert.Equal(0, fund.Column);
      Assert.Equal(1, fund.Row);

      Assert.Throws<ObjectDisposedException>(() => fund.AsText());
      Assert.Throws<ObjectDisposedException>(() => fund.Text());
      Assert.True(EngineRules.IsFault(new ObjectDisposedException("Workbook")));
    }

    [Fact]
    public void AndSoAreTheOnesALabelledCardYields()
    {
      // The same rule through Fields, whose element type is the same dictionary of points. Stated
      // separately because the two rungs find their cells by completely different means — a header
      // row versus a label column — and share only what they hand back.
      var card = Fields(Field("Fund")).MapWorkbook(Path("multi-sheet.xlsx"), "Detail", Cold());

      var value = card["Fund"];

      Assert.Equal(1, value.Column);
      Assert.Equal(0, value.Row);

      Assert.Throws<ObjectDisposedException>(() => value.AsText());
    }

    [Fact]
    public void AViewReturnedFromTheDeclarationIsDeadOnArrival()
    {
      // The documented trap, pinned as documented: the whole-table rung hands back a TableView, which
      // is a reader over the sheet rather than a value read from it, and the sheet is gone. Project
      // what you need inside the declaration — that is what a declaration is for.
      var view = Table(headerRows: 1, (TableView<ICellSpace> table) => table).MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Throws<ObjectDisposedException>(() => view.Space[0, 0].AsText());
      Assert.Throws<ObjectDisposedException>(() => view.Rows[0][0].AsText());
    }

    [Fact]
    public void WhereProjectingInsideTheDeclarationReadsTheSameTablePerfectlyWell()
    {
      // The other half of the trap, so it reads as a rule rather than a defect: the same rung asked
      // for VALUES comes back with values, and nothing about the lifetime is a problem.
      var captions = Table(headerRows: 1, (TableView<ICellSpace> table) => table.ColumnNames.ToArray())
        .MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Equal(new[] { "Fund", "Date", "Amount" }, captions);
    }

    // --- What must NOT compile ----------------------------------------------------------------------------
    //
    // Recorded rather than asserted; the spike's MustNotCompile.cs holds it as real code behind a
    // define. Verbatim from `dotnet build spike/TypedSpacesGauntlet -p:DefineConstants=MUST_NOT_COMPILE`,
    // re-run against this tree when these tests were written:
    //
    //   (n) Formula().MapWorkbook(path, "Data")
    //       CS1061: 'IProjectionDefinition<IFormulaSpace, string?>' does not contain a definition for
    //               'MapWorkbook' and no accessible extension method 'MapWorkbook' accepting a first
    //               argument of type 'IProjectionDefinition<IFormulaSpace, string?>' could be found (are you
    //               missing a using directive or an assembly reference?)
    //
    // The refusal is the point and the tail is misleading: no import would help, because there is no
    // demanding overload to find. A streamed sheet reads values only — Workbook.Sheet hands back a
    // plain ICellSpace — so a formula-reading declaration has no capable space here to be applied to, and
    // the alternatives were a run-time fault or a file's formulas quietly read as absent. Read
    // formulas through the eager door instead: projection.Map(SpreadsheetSpace.CreateWithFormulas(…)).
  }
}
