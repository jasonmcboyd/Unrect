using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <c>Table(headerRows:, eachRow:)</c> — the rung where a record is read by a <em>projection</em>
  /// rather than by a lambda. The row is a declaration like any other, so it is inspectable, reusable
  /// and says in its own type what it demands of the space; what this suite pins is that putting one
  /// in the slot changes nothing about the table around it.
  /// <para>
  /// The fixtures are the shape the slot was designed for and the reason the phase exists: an export
  /// with no header a record can be bound by, values in columns 1, 6 and 9 with nothing between, and
  /// a last record that carries a fund code and two absences. Two shapes of row cover it — a
  /// <c>HorizontalFlow</c> where the columns are adjacent and nothing is written down, and an
  /// <c>Overlay</c> of <c>.Right(n)</c> where they are not — and which one is written is the
  /// declaration saying how its columns are found.
  /// </para>
  /// </summary>
  public class RowProjectionTableTests
  {
    // --- The fixtures ------------------------------------------------------------------------------

    /// <summary>
    /// The sparse export, distilled. Nothing here is incidental:
    /// <code>
    ///      c0        c1      c6         c9
    /// r0   PCTCAL2 BUYING POWER
    /// r1   (blank)
    /// r2   ACCOUNT   FUND    PRIMARY    FEP
    /// r3             ABC     1250.75    300.00
    /// r4             DEF      980.50    &lt;fep&gt;
    /// r5             GHI     (blank)    (blank)   &lt;- a fund code and nothing else
    /// r6   (blank)
    /// r7   TOTAL             2231.25
    /// </code>
    /// The title above and the total below are what makes the table's extent a decision rather than
    /// the whole sheet; the gaps between columns 1, 6 and 9 are what makes an overlay the right
    /// layout; and r5 is the record the declaration must be allowed to describe as incomplete.
    /// </summary>
    private static ISpace BuyingPower(object? fepOfSecondRecord = null)
    {
      var cells = new object?[8, 11];

      cells[0, 0] = "PCTCAL2 BUYING POWER";

      cells[2, 0] = "ACCOUNT";
      cells[2, 1] = "FUND";
      cells[2, 6] = "PRIMARY";
      cells[2, 9] = "FEP";

      cells[3, 1] = "ABC";
      cells[3, 6] = 1250.75m;
      cells[3, 9] = 300.00m;

      cells[4, 1] = "DEF";
      cells[4, 6] = 980.50m;
      cells[4, 9] = fepOfSecondRecord;

      cells[5, 1] = "GHI";

      cells[7, 0] = "TOTAL";
      cells[7, 6] = 2231.25m;

      return Mixed(cells);
    }

    /// <summary>The dense, headered shape: adjacent columns, so a flow reads it with no coordinates.</summary>
    private static ISpace Allocations() => Mixed(new object?[,]
    {
      { "Account", "Symbol", "Weight" },
      { "A-1", "XYZ", 1.5m },
      { "A-2", "ABC", 2.5m },
    });

    private sealed record BuyingPowerRow(string FundCode, decimal? Primary, decimal? Fep);

    private sealed record Allocation(string Account, string Symbol, decimal Weight);

    private sealed record SourcedRow(string Account, string? Formula);

    /// <summary>
    /// The sparse declaration, hoisted so the record's label is an identifier a reader can grep for.
    /// </summary>
    private static IProjection<IReadOnlyList<BuyingPowerRow>> SparseTable()
    {
      var allocation = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Text().Right(1)),
        Primary: o.Next(Decimal().OrBlank().Right(6)),
        Fep: o.Next(Decimal().OrBlank().Right(9))));

      return Table(headerRows: 0, eachRow: allocation)
        .Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue());
    }

    // --- Matrix cell 4: no headers, sparse, incomplete ---------------------------------------------

    [Fact]
    public void EveryBodyRowIsProjectedByTheRowProjection()
    {
      var records = SparseTable().Map(BuyingPower());

      Assert.Equal(
        new[]
        {
          new BuyingPowerRow("ABC", 1250.75m, 300.00m),
          new BuyingPowerRow("DEF", 980.50m, null),
          new BuyingPowerRow("GHI", null, null),
        },
        records);
    }

    [Fact]
    public void ARecordWithNothingButItsFundCodeIsStillARecord()
    {
      // The declaration is the completeness contract, per field: FundCode is required and the two
      // amounts are not, so a row carrying only the first is described rather than rejected — and
      // nothing is reported, because nothing went wrong.
      var read = SparseTable().MapWithDiagnostics(BuyingPower());

      var last = read.Value[read.Value.Count - 1];

      Assert.Equal("GHI", last.FundCode);
      Assert.Null(last.Primary);
      Assert.Null(last.Fep);

      // Nothing was absorbed, so nothing is reported about the reading. The one Info on the run is
      // the unconsumed-space notice every declaration that describes part of a sheet earns — the
      // title, the caption and the total line are outside this table by design.
      Assert.DoesNotContain(read.Diagnostics, d => d.Severity != DiagnosticSeverity.Info);

      var unconsumed = Assert.Single(read.Diagnostics);

      Assert.Equal(DiagnosticSeverity.Info, unconsumed.Severity);
      Assert.Equal("the projection consumed 3 of 8 rows; rows 1-3 and 7+ were not described", unconsumed.Message);
    }

    [Fact]
    public void ARequiredFieldOfAnIncompleteRowStillFails()
    {
      // The other side of the same contract. Take OrBlank off the fund code and the last row is no
      // longer describable — which is the point of stating tolerance per field rather than per row.
      var strict = Table(
        headerRows: 0,
        eachRow: Overlay(o => new BuyingPowerRow(
          FundCode: o.Next(Text().Right(1)),
          Primary: o.Next(Decimal().Right(6)),
          Fep: o.Next(Decimal().OrBlank().Right(9)))))
        .Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue());

      var failure = Assert.Throws<ProjectionException>(() => strict.Map(BuyingPower()));

      Assert.Equal("expected Number at G6, found Blank", Problem(failure));
    }

    [Fact]
    public void TheTableConsumesTheBandItDiscoveredAndNoMore()
    {
      // The extent is the table's, not the row's: the row consumes as much or as little of the band
      // it is handed as it likes, and the three body rows below the caption are what the declaration
      // consumed.
      var applied = SparseTable().Apply(BuyingPower());

      Assert.Equal(11, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);      // the three records, and the offset to them is not part of it
    }

    // --- Matrix cell 3: no headers, dense --------------------------------------------------------------

    [Fact]
    public void ADenseRowIsAFlowWithNoCoordinatesInIt()
    {
      // Silence is adjacency: every leaf consumes its own cell and the next starts where it stopped.
      var allocation = HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

      Assert.Equal(
        new[] { new Allocation("A-1", "XYZ", 1.5m), new Allocation("A-2", "ABC", 2.5m) },
        Table(headerRows: 1, eachRow: allocation).Map(Allocations()));
    }

    [Fact]
    public void AHeaderRowIsConsumedRatherThanProjected()
    {
      // Until the bind exists, a headered table with a row projection means "skip that row" — so the
      // same declaration over the same sheet reads two records with headerRows: 1 and meets the
      // caption row itself with headerRows: 0.
      var allocation = HorizontalFlow(h => new Allocation(
        Account: h.Next(Text()),
        Symbol: h.Next(Text()),
        Weight: h.Next(Decimal())));

      Assert.Equal(2, Table(headerRows: 1, eachRow: allocation).Map(Allocations()).Count);

      var failure = Assert.Throws<ProjectionException>(() => Table(headerRows: 0, eachRow: allocation).Map(Allocations()));

      Assert.Equal("expected Number at C1, found Text", Problem(failure));
    }

    [Fact]
    public void AHeaderRowIsStillPartOfWhatTheTableConsumed()
    {
      var withHeader = Table(headerRows: 1, eachRow: Row(cells => cells.Count)).Apply(Allocations());
      var without = Table(headerRows: 0, eachRow: Row(cells => cells.Count)).Apply(Allocations());

      Assert.Equal(2, withHeader.Value.Count);
      Assert.Equal(3, without.Value.Count);
      Assert.Equal(without.Consumed, withHeader.Consumed);
    }

    // --- The empty body --------------------------------------------------------------------------------

    [Fact]
    public void ATableWithNoBodyRowsReadsAsAnEmptyList()
    {
      var headerOnly = Mixed(new object?[,] { { "Account", "Symbol", "Weight" } });

      Assert.Empty(Table(headerRows: 1, eachRow: Row(cells => cells.Count)).Map(headerOnly));
    }

    [Fact]
    public void AndSoDoesAHeaderlessTableWithNothingInIt()
    {
      // Not a failure: a headerless table declares no row that must be there, so an extent with
      // nothing in it has described everything it was asked to describe.
      var blank = Mixed(new object?[2, 2]);

      var read = Table(headerRows: 0, eachRow: Row(cells => cells.Count))
        .Sized(RowsWhileAnyValue())
        .MapWithDiagnostics(blank);

      Assert.Empty(read.Value);
    }

    // --- One walk, asked about twice ------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void TheRowWalkAndTheRecordWalkVisitTheSameBands(int headerRows)
    {
      // Table is written over the same band walk the slot form uses, so the two readings of a
      // body are one walk asked about twice. Compared over a sheet rather than by construction,
      // because what would go wrong is a band offset drifting by a row — and that is invisible in
      // the shape of the code and obvious in the values.
      var viaRows = Table(headerRows, row => $"{row.Index}:{row[0].TryGetString() ?? "-"}").Map(Allocations());
      var viaRecords = Table(headerRows, eachRow: Row(cells => cells[0].TryGetString() ?? "-")).Map(Allocations());

      Assert.Equal(viaRows.Count, viaRecords.Count);

      for (var index = 0; index < viaRows.Count; index++)
        Assert.Equal(viaRows[index], $"{index}:{viaRecords[index]}");
    }

    [Fact]
    public void AndABandIsAsWideAsTheTableAndOneRowTall()
    {
      // The record is handed the whole row of the table's own extent — which is what makes an
      // overlay of Right(n) address the columns a reader counted on the sheet, even where the first
      // of them is empty.
      //
      // Read through WholeExtent rather than through Row on purpose: Row declares an extent of its
      // own ("as wide as the leading columns that carry values"), so over this fixture it would
      // measure ITSELF at zero — column 0 is empty in every body row — and say nothing about the
      // band it was handed.
      var bands = Table(headerRows: 0, eachRow: Range(WholeExtent(), block => $"{block.Width}x{block.Height}"))
        .Below(RowContaining("ACCOUNT"))
        .Sized(RowsWhileAnyValue())
        .Map(BuyingPower());

      Assert.Equal(new[] { "11x1", "11x1", "11x1" }, bands);
    }

    // --- The path law ------------------------------------------------------------------------------------

    [Fact]
    public void AFailureInsideARecordNamesTheRecordItHappenedIn()
    {
      // Body records are counted from zero, the way a repeat indexes its occurrences: "n/a" sits in
      // the SECOND record, so the index is 1 — and the cell is the one in the file, J5.
      var failure = Assert.Throws<ProjectionException>(() => SparseTable().Map(BuyingPower("n/a")));

      Assert.Equal("Table[1] -> 'allocation' -> Decimal?#3", failure.Path);
      Assert.Equal("J5", failure.Location.A1);
      Assert.Equal("expected Number at J5, found Text", Problem(failure));
    }

    [Fact]
    public void TheRecordsLabelComesFromTheIdentifierItWasWrittenAs()
    {
      // The index belongs to the table's own segment and the label to the record's — the same
      // division a repeat makes. A hoisted row therefore labels every occurrence of itself.
      var buyingPowerRow = Overlay(o => new BuyingPowerRow(
        FundCode: o.Next(Text().Right(1)),
        Primary: o.Next(Decimal().Right(6)),
        Fep: o.Next(Decimal().OrBlank().Right(9))));

      var failure = Assert.Throws<ProjectionException>(() =>
        Table(headerRows: 0, eachRow: buyingPowerRow)
          .Below(RowContaining("ACCOUNT"))
          .Sized(RowsWhileAnyValue())
          .Map(BuyingPower()));

      Assert.Equal("Table[2] -> 'buyingPowerRow' -> Decimal#2", failure.Path);
    }

    [Fact]
    public void AndFallsBackToItsDescriptionWhenTheRowIsWrittenInline()
    {
      // An inline row has no identifier to borrow, so it renders as what it is. The index is still
      // the table's, which is the half that has to survive either way.
      var failure = Assert.Throws<ProjectionException>(() =>
        Table(
          headerRows: 0,
          eachRow: Overlay(o => new BuyingPowerRow(
            FundCode: o.Next(Text().Right(1)),
            Primary: o.Next(Decimal().Right(6)),
            Fep: o.Next(Decimal().OrBlank().Right(9)))))
          .Below(RowContaining("ACCOUNT"))
          .Sized(RowsWhileAnyValue())
          .Map(BuyingPower()));

      Assert.Equal("Table[2] -> Overlay -> Decimal#2", failure.Path);
    }

    [Fact]
    public void AndANamedRowOutranksBoth()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        Table(
          headerRows: 0,
          eachRow: Overlay(o => new BuyingPowerRow(
            FundCode: o.Next(Text().Right(1)),
            Primary: o.Next(Decimal().Right(6)),
            Fep: o.Next(Decimal().OrBlank().Right(9)))).Named("allocation line"))
          .Below(RowContaining("ACCOUNT"))
          .Sized(RowsWhileAnyValue())
          .Map(BuyingPower()));

      Assert.Equal("Table[2] -> 'allocation line' -> Decimal#2", failure.Path);
    }

    [Fact]
    public void ARecordsFailureIsAbsorbableLikeAnyOther()
    {
      // Nothing about the slot is outside the tolerance boundaries: a row that fails inside a
      // Choice is an alternative that did not match, not a broken read.
      var read = Choice(SparseTable().Select(rows => rows.Count), Range(WholeExtent(), _ => -1))
        .MapWithDiagnostics(BuyingPower("n/a"));

      Assert.Equal(-1, read.Value);
      Assert.Single(read.Diagnostics, d => d.Severity == DiagnosticSeverity.Info);
    }

    // --- Construction ------------------------------------------------------------------------------------

    [Fact]
    public void ANullRowProjectionIsRejectedAtConstruction()
    {
      Assert.Throws<ArgumentNullException>(() => Table(0, (IProjection<int>)null!));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void AndSoIsAMultiRowHeader(int headerRows)
    {
      var failure = Assert.Throws<ArgumentOutOfRangeException>(() => Table(headerRows, Row(cells => cells.Count)));

      Assert.Contains("multi-row headers are not supported in this release", failure.Message);
    }

    // --- The demand flows through the slot -----------------------------------------------------------------

    /// <summary>
    /// A caption row over two rows whose third column is computed — enough for a record that reads
    /// both a value and a formula, which is an overlay's job because a flow would step past the cell.
    /// </summary>
    private static ISpreadsheetSpace Sourced()
    {
      var values = new CellValue[3, 3];
      var formulas = new string?[3, 3];

      values[0, 0] = CellValue.Of("Account");
      values[0, 1] = CellValue.Of("Amount");
      values[0, 2] = CellValue.Of("Total");

      values[1, 0] = CellValue.Of("Acme");
      values[1, 1] = CellValue.Of(10m);
      values[1, 2] = CellValue.Of(30m);
      formulas[1, 2] = "B2*3";

      values[2, 0] = CellValue.Of("Beta");
      values[2, 1] = CellValue.Of(20m);
      values[2, 2] = CellValue.Of(60m);
      formulas[2, 2] = "B3*3";

      return new FormulaGridSpace(values, formulas);
    }

    [Fact]
    public void ARowThatDemandsACapabilityMakesTheTableDemandIt()
    {
      // The assignment IS the assertion: this compiles only because Table(headerRows:, eachRow:)
      // carries the row's own space parameter out into its result. Nothing is annotated but the
      // overlay's witness, which is there because a layout's demand lives in its lambda's body.
      IProjection<IFormulaSpace, IReadOnlyList<SourcedRow>> table = Table(
        headerRows: 1,
        eachRow: Overlay(Formulas, o => new SourcedRow(
          Account: o.Next(Text()),
          Formula: o.Next(Formula().Right(2)))));

      Assert.Equal(
        new[] { new SourcedRow("Acme", "B2*3"), new SourcedRow("Beta", "B3*3") },
        table.Map(Sourced()));
    }

    [Fact]
    public void APlainRowLeavesTheTablePlain()
    {
      // The control, and the reason every existing declaration in the corpus still compiles: a slot
      // that demands nothing raises nothing.
      IProjection<IReadOnlyList<Allocation>> table = Table(
        headerRows: 1,
        eachRow: HorizontalFlow(h => new Allocation(
          Account: h.Next(Text()),
          Symbol: h.Next(Text()),
          Weight: h.Next(Decimal()))));

      Assert.Equal(2, table.Map(Allocations()).Count);
    }

    [Fact]
    public void TheDemandOnlyEverPointsOneWay()
    {
      // The variance direction the whole typed layer rests on, stated about the table's own type: a
      // plain table is usable where a formula-carrying space is offered, and a demanding one is not
      // usable where only a plain space is. Written reflectively so the refusal can be asserted at
      // all — the compiler's half of it cannot be, since it is the absence of a conversion.
      var plain = typeof(IProjection<IReadOnlyList<SourcedRow>>);
      var demanding = typeof(IProjection<IFormulaSpace, IReadOnlyList<SourcedRow>>);

      Assert.True(demanding.IsAssignableFrom(plain), "a plain table is usable where a demanding one is wanted");
      Assert.False(plain.IsAssignableFrom(demanding), "a demanding table must not be usable as a plain one");
    }
  }
}
