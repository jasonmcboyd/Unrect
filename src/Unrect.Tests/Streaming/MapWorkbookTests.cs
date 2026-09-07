using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;

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

    /// <summary>Warming off, as everywhere else here: a background open makes nothing wrong, only noisy.</summary>
    private static WorkbookOptions Cold() => new WorkbookOptions { WarmReaders = false };

    /// <summary>
    /// The Detail sheet of multi-sheet.xlsx: captions Fund / Date / Amount over five records. Date is
    /// a column no member claims, which is the strict-one-way rule doing its job.
    /// </summary>
    private sealed record FundAmount(string Fund, decimal Amount);

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
      // The option object reaches Workbook.Open unchanged, which is what this catches: a window of no
      // rows is refused by the workbook's own validation, and it is refused over a file that does not
      // exist — so the refusal cannot have come from reading anything.
      var failure = Assert.Throws<ArgumentOutOfRangeException>(() =>
        Text().MapWorkbook(Path("no-such-workbook.xlsx"), "Any", new WorkbookOptions { WindowRows = 0 }));

      Assert.Equal("WindowRows", failure.ParamName);
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
      var cell = Cell(value => value.TryGetString() ?? "<blank>").Down(2);

      Assert.Equal("<blank>", cell.MapWorkbook(Path("edge-cases.xlsx"), "Edges"));
      Assert.Equal(
        "  ",
        cell.MapWorkbook(Path("edge-cases.xlsx"), "Edges", new WorkbookOptions { IsBlank = _ => false }));
    }

    [Fact]
    public void ASmallWindowChangesTheCostAndNotTheAnswer()
    {
      // What WindowRows is honestly assertable for from out here. The window governs cost, and the
      // statistics that would show it die with the workbook — which is one of the documented reasons
      // to open the book yourself. What a caller CAN check is that turning the knob does not change
      // the reading, over a file tall enough for the window to matter.
      var ledger = Column(row => row.Count);
      var path = Path("tall-ledger.xlsx");

      var wide = ledger.MapWorkbook(path, "Ledger");
      var narrow = ledger.MapWorkbook(path, "Ledger", new WorkbookOptions { WarmReaders = false, WindowRows = 64, ChunkRows = 16 });

      Assert.Equal(1201, wide);
      Assert.Equal(wide, narrow);
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

      Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).MapWorkbook(missing, "Any"));
      Assert.Throws<ArgumentNullException>(() => ((IProjection<string>)null!).MapWorkbookWithDiagnostics(missing, "Any"));

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

      Assert.Throws<ObjectDisposedException>(() => space[0, 0]);
    }

    [Fact]
    public void AndAlsoWhenTheDeclarationFails()
    {
      // The other exit, which a `using` gives for free and a hand-rolled open would be one early
      // return away from losing. The probe captures the extent it was handed on the way past; the
      // sibling after it then fails, and the captured view is dead by the time the exception
      // surfaces.
      ISpace? captured = null;

      var probe = Range(1, 1, block =>
      {
        captured = block.Space;

        return block.Width;
      });

      var declaration = VerticalFlow(v => $"{v.Next(probe)}{v.Next(Decimal())}");

      Assert.Throws<ProjectionException>(() => declaration.MapWorkbook(Path("multi-sheet.xlsx"), "Detail"));

      Assert.NotNull(captured);
      Assert.Throws<ObjectDisposedException>(() => captured![0, 0]);
    }

    [Fact]
    public void AViewReturnedFromTheDeclarationIsDeadOnArrival()
    {
      // The documented trap, pinned as documented: the whole-table rung hands back a TableView, which
      // is a reader over the sheet rather than a value read from it, and the sheet is gone. Project
      // what you need inside the declaration — that is what a declaration is for.
      var view = Table(headerRows: 1, (TableView table) => table).MapWorkbook(Path("multi-sheet.xlsx"), "Detail");

      Assert.Throws<ObjectDisposedException>(() => view.Space[0, 0]);
      Assert.Throws<ObjectDisposedException>(() => view.Rows[0][0]);
    }

    [Fact]
    public void WhereProjectingInsideTheDeclarationReadsTheSameTablePerfectlyWell()
    {
      // The other half of the trap, so it reads as a rule rather than a defect: the same rung asked
      // for VALUES comes back with values, and nothing about the lifetime is a problem.
      var captions = Table(headerRows: 1, (TableView table) => table.ColumnNames.ToArray())
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
    //       CS1061: 'IProjection<IFormulaSpace, string?>' does not contain a definition for
    //               'MapWorkbook' and no accessible extension method 'MapWorkbook' accepting a first
    //               argument of type 'IProjection<IFormulaSpace, string?>' could be found (are you
    //               missing a using directive or an assembly reference?)
    //
    // The refusal is the point and the tail is misleading: no import would help, because there is no
    // demanding overload to find. A streamed sheet reads values only — Workbook.Sheet hands back a
    // plain ISpace — so a formula-reading declaration has no capable space here to be applied to, and
    // the alternatives were a run-time fault or a file's formulas quietly read as absent. Read
    // formulas through the eager door instead: projection.Map(SpreadsheetSpace.CreateWithFormulas(…)).
  }
}
