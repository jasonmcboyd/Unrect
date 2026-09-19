using System;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// The formula capability, wired end to end over the committed fixture: the eager door's opt-in
  /// factory, the reconstruction of a shared formula, the leaf through a real <c>Map</c>, the
  /// transport seam through a discovered extent, and the boundary fault.
  /// <para>
  /// Deliberately thin — one test per claim the phase makes, over a real file rather than a double.
  /// The broad suites are their own classes and each says what a file cannot: the slicing law across
  /// capable backends is a theory in <see cref="SpaceContractTests"/> (with a second implementation
  /// beside it, so it states a law rather than a habit); the shifter's adversarial set is
  /// <see cref="SharedFormulaShiftTests"/> and the coordinate arithmetic under it
  /// <see cref="A1ReferenceTests"/>; the .xls, .xlsb and streaming absences are
  /// <see cref="FormulaAbsenceTests"/>.
  /// </para>
  /// <para>
  /// The file is scoped to <see cref="ISpreadsheetSpace"/>, which is the whole of what used to be a
  /// capability seam: a declaration that reads a formula says so in its own type, there is no
  /// witness to pass and nothing to ask a space for at run time.
  /// </para>
  /// </summary>
  public class FormulaCapabilityTests
  {
    private static string Path() => System.IO.Path.Combine(AppContext.BaseDirectory, "TestData", "formulas.xlsx");

    private static ISpreadsheetSpace Sheet() => SpreadsheetSpace.CreateWithFormulas(Path(), "Formulas");

    [Fact]
    public void CreateWithFormulas_ReadsAFormulaAndLeavesAPlainCellNull()
    {
      var sheet = Sheet();

      Assert.Equal("SUM(D2:D5)", sheet.FormulaAt(3, 6));    // D7
      Assert.Null(sheet.FormulaAt(1, 7));                   // B8, a typed-in number
    }

    [Fact]
    public void ASharedFollowerIsReconstructedForItsOwnCell()
    {
      var sheet = Sheet();

      // D2 is the master; D3..D5 carry only its index. The relative B2 moves with the row, the
      // absolute $C$2 and $B$2 do not, and the "B2" inside the string literal is not a reference.
      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", sheet.FormulaAt(3, 1));
      Assert.Equal(@"IF(B5>0,ROUND(B5*$C$2,2)+SUM($B$2:B5),""B2"")", sheet.FormulaAt(3, 4));

      // The second group shifts columns, and LOG10 is a function rather than column LOG row 10.
      Assert.Equal("LOG10(B8)+B8", sheet.FormulaAt(1, 8));
      Assert.Equal("LOG10(D8)+D8", sheet.FormulaAt(3, 8));
    }

    [Fact]
    public void AnArrayFormulaIsSpelledAtItsAnchorAndNowhereElse()
    {
      var sheet = Sheet();

      Assert.Equal("B8:C8*2", sheet.FormulaAt(1, 11));    // B12, the anchor
      Assert.Null(sheet.FormulaAt(2, 11));                // C12 spills; the file gives it no formula
    }

    [Fact]
    public void TheDefaultDoorDoesNotImplementTheCapabilityAtAll()
    {
      var plain = SpreadsheetSpace.Create(Path(), "Formulas");

      // Honest absence: present-and-null everywhere would say the file has no formulas. The type is
      // the whole of the answer now — a declaration written over this door cannot name Formula() at
      // all, which is the static half the run-time query used to stand in for.
      Assert.False(plain is IFormulaSpace);
    }

    [Fact]
    public void ARegionOfASheetReadsItsOwnCellsFormula()
    {
      var band = Plane<ISpreadsheetSpace>.Of(Sheet()).Slice(new Offset(3, 1), new Area(1, 4));    // D2:D5

      Assert.Equal(@"IF(B4>0,ROUND(B4*$C$2,2)+SUM($B$2:B4),""B2"")", FormulaOf(band[0, 2]));      // D4
    }

    [Fact]
    public void TheLeafReadsAFormulaWhereTheCellReadsAValue()
    {
      // A cell has both, so reading both is an overlay's job: the same cell, twice.
      var cell = On(RowContaining("Text")).Right(1).Of(Overlay(o => (Value: o.Next(Text()), Formula: o.Next(Formula()))));

      var read = cell.Map(Sheet());

      Assert.Equal("42", read.Value);
      Assert.Equal(@"TEXT(6*7,""0"")", read.Formula);
    }

    [Fact]
    public void ARegionStillBeingDiscoveredStillAddressesTheSheetItself()
    {
      // Sizing by a strategy hands the projection a region whose height is not yet settled. Its
      // points are still the SHEET's cells at the sheet's own coordinates, so reading a formula off
      // one needs nothing looked up and nothing unwrapped — which is what replaced the seam.
      var scaled = On(RowContaining("Scaled")).Of(Range(RowsWhileAnyValue(), block => FormulaOf(block[1, 0])));

      Assert.Equal("LOG10(B8)+B8", scaled.Map(Sheet()));
    }

    /// <summary>The formula behind the cell a point addresses, read through the point's own space.</summary>
    private static string? FormulaOf(Point<ISpreadsheetSpace> point) => point.Space.FormulaAt(point.Column, point.Row);

    [Fact]
    public void AndThroughTheTailOfOneAsWell()
    {
      // The second child of a composite inside a bound is not handed the bound: it is handed a TAIL
      // of it, offset by what the first child consumed and with the height still the bound's to
      // discover. That is a second chart in the stack, and the one that could go wrong QUIETLY — a
      // tail whose coordinates had shifted would find the capability and answer about the wrong
      // cell. Rows 2 and 3 of this file carry the same shared expression one row apart, so a
      // one-row slip has a plausible-looking answer waiting for it.
      var lines = Sized(RowsWhileAnyValue()).Of(VerticalFlow(v =>
      {
        v.Next(Row(cells => cells.Count));

        var overlay = v.Next(Overlay(o => (Total: o.Next(Right(3).Of(Decimal())), Formula: o.Next(Right(3).Of(Formula())))));
var overlay2 = v.Next(Overlay(o => (Total: o.Next(Right(3).Of(Decimal())), Formula: o.Next(Right(3).Of(Formula())))));

return (
          First: overlay,
          Second: overlay2);
      }));

      var read = lines.Map(Sheet());

      Assert.Equal(4.5m, read.First.Total);
      Assert.Equal(@"IF(B2>0,ROUND(B2*$C$2,2)+SUM($B$2:B2),""B2"")", read.First.Formula);

      // The one that matters: D3, read through a tail two rows into the bound, and reconstructed for
      // its OWN row rather than for the master's.
      Assert.Equal(9.5m, read.Second.Total);
      Assert.Equal(@"IF(B3>0,ROUND(B3*$C$2,2)+SUM($B$2:B3),""B2"")", read.Second.Formula);
    }

    [Fact]
    public void AndAMatcherStillLooksForAFormulaThroughATailToo()
    {
      // The boundary site's door through the same two charts: the matcher demands the capability of
      // the space it is handed, which by then is a tail of a discovered bound. The first computed row
      // past the consumed header is row 2, whose first cell says which one it found.
      var firstComputed = Sized(RowsWhileAnyValue()).Of(VerticalFlow(v =>
      {
        v.Next(Row(cells => cells.Count));

        var on = v.Next(On(RowWithFormula()).Of(Row(cells => cells[0].Text())));

        return on;
      }));

      Assert.Equal("Widget", firstComputed.Map(Sheet()));
    }

    [Fact]
    public void ABoundaryThatCannotLookFaultsAndNoToleranceAbsorbsIt()
    {
      // The run-time path the typed layer cannot close: the matcher's own type names the capability,
      // so the only pipeline that accepts it is closed over a space that has one — unless a
      // declaration reaches through `Landmark` for the plain lift and casts the demand away. It is
      // then a space with no formulas, inside a boundary that is about to look for one.
      //
      // A cast this library owes itself, failing: that says the reader is wrong, never that a
      // section is missing, so it arrives as a fault and NO tolerance boundary absorbs it.
      var plain = ProjectionBuilders<ISheetCells>.On(SpreadsheetProjections.RowWithFormula().Landmark)
        .Of(SpreadsheetProjections.Text<ISheetCells>());

      ISheetCells sheet = SheetGrid.Of(new object?[,] { { "a" } });

      var failure = Assert.Throws<ProjectionException>(() => plain.Map(sheet));

      Assert.True(failure.IsFault, "a boundary that could not look must be a fault");
      Assert.IsType<InvalidCastException>(failure.GetBaseException());
      Assert.Contains("IFormulaSpace", failure.Message, StringComparison.Ordinal);

      // "I could not look" is not "the section is absent", so tolerance must not swallow it.
      Assert.Throws<ProjectionException>(() => plain.Optional().Map(sheet));
    }
  }
}
